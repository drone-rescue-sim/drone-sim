from flask import Flask, request, jsonify
import requests
import tempfile
import os
import subprocess
import json
import sys
import math
from dotenv import load_dotenv
from main import get_drone_instructions, send_to_unity

# Load environment variables from config.env
load_dotenv('config.env')

app = Flask(__name__)

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"  # Unity listens on this endpoint

def query_gaze_history(tag):
    """
    Query Unity's gaze history for the last object with the specified tag
    """
    try:
        url = f"{UNITY_URL}gaze_history?tag={tag}"
        response = requests.get(url, timeout=5)
        
        if response.status_code == 200:
            try:
                raw = response.text
                data = response.json()
            except Exception:
                print(f"❌ Failed to parse gaze history JSON for tag '{tag}': {response.text[:200]}")
                return None
            if isinstance(data, dict) and data.get('found') is False:
                print(f"ℹ️ query_gaze_history('{tag}') -> found=false")
                return None
            print(f"🔎 query_gaze_history('{tag}') -> HTTP 200, keys={list(data.keys())}, raw={raw[:200]}")
            return data
        else:
            print(f"Error querying gaze history: HTTP {response.status_code}")
            return None
            
    except requests.exceptions.RequestException as e:
        print(f"Error connecting to Unity for gaze history: {e}")
        return None

def query_gaze_history_by_name(object_name):
    """
    Query Unity's gaze history for an object with the specified name
    """
    try:
        url = f"{UNITY_URL}gaze_history_by_name?name={object_name}"
        print(f"🔍 Searching for object: '{object_name}' at URL: {url}")
        response = requests.get(url, timeout=5)
        
        if response.status_code == 200:
            try:
                raw = response.text
                data = response.json()
            except Exception:
                print(f"❌ Failed to parse gaze history-by-name JSON for '{object_name}': {response.text[:200]}")
                return None
            if isinstance(data, dict) and data.get('found') is False:
                print(f"ℹ️ query_gaze_history_by_name('{object_name}') -> found=false")
                return None
            print(f"🔎 query_gaze_history_by_name('{object_name}') -> HTTP 200, keys={list(data.keys())}, raw={raw[:200]}")
            return data
        else:
            print(f"❌ Error querying gaze history by name: HTTP {response.status_code}")
            return None
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Error connecting to Unity for gaze history by name: {e}")
        return None

def _extract_position(gaze_payload):
    """Try multiple shapes to extract a position dict with x,y,z floats."""
    if not isinstance(gaze_payload, dict):
        return None
    # Common shapes
    candidates = [
        gaze_payload.get('position'),
        (gaze_payload.get('object') or {}).get('position') if isinstance(gaze_payload.get('object'), dict) else None,
        (gaze_payload.get('data') or {}).get('position') if isinstance(gaze_payload.get('data'), dict) else None,
    ]
    for pos in candidates:
        if isinstance(pos, dict) and all(k in pos for k in ('x', 'y', 'z')):
            return pos
    return None

def get_recent_gaze_history(count=30):
    """
    Get recent gaze history objects from Unity
    """
    try:
        url = f"{UNITY_URL}gaze_history_recent?count={count}"
        response = requests.get(url, timeout=5)
        
        if response.status_code == 200:
            data = response.json()
            if data and data.get('found', False):
                return data.get('objects', [])
            else:
                print("No objects found in recent gaze history")
                return []
        else:
            print(f"Error querying recent gaze history: HTTP {response.status_code}")
            return []
            
    except requests.exceptions.RequestException as e:
        print(f"Error connecting to Unity for recent gaze history: {e}")
        return []

def calculate_navigation_position(object_position, offset_distance=5.0):
    """
    Calculate a safe viewing position offset from the target object
    """
    # For now, we'll place the drone 5 meters away from the object
    # In a more sophisticated implementation, we could consider:
    # - Object size
    # - Terrain height
    # - Obstacles
    # - Optimal viewing angle
    
    # Simple offset: move 5 meters in the X direction (could be improved)
    offset_position = {
        'x': object_position['x'] + offset_distance,
        'y': object_position['y'],  # Keep same height for now
        'z': object_position['z']
    }
    
    return offset_position

def create_estimated_navigation_command(object_identifier, command_type):
    """
    Disabled: previously returned an estimated navigation command when gaze history
    was not available. Now we avoid moving when no gaze target exists.
    """
    return None

