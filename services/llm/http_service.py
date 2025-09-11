import ollama
import requests  # for HTTP requests
from flask import Flask, request, jsonify
from flask_cors import CORS
import threading
import time
import logging

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

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
1. Respond with ONLY the command name, nothing else
2. Use exactly the command names listed above
3. If the user gives a complex instruction, break it down to the most important single command
4. If unsure, respond with "stop"

Examples:
User: "fly forward" -> move_forward
User: "go up in the air" -> ascend
User: "turn around" -> turn_left
User: "move to the right" -> move_right
User: "hover in place" -> stop
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
        command = instructions.strip().lower()

        # Validate command
        valid_commands = [
            "move_forward", "move_backward", "move_left", "move_right",
            "ascend", "go_up", "descend", "go_down",
            "turn_left", "turn_right", "stop"
        ]

        if command in valid_commands:
            return command
        else:
            logger.warning(f"Invalid command generated: {command}, using 'stop' instead")
            return "stop"

    except Exception as e:
        logger.error(f"Error generating instructions: {e}")
        return "stop"

def send_to_unity(command):
    """
    Send the processed instructions to Unity via HTTP POST.
    """
    try:
        payload = {"command": command}
        headers = {"Content-Type": "application/json"}

        r = requests.post(UNITY_URL, json=payload, headers=headers, timeout=5.0)

        if r.status_code == 200:
            logger.info(f"✓ Sent command to Unity: {command}")
            return True, None
        else:
            error_msg = f"✗ Unity responded with status code {r.status_code}: {r.text}"
            logger.error(error_msg)
            return False, error_msg
    except requests.exceptions.ConnectionError:
        error_msg = "✗ Cannot connect to Unity. Make sure Unity is running and the HTTP server is started."
        logger.error(error_msg)
        return False, error_msg
    except requests.exceptions.Timeout:
        error_msg = "✗ Request to Unity timed out."
        logger.error(error_msg)
        return False, error_msg
    except Exception as e:
        error_msg = f"✗ Error sending to Unity: {e}"
        logger.error(error_msg)
        return False, error_msg

@app.route('/process_command', methods=['POST'])
def process_command():
    """
    HTTP endpoint to process commands from Unity UI.
    """
    try:
        data = request.get_json()
        if not data or 'command' not in data:
            return jsonify({
                'error': 'Missing command in request body',
                'status': 'error'
            }), 400

        user_command = data['command'].strip()
        if not user_command:
            return jsonify({
                'error': 'Empty command',
                'status': 'error'
            }), 400

        logger.info(f"Processing command from Unity: {user_command}")

        # Process with LLM
        processed_command = get_drone_instructions(user_command)

        # Send to Unity
        success, error_msg = send_to_unity(processed_command)

        if success:
            return jsonify({
                'processed_command': processed_command,
                'status': 'success'
            })
        else:
            return jsonify({
                'processed_command': processed_command,
                'error': error_msg,
                'status': 'unity_error'
            })

    except Exception as e:
        logger.error(f"Error processing command: {e}")
        return jsonify({
            'error': f'Internal server error: {str(e)}',
            'status': 'error'
        }), 500

@app.route('/health', methods=['GET'])
def health_check():
    """
    Health check endpoint.
    """
    return jsonify({
        'status': 'healthy',
        'service': 'drone-llm-service',
        'ollama_available': check_ollama_status()
    })

def check_ollama_status():
    """
    Check if Ollama service is available.
    """
    try:
        ollama.list()
        return True
    except Exception as e:
        logger.warning(f"Ollama check failed: {e}")
        return False

@app.route('/', methods=['GET'])
def root():
    """
    Root endpoint with service information.
    """
    return jsonify({
        'service': 'Drone LLM Control Service',
        'version': '1.0.0',
        'endpoints': {
            'POST /process_command': 'Process natural language drone commands',
            'GET /health': 'Service health check'
        },
        'status': 'running'
    })

def run_server():
    """
    Run the Flask server.
    """
    logger.info("🤖 Drone Control LLM HTTP Service Starting...")
    logger.info("📡 Service will listen on http://127.0.0.1:5006")
    logger.info("🎯 Available endpoints:")
    logger.info("   POST /process_command - Process commands from Unity UI")
    logger.info("   GET /health - Health check")
    logger.info("   GET / - Service information")
    logger.info("-" * 50)

    app.run(host='127.0.0.1', port=5006, debug=False, threaded=True)

if __name__ == "__main__":
    try:
        run_server()
    except KeyboardInterrupt:
        logger.info("👋 HTTP service stopped by user")
    except Exception as e:
        logger.error(f"❌ Fatal error: {e}")
        raise
