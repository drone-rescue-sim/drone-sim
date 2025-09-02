import ollama
import requests  # for HTTP requests
from flask import Flask, request, jsonify
import json
import threading
import time

app = Flask(__name__)

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/receive_command"  # Unity must listen at this URL, could use ports or other methods 

def get_drone_instructions(user_input):
    """
    Process user input using an LLM to generate drone instructions in JSON format.
    """
    try:
        response = ollama.chat(
            model="llama2",  # Or another model of choice
            messages=[
                {"role": "system",
                 "content": """You are a drone control assistant for Unity. Translate user instructions into simple drone movement commands.
                 Always respond with valid JSON in this exact format:
                 {"command": "single_command", "details": "brief description"}

                 Available commands: move_forward, move_backward, move_left, move_right, ascend, descend, turn_left, turn_right

                 Examples:
                 - "go forward" -> {"command": "move_forward", "details": "Moving drone forward"}
                 - "fly up and turn left" -> {"command": "ascend; turn_left", "details": "Ascending and turning left"}
                 - "move to the right" -> {"command": "move_right", "details": "Moving drone to the right"}

                 Only output the JSON, no additional text."""},
                {"role": "user", "content": user_input}
            ],
            options={"format": "json"}  # Force JSON output
        )
        instructions = response['message']['content']

        # Validate JSON
        try:
            json_data = json.loads(instructions)
            return json_data
        except json.JSONDecodeError as json_error:
            print(f"LLM output is not valid JSON: {instructions}")
            print(f"JSON Error: {json_error}")
            return {"command": "unknown", "details": "Failed to parse LLM response"}

    except Exception as e:
        print(f"Error generating instructions: {e}")
        return {"command": "error", "details": f"LLM service error: {str(e)}"}

def send_to_unity(command_data):
    """
    Send the processed instructions to Unity via HTTP POST.
    """
    try:
        payload = {
            "command": command_data.get("command", ""),
            "details": command_data.get("details", "")
        }
        r = requests.post(UNITY_URL, json=payload)
        if r.status_code == 200:
            print(f"Sent to Unity: {payload}")
        else:
            print(f"Unity responded with status code {r.status_code}")
            print(f"Response: {r.text}")
    except Exception as e:
        print(f"Error sending to Unity: {e}")

@app.route('/process_command', methods=['POST'])
def process_command():
    """
    Endpoint for Unity to send user input for processing.
    """
    try:
        data = request.get_json()
        if not data or 'input' not in data:
            return jsonify({"error": "Missing 'input' field"}), 400

        user_input = data['input']
        print(f"Received command from Unity: {user_input}")

        # Process input with LLM
        result = get_drone_instructions(user_input)
        if result:
            print(f"Generated command: {result}")
            # Send to Unity for execution
            send_to_unity(result)
            return jsonify(result), 200
        else:
            return jsonify({"command": "error", "details": "Failed to generate instructions"}), 500

    except Exception as e:
        print(f"Error in process_command: {e}")
        return jsonify({"command": "error", "details": f"Server error: {str(e)}"}), 500

def run_flask():
    """Run Flask server in a separate thread"""
    print("Starting LLM service on port 5006...")
    app.run(host='127.0.0.1', port=5006, debug=False)

def main():
    """Console mode for testing"""
    print("Drone Simulation LLM Service Started")
    print("Running in console mode - Unity should call /process_command endpoint")
    print("Press Ctrl+C to exit")

    while True:
        try:
            # Get user input
            user_input = input("Enter instructions for the drone: ")
            if user_input.lower() in ['exit', 'quit']:
                print("Exiting...")
                break

            # Process input with LLM
            result = get_drone_instructions(user_input)
            if result:
                print(f"Generated Instructions: {result}")
                # Send instructions to Unity
                send_to_unity(result)
            else:
                print("Failed to generate instructions. Please try again.")
        except KeyboardInterrupt:
            print("\nExiting...")
            break
        except Exception as e:
            print(f"Error: {e}")

if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1 and sys.argv[1] == "--flask":
        # Run Flask server only
        run_flask()
    else:
        # Run both Flask server and console interface
        flask_thread = threading.Thread(target=run_flask, daemon=True)
        flask_thread.start()
        time.sleep(1)  # Give Flask time to start
        main()