def process_navigation_command(commands):
    """
    Process navigation commands that require querying gaze history
    """
    processed_commands = []
    errors = []
    valid_drone_commands = [
        "move_forward", "move_backward", "move_left", "move_right",
        "ascend", "go_up", "descend", "go_down",
        "turn_left", "turn_right", "stop", "navigate_to_previous", "navigate_to_object"
    ]
    
    for i, cmd in enumerate(commands):
        if cmd == "navigate_to_previous":
            # Look for the object type in the next command or extract from context
            if i + 1 < len(commands):
                object_type = commands[i + 1].lower()
                
                # Query Unity for the last object of this type
                gaze_data = query_gaze_history(object_type)
                
                # If not found, try a few robust fallbacks
                if not gaze_data:
                    # 1) Common synonyms → Unity tag mapping
                    synonyms_to_tag = {
                        'human': 'Person',
                        'person': 'Person',
                        'people': 'Person',
                        'woman': 'Person',
                        'female': 'Person',
                        'man': 'Person',
                        'male': 'Person',
                        'boy': 'Person',
                        'girl': 'Person',
                        'elder': 'Person',
                        'old': 'Person'
                    }
                    words = object_type.lower().split()
                    mapped_tag = None
                    for w in words:
                        if w in synonyms_to_tag:
                            mapped_tag = synonyms_to_tag[w]
                            break
                    if mapped_tag:
                        print(f"🔁 Falling back to mapped tag '{mapped_tag}' for '{object_type}'")
                        gaze_data = query_gaze_history(mapped_tag)

                if not gaze_data:
                    # 2) Fuzzy name match against recent history
                    recent = get_recent_gaze_history(30)
                    if recent:
                        ot = object_type.lower()
                        def score(obj):
                            name = str(obj.get('name','')).lower()
                            tag = str(obj.get('tag','')).lower()
                            s = 0
                            if ot in name:
                                s += 3
                            if ot in tag:
                                s += 2
                            # token overlap bonus
                            for token in ot.split():
                                if token and token in name:
                                    s += 1
                            return s
                        best = None
                        best_score = 0
                        for obj in recent:
                            sc = score(obj)
                            if sc > best_score:
                                best, best_score = obj, sc
                        if best and best_score >= 3:
                            print(f"🔎 Fuzzy matched '{object_type}' to recent object '{best.get('name')}' (score {best_score})")
                            gaze_data = best

                if gaze_data:
                    # Calculate navigation position
                    object_pos = _extract_position(gaze_data)
                    if not object_pos:
                        message = f"Gaze data for previous {object_type} missing 'position'"
                        print(message)
                        errors.append(message)
                        continue
                    nav_pos = calculate_navigation_position(object_pos)
                    
                    # Create navigation command
                    nav_command = f"navigate_to_position:{nav_pos['x']},{nav_pos['y']},{nav_pos['z']},{object_pos['x']},{object_pos['y']},{object_pos['z']}"
                    processed_commands.append(nav_command)
                    
                    print(f"Navigation command: {object_type} at {object_pos} -> navigate to {nav_pos}")
                else:
                    message = f"Could not find previous {object_type} in gaze history"
                    print(message)
                    errors.append(message)
            else:
                print("Navigation command missing object type")
                processed_commands.append("stop")
        elif cmd == "navigate_to_object":
            # Look for the object name in the next command
            if i + 1 < len(commands):
                object_name = commands[i + 1]
                
                # Query Unity for the object with this specific name
                gaze_data = query_gaze_history_by_name(object_name)
                
                if gaze_data:
                    # Calculate navigation position
                    object_pos = _extract_position(gaze_data)
                    if not object_pos:
                        message = f"Gaze data for object '{object_name}' missing 'position'"
                        print(message)
                        errors.append(message)
                        continue
                    nav_pos = calculate_navigation_position(object_pos)
                    
                    # Create navigation command
                    nav_command = f"navigate_to_position:{nav_pos['x']},{nav_pos['y']},{nav_pos['z']},{object_pos['x']},{object_pos['y']},{object_pos['z']}"
                    processed_commands.append(nav_command)
                    
                    print(f"Navigation command: {object_name} at {object_pos} -> navigate to {nav_pos}")
                else:
                    message = f"Could not find object '{object_name}' in gaze history"
                    print(message)
                    errors.append(message)
            else:
                print("Navigation command missing object name")
                processed_commands.append("stop")
        else:
            # Regular command, add as-is (but filter out object names)
            if cmd.lower() in valid_drone_commands:
                processed_commands.append(cmd)
    
    return processed_commands, errors

