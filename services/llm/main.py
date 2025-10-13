import ollama
import requests  # for HTTP requests
import os
import json
from dotenv import load_dotenv

# Load environment variables from config.env
load_dotenv('config.env')

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"  # Unity listens on this endpoint

# LLM Configuration
LLM_PROVIDER = os.getenv("LLM_PROVIDER", "ollama")  # "ollama" or "openai"
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
OPENAI_MODEL = os.getenv("OPENAI_MODEL", "gpt-3.5-turbo")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama2")

def call_ollama(system_prompt, user_input):
    """
    Call Ollama API with the given prompts
    """
    try:
        print(f"🔄 Calling Ollama with model: {OLLAMA_MODEL}")
        response = ollama.chat(
            model=OLLAMA_MODEL,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input}
            ]
        )
        return response['message']['content']
    except Exception as e:
        print(f"❌ Error calling Ollama: {e}")
        print("💡 Make sure Ollama is running and the model is available:")
        print(f"   ollama serve")
        print(f"   ollama pull {OLLAMA_MODEL}")
        return None

def call_openai(system_prompt, user_input):
    """
    Call OpenAI API with the given prompts
    """
    try:
        if not OPENAI_API_KEY:
            print("❌ OpenAI API key not found. Set OPENAI_API_KEY environment variable.")
            return None
            
        headers = {
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json"
        }
        
        payload = {
            "model": OPENAI_MODEL,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input}
            ],
            "stream": False      # Ensure non-streaming for faster processing
        }
        
        # Add model-specific parameters
        if OPENAI_MODEL.startswith("gpt-5"):
            payload["max_completion_tokens"] = 50  # Use max_completion_tokens for newer models
        else:
            payload["max_tokens"] = 50  # Use max_tokens for older models
            payload["temperature"] = 0.1  # Low temperature for consistent responses
        
        response = requests.post(
            "https://api.openai.com/v1/chat/completions",
            headers=headers,
            json=payload,
            timeout=10  # Reduced timeout for faster response
        )
        
        if response.status_code == 200:
            result = response.json()
            return result['choices'][0]['message']['content']
        else:
            print(f"❌ OpenAI API error: {response.status_code}")
            print(f"📄 Response: {response.text}")
            if response.status_code == 401:
                print("🔑 Authentication failed - check your API key")
            elif response.status_code == 404:
                print("🤖 Model not found - check your model name")
            elif response.status_code == 429:
                print("⏰ Rate limit exceeded - try again later")
            return None
            
    except Exception as e:
        print(f"Error calling OpenAI: {e}")
        return None

def get_drone_instructions(user_input, gaze_history=None):
    """
    Process user input using an LLM to generate drone instructions.
    """
    try:
        # Get recent gaze history if not provided
        if gaze_history is None:
            from http_service import get_recent_gaze_history
            gaze_history = get_recent_gaze_history(30)
        
        # Build gaze history context (shorter for faster processing)
        gaze_context = ""
        if gaze_history:
            # Include full recent gaze history (capped at 30) with detailed fields
            lines = []
            for idx, obj in enumerate(gaze_history[:30], 1):
                pos = obj.get('position', {})
                rot = obj.get('rotation', {})
                name = obj.get('name', 'Unknown')
                tag = obj.get('tag', 'Untagged')
                dist = obj.get('distance', 'Unknown')
                ts = obj.get('timestamp', 'Unknown')
                lines.append(
                    f"{idx:02d}. name={name} tag={tag} pos=({pos.get('x',0):.2f},{pos.get('y',0):.2f},{pos.get('z',0):.2f}) "
                    f"rot=({rot.get('x',0):.2f},{rot.get('y',0):.2f},{rot.get('z',0):.2f},{rot.get('w',0):.2f}) dist={dist} ts={ts}"
                )
            gaze_context = "\n\nRecent gaze objects (max 30):\n" + "\n".join(lines) + "\n"
        
        # Optimized system prompt for faster processing
        system_prompt = f"""Drone control assistant. Return JSON array.{gaze_context}

Commands: move_forward, move_backward, move_left, move_right, ascend, descend, turn_left, turn_right, stop, navigate_to_previous, navigate_to_object

Rules:
- Use navigate_to_object for specific names from objects list
- Use navigate_to_previous for object types (e.g., Person)
- Use the recent gaze objects list to disambiguate natural phrases; you do not need exact tag equality here
- Return JSON array like ["move_forward"]

Examples:
"move forward" -> ["move_forward"]
"move to Tree 1" -> ["navigate_to_object", "Tree 1"]
"go to person" -> ["navigate_to_previous", "Person"]
"move to elder female" -> ["navigate_to_previous", "Person"]
"""

        # Call the appropriate LLM provider
        if LLM_PROVIDER.lower() == "openai":
            print(f"🤖 Using OpenAI ({OPENAI_MODEL})")
            instructions = call_openai(system_prompt, user_input)
        else:
            print(f"🤖 Using Ollama ({OLLAMA_MODEL})")
            instructions = call_ollama(system_prompt, user_input)
        
        if not instructions:
            print("❌ Failed to get response from LLM")
            if LLM_PROVIDER.lower() == "openai":
                print("💡 Check your OpenAI API key and model configuration")
            else:
                print("💡 Check if Ollama is running and the model is available")
            print("🛑 Using 'stop' command as fallback")
            return ["stop"]

        # Clean and validate the response
        command_text = instructions.strip()
        
        # Try to parse as JSON array
        try:
            import json
            commands = json.loads(command_text)
            
            # Ensure it's a list
            if not isinstance(commands, list):
                commands = [commands]
            
            # Validate each command (including new navigation commands)
            valid_commands = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop", "navigate_to_previous", "navigate_to_object"
            ]
            
            validated_commands = []
            for i, cmd in enumerate(commands):
                cmd_lower = cmd.strip().lower()
                if cmd_lower in valid_commands:
                    validated_commands.append(cmd_lower)
                elif i > 0 and validated_commands and validated_commands[-1] in ["navigate_to_previous", "navigate_to_object"]:
                    # This is likely an object name/type for navigation commands
                    validated_commands.append(cmd.strip())  # Keep original case for object names
                    print(f"Added object name for navigation: {cmd}")
                else:
                    print(f"Invalid command in array: {cmd}, skipping")
            
            if validated_commands:
                return validated_commands
            else:
                print("No valid commands found, using 'stop'")
                return ["stop"]
                
        except json.JSONDecodeError:
            # Fallback: try to parse as single command
            command = command_text.lower()
            valid_commands = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop", "navigate_to_previous", "navigate_to_object"
            ]
            
            if command in valid_commands:
                return [command]
            else:
                print(f"Invalid command generated: {command}, using 'stop' instead")
                return ["stop"]

    except Exception as e:
        print(f"Error generating instructions: {e}")
        return ["stop"]

