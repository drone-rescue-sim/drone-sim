from flask import Flask, request, jsonify
import requests
import tempfile
import os
import subprocess
import json
from main import get_drone_instructions, send_to_unity

app = Flask(__name__)

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"  # Unity listens on this endpoint

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

        # Process input with LLM
        command = get_drone_instructions(user_input)

        if command:
            print(f"📤 Processing command: {command}")
            # Send instructions to Unity
            success = send_to_unity(command)
            if success:
                return jsonify({'status': 'success', 'command': command}), 200
            else:
                return jsonify({'error': 'Failed to send to Unity'}), 500
        else:
            return jsonify({'error': 'Failed to generate valid command'}), 500

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

            # Run whisper command with longer timeout (5 minutes for first model download)
            print("🚀 Starting Whisper transcription (this may take a while on first run)...")
            result = subprocess.run([
                'whisper', temp_audio_path,
                '--model', 'tiny',  # Use smaller, faster model
                '--output_format', 'json',
                '--output_dir', tempfile.gettempdir(),
                '--verbose', 'True'  # Add verbose output
            ], capture_output=True, text=True, timeout=300)  # 5 minute timeout

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
                    # Process the transcribed text as a drone command
                    command = get_drone_instructions(transcript)

                    if command:
                        print(f"📤 Sending command: {command}")
                        success = send_to_unity(command)

                        return jsonify({
                            'transcript': transcript,
                            'confidence': confidence,
                            'command': command,
                            'status': 'success' if success else 'failed'
                        }), 200
                    else:
                        return jsonify({
                            'transcript': transcript,
                            'confidence': confidence,
                            'error': 'Failed to generate valid command'
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
        result = subprocess.run(['whisper', '--help'], capture_output=True, text=True, timeout=5)
        whisper_available = result.returncode == 0
    except:
        whisper_available = False

    # Check if ollama is available
    try:
        import ollama
        ollama_available = True
    except:
        ollama_available = False

    return jsonify({
        'status': 'healthy',
        'service': 'drone-llm-service',
        'whisper_available': whisper_available,
        'ollama_available': ollama_available
    }), 200

if __name__ == '__main__':
    print("🚀 Starting Drone LLM HTTP Service...")
    print("📡 Listening on http://127.0.0.1:5006")
    print("📝 Text commands: POST /process_command")
    print("🎤 Audio commands: POST /process_audio_command")
    print("💚 Health check: GET /health")
    print("-" * 50)

    app.run(host='127.0.0.1', port=5006, debug=True)
