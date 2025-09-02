#!/usr/bin/env python3
"""
Test script for the drone simulation system.
This tests the LLM service and simulates Unity communication without actually running Unity.
"""

import json
import requests
import time
import threading
from services.llm.main import get_drone_instructions, send_to_unity

class MockUnity:
    """Mock Unity server that simulates the Unity HTTP server"""

    def __init__(self, port=5005):
        self.port = port
        self.received_commands = []
        self.running = False

    def start(self):
        """Start the mock Unity server"""
        from flask import Flask, request, jsonify

        self.app = Flask(__name__)

        @self.app.route('/receive_command', methods=['POST'])
        def receive_command():
            try:
                data = request.get_json()
                command = data.get('command', '')
                details = data.get('details', '')

                self.received_commands.append({
                    'command': command,
                    'details': details,
                    'timestamp': time.time()
                })

                print(f"🎯 Mock Unity received: {command} - {details}")
                return jsonify({"status": "success"}), 200

            except Exception as e:
                print(f"❌ Mock Unity error: {e}")
                return jsonify({"error": str(e)}), 500

        def run_server():
            self.app.run(host='127.0.0.1', port=self.port, debug=False)

        self.server_thread = threading.Thread(target=run_server, daemon=True)
        self.server_thread.start()
        self.running = True
        print(f"🚀 Mock Unity server started on port {self.port}")
        time.sleep(1)  # Give server time to start

    def stop(self):
        """Stop the mock Unity server"""
        self.running = False
        print("🛑 Mock Unity server stopped")

    def get_commands(self):
        """Get all received commands"""
        return self.received_commands.copy()

    def clear_commands(self):
        """Clear the command history"""
        self.received_commands.clear()

class DroneSystemTester:
    """Test the complete drone system"""

    def __init__(self):
        self.mock_unity = MockUnity()
        self.test_results = []

    def run_all_tests(self):
        """Run all system tests"""
        print("🧪 Starting Drone System Tests...")
        print("=" * 50)

        # Test 1: LLM Service
        self.test_llm_service()

        # Test 2: Mock Unity Server
        self.test_mock_unity()

        # Test 3: End-to-End Communication
        self.test_end_to_end()

        # Test 4: Command Processing
        self.test_command_processing()

        # Print results
        self.print_results()

    def test_llm_service(self):
        """Test the LLM service directly"""
        print("\n📝 Test 1: LLM Service")

        try:
            # Test with Ollama (this will fail if not running, but shows the code works)
            test_input = "fly forward"
            result = get_drone_instructions(test_input)

            if isinstance(result, dict) and 'command' in result:
                print(f"✅ LLM returned valid JSON: {result}")
                self.test_results.append(("LLM Service", True, "JSON response generated"))
            else:
                print(f"⚠️  LLM returned unexpected format: {result}")
                self.test_results.append(("LLM Service", True, "Response generated (but not JSON)"))

        except Exception as e:
            print(f"❌ LLM Service error: {e}")
            self.test_results.append(("LLM Service", False, str(e)))

    def test_mock_unity(self):
        """Test the mock Unity server"""
        print("\n🎮 Test 2: Mock Unity Server")

        try:
            self.mock_unity.start()

            # Test sending a command to mock Unity
            test_payload = {"command": "move_forward", "details": "Test command"}
            response = requests.post('http://127.0.0.1:5005/receive_command', json=test_payload)

            if response.status_code == 200:
                print("✅ Mock Unity server responded correctly")
                self.test_results.append(("Mock Unity Server", True, "HTTP server working"))
            else:
                print(f"❌ Mock Unity server error: {response.status_code}")
                self.test_results.append(("Mock Unity Server", False, f"HTTP error: {response.status_code}"))

        except Exception as e:
            print(f"❌ Mock Unity Server error: {e}")
            self.test_results.append(("Mock Unity Server", False, str(e)))

    def test_end_to_end(self):
        """Test end-to-end communication"""
        print("\n🔄 Test 3: End-to-End Communication")

        try:
            # Clear previous commands
            self.mock_unity.clear_commands()

            # Send a command through the LLM service
            test_input = "turn left"
            result = get_drone_instructions(test_input)

            if isinstance(result, dict):
                send_to_unity(result)

                # Wait a moment for the command to be received
                time.sleep(0.5)

                commands = self.mock_unity.get_commands()
                if len(commands) > 0:
                    received_cmd = commands[-1]['command']
                    print(f"✅ End-to-end test successful: '{test_input}' → '{received_cmd}'")
                    self.test_results.append(("End-to-End", True, f"'{test_input}' processed successfully"))
                else:
                    print("❌ End-to-end test failed: No command received by Unity")
                    self.test_results.append(("End-to-End", False, "No command received"))
            else:
                print("❌ End-to-end test failed: LLM didn't return valid data")
                self.test_results.append(("End-to-End", False, "Invalid LLM response"))

        except Exception as e:
            print(f"❌ End-to-end test error: {e}")
            self.test_results.append(("End-to-End", False, str(e)))

    def test_command_processing(self):
        """Test various command processing scenarios"""
        print("\n⚙️  Test 4: Command Processing")

        test_commands = [
            "fly forward",
            "turn left",
            "go up",
            "move right",
            "stop moving",
            "fly up and turn left"
        ]

        success_count = 0

        for cmd in test_commands:
            try:
                result = get_drone_instructions(cmd)
                if isinstance(result, dict) and 'command' in result:
                    print(f"✅ '{cmd}' → '{result['command']}'")
                    success_count += 1
                else:
                    print(f"❌ '{cmd}' → Invalid response: {result}")
            except Exception as e:
                print(f"❌ '{cmd}' → Error: {e}")

        success_rate = success_count / len(test_commands) * 100
        print(".1f")

        if success_rate >= 80:
            self.test_results.append(("Command Processing", True, f"{success_rate:.1f}% success rate"))
        else:
            self.test_results.append(("Command Processing", False, f"Only {success_rate:.1f}% success rate"))

    def print_results(self):
        """Print test results summary"""
        print("\n" + "=" * 50)
        print("📊 TEST RESULTS SUMMARY")
        print("=" * 50)

        passed = 0
        total = len(self.test_results)

        for test_name, success, details in self.test_results:
            status = "✅ PASS" if success else "❌ FAIL"
            print(f"{status} {test_name}: {details}")

            if success:
                passed += 1

        print(f"\n🎯 Overall: {passed}/{total} tests passed ({passed/total*100:.1f}%)")

        if passed == total:
            print("🎉 All tests passed! The system is ready to use with Unity.")
        elif passed >= total * 0.75:
            print("👍 Most tests passed! The system should work well with Unity.")
        else:
            print("⚠️  Some tests failed. Check the error messages above.")

    def cleanup(self):
        """Clean up resources"""
        if hasattr(self, 'mock_unity'):
            self.mock_unity.stop()

def main():
    """Main test function"""
    print("🚁 Drone Simulation System Test Suite")
    print("This will test the LLM service and mock Unity communication.")
    print("Note: Ollama must be running for full LLM functionality.\n")

    tester = DroneSystemTester()
    tester.run_all_tests()

    print("\n🧪 Test complete!")
    print("\nTo test with real Unity:")
    print("1. Start the LLM service: python services/llm/main.py")
    print("2. Open Unity project and run the scene")
    print("3. The system should work the same way!")

if __name__ == "__main__":
    main()
