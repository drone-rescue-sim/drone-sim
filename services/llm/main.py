import ollama
import requests  # for HTTP requests

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"  # Unity listens on this endpoint 

def get_drone_instructions(user_input):
    """
    Process user input using an LLM to generate drone instructions.
    """
    try:
        # Improved system prompt with specific commands and examples
        system_prompt = """You are a drone control assistant for Unity. Translate user instructions into drone movement commands.

Available commands:
- move_forward: Move the drone forward
- move_backward: Move the drone backward
- move_left: Move the drone left
- move_right: Move the drone right
- ascend or go_up: Move the drone up
- descend or go_down: Move the drone down
- turn_left: Rotate the drone left
- turn_right: Rotate the drone right
- stop: Stop all movement

Instructions:
1. Respond with a JSON array of commands, e.g., ["move_forward", "ascend"]
2. Use exactly the command names listed above
3. If the user gives multiple instructions, include all relevant commands
4. If the user gives a single instruction, return a single-element array
5. If unsure, respond with ["stop"]

Examples:
User: "fly forward" -> ["move_forward"]
User: "go up in the air" -> ["ascend"]
User: "turn around" -> ["turn_left"]
User: "move to the right" -> ["move_right"]
User: "hover in place" -> ["stop"]
User: "fly forward and go up" -> ["move_forward", "ascend"]
User: "turn left and move forward" -> ["turn_left", "move_forward"]
User: "go up and turn right" -> ["ascend", "turn_right"]
"""

        response = ollama.chat(
            model="llama2",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input}
            ]
        )
        instructions = response['message']['content']

        # Clean and validate the response
        command_text = instructions.strip()
        
        # Try to parse as JSON array
        try:
            import json
            commands = json.loads(command_text)
            
            # Ensure it's a list
            if not isinstance(commands, list):
                commands = [commands]
            
            # Validate each command
            valid_commands = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop"
            ]
            
            validated_commands = []
            for cmd in commands:
                cmd_lower = cmd.strip().lower()
                if cmd_lower in valid_commands:
                    validated_commands.append(cmd_lower)
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
                "turn_left", "turn_right", "stop"
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

def main():
    print("🤖 Drone Control LLM Service Started")
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
