#!/usr/bin/env python3
"""
Setup script for Unity UI Command Integration

This script helps set up the Unity UI command system that replaces the terminal-based
command input with an in-game popup interface.
"""

import os
import sys
import subprocess
import time

def print_header():
    print("=" * 60)
    print("🤖 DRONE SIMULATION - UNITY UI COMMAND INTEGRATION")
    print("=" * 60)
    print()

def check_requirements():
    """Check if all required components are available."""
    print("🔍 Checking requirements...")

    # Check if Python packages are installed
    try:
        import flask
        import flask_cors
        import ollama
        import requests
        print("✅ Python dependencies are installed")
    except ImportError as e:
        print(f"❌ Missing Python dependency: {e}")
        print("   Run: pip install flask flask-cors ollama requests")
        return False

    # Check if Ollama is running
    try:
        import ollama
        ollama.list()
        print("✅ Ollama service is running")
    except Exception as e:
        print(f"❌ Ollama service not available: {e}")
        print("   Make sure Ollama is installed and running")
        return False

    return True

def test_python_service():
    """Test the Python HTTP service."""
    print("\n🧪 Testing Python LLM HTTP Service...")

    try:
        from services.llm.http_service import app

        # Test the service startup
        print("✅ HTTP service module loads successfully")

        # Test health endpoint
        with app.test_client() as client:
            response = client.get('/health')
            if response.status_code == 200:
                print("✅ Health endpoint responds correctly")
            else:
                print(f"❌ Health endpoint failed: {response.status_code}")
                return False

        return True

    except Exception as e:
        print(f"❌ Failed to test HTTP service: {e}")
        return False

def create_startup_script():
    """Create a startup script for the Python service."""
    startup_script = """#!/bin/bash
# Drone LLM HTTP Service Startup Script

echo "🤖 Starting Drone LLM HTTP Service..."
echo "📡 Service will be available at http://127.0.0.1:5006"
echo ""

# Navigate to the correct directory
cd "$(dirname "$0")"

# Activate virtual environment if it exists
if [ -f "venv/bin/activate" ]; then
    source venv/bin/activate
    echo "✅ Virtual environment activated"
fi

# Start the HTTP service
python services/llm/http_service.py
"""

    with open('start_llm_service.sh', 'w') as f:
        f.write(startup_script)

    # Make it executable
    os.chmod('start_llm_service.sh', 0o755)

    print("✅ Startup script created: start_llm_service.sh")

def print_instructions():
    """Print setup and usage instructions."""
    print("\n📋 SETUP INSTRUCTIONS:")
    print("=" * 40)
    print()
    print("1. 🐍 PYTHON SERVICE SETUP:")
    print("   a) Make sure Ollama is running with llama2 model")
    print("   b) Start the LLM HTTP service:")
    print("      ./start_llm_service.sh")
    print("      or: python services/llm/http_service.py")
    print()
    print("2. 🎮 UNITY SETUP:")
    print("   a) Open your Unity project")
    print("   b) In the SampleScene, create an empty GameObject")
    print("   c) Add the 'CommandUIManager' script to it")
    print("   d) Make sure TextMeshPro is installed in Unity")
    print("   e) Run the scene")
    print()
    print("3. 🎯 USAGE:")
    print("   - Press TAB to open/close the command input popup")
    print("   - Type natural language commands (e.g., 'fly forward', 'go up')")
    print("   - Press Enter or click Send to execute commands")
    print("   - Press Escape to close the popup")
    print("   - View command history in the scrollable area")
    print()
    print("4. 🔧 TROUBLESHOOTING:")
    print("   - If Unity can't connect: Check that Python service is running on port 5006")
    print("   - If commands don't work: Check that Unity HTTP server is running on port 5005")
    print("   - Check console logs in both Unity and Python for error messages")
    print()

def main():
    print_header()

    if not check_requirements():
        print("❌ Requirements check failed. Please fix the issues above.")
        sys.exit(1)

    if not test_python_service():
        print("❌ Python service test failed.")
        sys.exit(1)

    create_startup_script()
    print_instructions()

    print("🎉 Setup complete! Follow the instructions above to get started.")
    print()
    print("💡 Pro tip: Keep both Unity and the Python service running simultaneously")
    print("   for the best experience.")

if __name__ == "__main__":
    main()
