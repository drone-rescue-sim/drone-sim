#!/usr/bin/env python3
"""
Demo script showing the LLM-Unity drone control integration
This script demonstrates the full pipeline without requiring actual services
"""

import time
import json

def simulate_llm_processing(user_input):
    """Simulate LLM processing (since Ollama may not be running)"""
    # Mock responses based on input
    mock_responses = {
        "fly forward": "move_forward",
        "go up": "ascend",
        "turn left": "turn_left",
        "stop": "stop",
        "move right": "move_right",
        "go down": "descend",
        "fly backward": "move_backward",
        "turn right": "turn_right"
    }

    # Find matching response or default to stop
    for key, response in mock_responses.items():
        if key in user_input.lower():
            return response

    return "stop"

def simulate_unity_response(command):
    """Simulate Unity HTTP response"""
    return {
        "status": "ok",
        "received_command": command,
        "timestamp": time.time()
    }

def demo_pipeline():
    """Demonstrate the full LLM -> Unity pipeline"""
    print("🚁 DRONE CONTROL INTEGRATION DEMO")
    print("=" * 50)
    print("This demo shows how the LLM-Unity integration works")
    print()

    demo_commands = [
        "fly forward",
        "go up in the air",
        "turn left",
        "stop moving",
        "move to the right"
    ]

    for user_input in demo_commands:
        print(f"👤 User says: '{user_input}'")

        # Step 1: LLM Processing
        print("🧠 LLM processing...")
        time.sleep(0.5)  # Simulate processing time
        llm_command = simulate_llm_processing(user_input)
        print(f"   Generated command: {llm_command}")

        # Step 2: Send to Unity
        print("📤 Sending to Unity...")
        time.sleep(0.3)  # Simulate network delay
        unity_response = simulate_unity_response(llm_command)
        print(f"   Unity response: {unity_response['status']}")

        # Step 3: Show result
        if unity_response['status'] == 'ok':
            print(f"✅ Drone should now: {llm_command}")
        else:
            print("❌ Command failed")

        print("-" * 30)
        time.sleep(1)

    print("\n🎉 Demo complete!")
    print("\nTo run with real services:")
    print("1. Start Unity and press Play")
    print("2. Start Ollama: ollama serve")
    print("3. Run: python services/llm/main.py")
    print("4. Type natural language commands!")

def show_command_mapping():
    """Show the command mapping table"""
    print("\n📋 COMMAND MAPPING")
    print("=" * 50)

    mappings = [
        ("Natural Language", "Drone Command"),
        ("-" * 20, "-" * 15),
        ("fly forward", "move_forward"),
        ("go up / fly higher", "ascend"),
        ("turn left / rotate left", "turn_left"),
        ("move right", "move_right"),
        ("go down / fly lower", "descend"),
        ("fly backward", "move_backward"),
        ("turn right / rotate right", "turn_right"),
        ("stop / hover", "stop")
    ]

    for natural, command in mappings:
        print("20")

def main():
    """Main demo function"""
    show_command_mapping()
    demo_pipeline()

if __name__ == "__main__":
    main()