@app.route('/process_command', methods=['POST'])
def process_command():
    """
    Process text commands from Unity
    """
    try:
        data = request.get_json()
        if not data or 'command' not in data:
            return jsonify({'error': 'Missing command in request'}), 400

        user_input = data['command']
        print(f"📥 Received command: {user_input}")

        # Get recent gaze history for context and print it fully (capped at 30)
        gaze_history = get_recent_gaze_history(30)
        print(f"📋 Gaze history context: {len(gaze_history)} objects (showing up to 30)")
        for idx, obj in enumerate(gaze_history[:30], 1):
            pos = obj.get('position', {})
            rot = obj.get('rotation', {})
            print(
                f"  {idx:02d}. {obj.get('name','Unknown')} [{obj.get('tag','Untagged')}] "
                f"pos=({pos.get('x',0):.2f},{pos.get('y',0):.2f},{pos.get('z',0):.2f}) "
                f"rot=({rot.get('x',0):.2f},{rot.get('y',0):.2f},{rot.get('z',0):.2f},{rot.get('w',0):.2f}) "
                f"dist={obj.get('distance','?')} ts={obj.get('timestamp','?')}"
            )
        
        # Process input with LLM (including gaze history context)
        commands = get_drone_instructions(user_input, gaze_history)

        if commands:
            print(f"📤 Processing commands: {commands}")
            
            # Check if any navigation commands need processing
            if "navigate_to_previous" in commands or "navigate_to_object" in commands:
                processed_commands, errors = process_navigation_command(commands)
                print(f"📤 Processed navigation commands: {processed_commands}")
                if errors and not processed_commands:
                    return jsonify({'status': 'not_found', 'error': errors[0], 'not_found': True}), 404
                commands = processed_commands
            
            # Send instructions to Unity
            success = send_to_unity(commands)
            if success:
                return jsonify({'status': 'success', 'commands': commands}), 200
            else:
                return jsonify({'error': 'Failed to send to Unity'}), 500
        else:
            return jsonify({'error': 'Failed to generate valid commands'}), 500

    except Exception as e:
        print(f"❌ Error processing command: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/process_audio_command', methods=['POST'])
def process_audio_command():
    """
    Process audio commands from Unity using Whisper
    """
    try:
        if 'audio' not in request.files:
            return jsonify({'error': 'No audio file provided'}), 400

        audio_file = request.files['audio']
        if audio_file.filename == '':
            return jsonify({'error': 'Empty audio file'}), 400

        # Save audio file temporarily
        with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as temp_audio:
            audio_file.save(temp_audio.name)
            temp_audio_path = temp_audio.name

        try:
            # Process audio with Whisper (using whisper command line)
            print("🎤 Processing audio with Whisper...")
            print(f"📁 Audio file: {temp_audio_path}")

            # Check file size
            import os
            file_size = os.path.getsize(temp_audio_path)
            print(f"📊 Audio file size: {file_size} bytes")

            # Run whisper via Python module runner to avoid PATH issues on Windows
            print("🚀 Starting Whisper transcription (this may take a while on first run)...")
            result = subprocess.run([
                sys.executable, '-m', 'whisper', temp_audio_path,
                '--model', 'tiny',
                '--output_format', 'json',
                '--output_dir', tempfile.gettempdir(),
                '--verbose', 'True'
            ], capture_output=True, text=True, timeout=300)

            if result.returncode != 0:
                print(f"❌ Whisper error: {result.stderr}")
                print(f"🔍 Whisper stdout: {result.stdout}")
                return jsonify({'error': 'Whisper processing failed'}), 500

            print("✅ Whisper command completed successfully")
            print(f"📝 Whisper stdout: {result.stdout[:200]}...")  # First 200 chars

            # Parse Whisper output
            output_file = temp_audio_path.replace('.wav', '.json')
            if os.path.exists(output_file):
                print(f"📄 Found output file: {output_file}")
                with open(output_file, 'r') as f:
                    whisper_data = json.load(f)

                transcript = whisper_data.get('text', '').strip()
                confidence = 0.9  # Default confidence for Whisper base model

                print(f"📝 Transcribed: '{transcript}' (confidence: {confidence})")

                if transcript:
                    # Get recent gaze history for context
                    gaze_history = get_recent_gaze_history(30)
                    
                    # Process the transcribed text as a drone command (including gaze history context)
                    commands = get_drone_instructions(transcript, gaze_history)

                    if commands:
                        print(f"📤 Processing commands: {commands}")
                        
                        # Check if any navigation commands need processing
                        if "navigate_to_previous" in commands or "navigate_to_object" in commands:
                            processed_commands, errors = process_navigation_command(commands)
                            print(f"📤 Processed navigation commands: {processed_commands}")
                            if errors and not processed_commands:
                                return jsonify({
                                    'transcript': transcript,
                                    'confidence': confidence,
                                    'status': 'not_found',
                                    'error': errors[0],
                                    'not_found': True
                                }), 404
                            commands = processed_commands
                        
                        success = send_to_unity(commands)

                        return jsonify({
                            'transcript': transcript,
                            'confidence': confidence,
                            'commands': commands,
                            'status': 'success' if success else 'failed'
                        }), 200
                    else:
                        return jsonify({
                            'transcript': transcript,
                            'confidence': confidence,
                            'error': 'Failed to generate valid commands'
                        }), 500
                else:
                    return jsonify({'error': 'No speech detected'}), 400
            else:
                return jsonify({'error': 'Whisper output file not found'}), 500

        finally:
            # Clean up temporary files
            try:
                os.unlink(temp_audio_path)
                output_file = temp_audio_path.replace('.wav', '.json')
                if os.path.exists(output_file):
                    os.unlink(output_file)
            except:
                pass

    except subprocess.TimeoutExpired:
        print("⏰ Audio processing timed out after 5 minutes")
        print("💡 This usually happens on first run when downloading the Whisper model")
        print("🔄 Try again in a few minutes once the model is cached")
        return jsonify({
            'error': 'Audio processing timeout - model may be downloading',
            'hint': 'Try again in a few minutes'
        }), 408
    except Exception as e:
        print(f"❌ Error processing audio: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/health', methods=['GET'])
def health():
    """
    Health check endpoint
    """
    # Check if whisper is available
    try:
        result = subprocess.run([sys.executable, '-m', 'whisper', '--help'], capture_output=True, text=True, timeout=5)
        whisper_available = result.returncode == 0
    except:
        whisper_available = False

    # Check if ollama is available
    try:
        import ollama
        ollama_available = True
    except:
        ollama_available = False

    # Test LLM connection
    llm_working = False
    try:
        test_commands = get_drone_instructions("test")
        llm_working = test_commands and test_commands != ["stop"]
    except:
        llm_working = False

    return jsonify({
        'status': 'healthy',
        'service': 'drone-llm-service',
        'whisper_available': whisper_available,
        'ollama_available': ollama_available,
        'llm_working': llm_working
    }), 200

@app.route('/test_llm', methods=['GET'])
def test_llm():
    """
    Test LLM endpoint
    """
    try:
        test_commands = get_drone_instructions("move forward")
        return jsonify({
            'status': 'success',
            'test_input': 'move forward',
            'test_output': test_commands,
            'llm_working': test_commands and test_commands != ["stop"]
        }), 200
    except Exception as e:
        return jsonify({
            'status': 'error',
            'error': str(e),
            'llm_working': False
        }), 500

if __name__ == '__main__':
    print("🚀 Starting Drone LLM HTTP Service...")
    
    # Import and show LLM provider info
    from main import LLM_PROVIDER, OPENAI_MODEL, OLLAMA_MODEL, OPENAI_API_KEY
    print(f"🔧 LLM Provider: {LLM_PROVIDER.upper()}")
    if LLM_PROVIDER.lower() == "openai":
        print(f"🤖 OpenAI Model: {OPENAI_MODEL}")
        if not OPENAI_API_KEY:
            print("⚠️  Warning: OPENAI_API_KEY not set. Set it as environment variable.")
    else:
        print(f"🤖 Ollama Model: {OLLAMA_MODEL}")
    
    print("📡 Listening on http://127.0.0.1:5006")
    print("📝 Text commands: POST /process_command")
    print("🎤 Audio commands: POST /process_audio_command")
    print("💚 Health check: GET /health")
    print("🧪 Test LLM: GET /test_llm")
    print("-" * 50)

    app.run(host='127.0.0.1', port=5006, debug=True)
