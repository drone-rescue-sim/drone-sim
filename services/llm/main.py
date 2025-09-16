#!/usr/bin/env python3
"""
Drone LLM Main Service
Process natural language commands for drone control
"""

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
        system_prompt = """You are a drone control assistant for Unity. Translate user instructions into simple drone movement commands.

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
1. If the user gives a single command, respond with just that command name
2. If the user gives multiple commands (like "fly forward and go up"), respond with a comma-separated list of commands
3. Use exactly the command names listed above
4. If unsure about any part, skip that part or use "stop" for unclear sections
5. Respond with ONLY command names separated by commas, nothing else

Examples:
User: "fly forward" -> move_forward
User: "go up in the air" -> ascend
User: "turn around" -> turn_left
User: "move to the right" -> move_right
User: "hover in place" -> stop
User: "fly forward and go up" -> move_forward,ascend
User: "move left then stop" -> move_left,stop
User: "turn right and move forward" -> turn_right,move_forward
User: "go up, turn left, and then stop" -> ascend,turn_left,stop
"""

        print(f"🧠 Processing with LLM: '{user_input}'")
        response = ollama.chat(
            model="llama2",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input}
            ]
        )
        instructions = response['message']['content']

        # Clean and validate the response
        response_text = instructions.strip().lower()

        # Validate commands (handle both single and multiple commands)
        valid_commands = [
            "move_forward", "move_backward", "move_left", "move_right",
            "ascend", "go_up", "descend", "go_down",
            "turn_left", "turn_right", "stop"
        ]

        # Split by comma and clean up
        command_list = [cmd.strip() for cmd in response_text.split(',') if cmd.strip()]

        # Validate each command
        validated_commands = []
        for cmd in command_list:
            if cmd in valid_commands:
                validated_commands.append(cmd)
            else:
                print(f"Invalid command generated: {cmd}, skipping")
                # Don't add invalid commands

        # Return list of validated commands, or ['stop'] if none are valid
        if validated_commands:
            print(f"📋 Generated commands: {validated_commands}")
            return validated_commands
        else:
            print(f"No valid commands generated from: {response_text}, using ['stop'] instead")
            return ["stop"]

    except Exception as e:
        print(f"Error generating instructions: {e}")
        return ["stop"]

def send_to_unity(command):
    """
    Send the processed instructions to Unity via HTTP POST.
    Now supports both single commands and lists of commands.
    """
    try:
        # Handle both single command and list of commands
        if isinstance(command, list):
            return send_multiple_commands_to_unity(command)
        else:
            # Single command - wrap in list for consistency
            return send_multiple_commands_to_unity([command])
    except Exception as e:
        print(f"✗ Error sending to Unity: {e}")
        return False

def send_multiple_commands_to_unity(commands):
    """
    Send multiple commands to Unity with delays between each command.
    """
    import time

    success_count = 0
    for i, command in enumerate(commands):
        try:
            payload = {"command": command}
            headers = {"Content-Type": "application/json"}

            r = requests.post(UNITY_URL, json=payload, headers=headers, timeout=5.0)

            if r.status_code == 200:
                print(f"✓ Sent command {i+1}/{len(commands)} to Unity: {command}")
                success_count += 1
            else:
                print(f"✗ Command {i+1} failed - Unity responded with status code {r.status_code}: {r.text}")
                return False

            # Add delay between commands (except for the last one)
            if i < len(commands) - 1:
                time.sleep(0.5)  # 500ms delay between commands

        except requests.exceptions.ConnectionError:
            print("✗ Cannot connect to Unity. Make sure Unity is running and the HTTP server is started.")
            return False
        except requests.exceptions.Timeout:
            print("✗ Request to Unity timed out.")
            return False
        except Exception as e:
            print(f"✗ Error sending command {i+1} to Unity: {e}")
            return False

    print(f"✅ Successfully sent {success_count}/{len(commands)} commands to Unity")
    return True

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

            if commands and len(commands) > 0:
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