def send_to_unity(commands):
    """
    Send the processed instructions to Unity via HTTP POST.
    """
    try:
        # Handle both single command (string) and multiple commands (list) for backward compatibility
        if isinstance(commands, str):
            commands = [commands]
        
        payload = {"commands": commands}
        headers = {"Content-Type": "application/json"}

        r = requests.post(UNITY_URL, json=payload, headers=headers, timeout=5.0)

        if r.status_code == 200:
            print(f"✓ Sent commands to Unity: {commands}")
            return True
        else:
            print(f"✗ Unity responded with status code {r.status_code}: {r.text}")
            return False
    except requests.exceptions.ConnectionError:
        print("✗ Cannot connect to Unity. Make sure Unity is running and the HTTP server is started.")
        return False
    except requests.exceptions.Timeout:
        print("✗ Request to Unity timed out.")
        return False
    except Exception as e:
        print(f"✗ Error sending to Unity: {e}")
        return False

def test_llm_connection():
    """
    Test the LLM connection with a simple command
    """
    print("🧪 Testing LLM connection...")
    test_result = get_drone_instructions("move forward")
    if test_result and test_result != ["stop"]:
        print(f"✅ LLM connection successful! Test result: {test_result}")
        return True
    else:
        print("❌ LLM connection failed!")
        return False

def main():
    print("🤖 Drone Control LLM Service Started")
    print(f"🔧 LLM Provider: {LLM_PROVIDER.upper()}")
    if LLM_PROVIDER.lower() == "openai":
        print(f"🤖 OpenAI Model: {OPENAI_MODEL}")
        if not OPENAI_API_KEY:
            print("⚠️  Warning: OPENAI_API_KEY not set. Set it as environment variable.")
            print("   export OPENAI_API_KEY=your_api_key_here")
    else:
        print(f"🤖 Ollama Model: {OLLAMA_MODEL}")
    
    # Test connection
    if not test_llm_connection():
        print("💡 Please check your LLM configuration and try again.")
        return
    
    print("Commands: Type natural language instructions for the drone")
    print("Examples: 'fly forward', 'go up', 'turn left', 'stop', 'exit'")
    print("-" * 50)

    while True:
        try:
            # Get user input
            user_input = input("\n🎯 Enter drone instruction: ").strip()

            if not user_input:
                continue

            if user_input.lower() in ['exit', 'quit', 'bye']:
                print("👋 Exiting drone control service...")
                break

            # Process input with LLM
            print("🧠 Processing with LLM...")
            commands = get_drone_instructions(user_input)

            if commands:
                print(f"📤 Sending commands: {commands}")
                # Send instructions to Unity
                success = send_to_unity(commands)
                if not success:
                    print("💡 Tip: Make sure Unity is running with the drone scene active")
            else:
                print("❌ Failed to generate valid commands")

        except KeyboardInterrupt:
            print("\n👋 Interrupted by user. Exiting...")
            break
        except Exception as e:
            print(f"❌ Unexpected error: {e}")
            continue

if __name__ == "__main__":
    main()
