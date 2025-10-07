#!/usr/bin/env python3
"""
Comprehensive test suite for Drone Simulation System
Tests Unity integration, LLM processing, and Whisper voice recognition
"""

import sys
import os
import time
import requests
import json
import subprocess
from pathlib import Path
from typing import Dict, List, Optional

class DroneSystemTester:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.test_results = []
        self.unavailable_tests = []

    def print_header(self, title: str):
        """Print a formatted header"""
        print("\n" + "="*60)
        print(f"🧪 {title}")
        print("="*60)

    def log_test(self, test_name: str, success: bool, message: str = ""):
        """Log a test result"""
        status = "✅ PASS" if success else "❌ FAIL"
        self.test_results.append({
            'name': test_name,
            'success': success,
            'message': message
        })
        print(f"{status}: {test_name}")
        if message:
            print(f"      {message}")

    def test_dependencies(self):
        """Test if all required dependencies are available"""
        self.print_header("DEPENDENCY TESTS")

        # Test Python packages
        packages = ['flask', 'requests', 'ollama', 'whisper']
        for package in packages:
            try:
                __import__(package)
                self.log_test(f"Python package: {package}", True)
            except ImportError:
                self.log_test(f"Python package: {package}", False, "Install with: pip install -r requirements.txt")
                return False

        # Test ffmpeg
        try:
            result = subprocess.run(['ffmpeg', '-version'], capture_output=True, text=True, timeout=5)
            if result.returncode == 0:
                self.log_test("ffmpeg availability", True)
            else:
                raise FileNotFoundError
        except (FileNotFoundError, subprocess.TimeoutExpired):
            self.log_test("ffmpeg availability", False, "Install with: brew install ffmpeg")
            return False

        return True

    def test_ollama_service(self):
        """Test Ollama service availability and model"""
        self.print_header("OLLAMA SERVICE TESTS")

        try:
            # Test Ollama API
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            if response.status_code == 200:
                self.log_test("Ollama API connection", True)

                # Check for Llama2 model
                models = response.json().get('models', [])
                model_names = [m['name'] for m in models]
                has_llama2 = any('llama2' in name for name in model_names)
                self.log_test("Llama2 model availability", has_llama2,
                            "Model found" if has_llama2 else "Run: ollama pull llama2")
                return has_llama2
            else:
                self.log_test("Ollama API connection", False, f"Status: {response.status_code}")
                return False

        except requests.exceptions.RequestException as e:
            self.log_test("Ollama API connection", False, f"Cannot connect: {e}")
            self.unavailable_tests.append("All Ollama-dependent tests")
            return False

    def test_llm_service(self):
        """Test LLM HTTP service"""
        self.print_header("LLM SERVICE TESTS")

        try:
            # Test health endpoint
            response = requests.get("http://127.0.0.1:5006/health", timeout=5)
            if response.status_code == 200:
                health = response.json()
                self.log_test("LLM service health", True)

                # Check components
                ollama_ok = health.get('ollama_available', False)
                whisper_ok = health.get('whisper_available', False)

                self.log_test("Ollama integration", ollama_ok)
                self.log_test("Whisper integration", whisper_ok)

                return ollama_ok and whisper_ok
            else:
                self.log_test("LLM service health", False, f"Status: {response.status_code}")
                return False

        except requests.exceptions.RequestException as e:
            self.log_test("LLM service connection", False, f"Cannot connect: {e}")
            self.unavailable_tests.append("All LLM service tests")
            return False

    def test_text_commands(self):
        """Test text command processing"""
        if "All LLM service tests" in self.unavailable_tests:
            self.log_test("Text command processing", False, "LLM service not available")
            return False

        self.print_header("TEXT COMMAND TESTS")

        test_commands = [
            ("fly forward", ["move_forward"]),
            ("go up", ["ascend"]),
            ("turn left", ["turn_left"]),
            ("stop", ["stop"]),
            ("go back", ["move_backward"]),
            ("descend", ["descend"]),
            ("fly forward and go up", ["move_forward", "ascend"]),
            ("turn left and move forward", ["turn_left", "move_forward"]),
            ("go up and turn right", ["ascend", "turn_right"])
        ]

        success_count = 0

        for input_cmd, expected_output in test_commands:
            try:
                payload = {"command": input_cmd}
                response = requests.post("http://127.0.0.1:5006/process_command",
                                       json=payload, timeout=10)

                if response.status_code == 200:
                    result = response.json()
                    processed_commands = result.get('commands', [])
                    if processed_commands == expected_output:
                        self.log_test(f"Text: '{input_cmd}'", True, f"→ {processed_commands}")
                        success_count += 1
                    else:
                        self.log_test(f"Text: '{input_cmd}'", False,
                                    f"Expected: {expected_output}, Got: {processed_commands}")
                else:
                    self.log_test(f"Text: '{input_cmd}'", False,
                                f"HTTP {response.status_code}: {response.text}")

            except Exception as e:
                self.log_test(f"Text: '{input_cmd}'", False, f"Error: {e}")

            time.sleep(0.2)  # Small delay between tests

        overall_success = success_count == len(test_commands)
        self.log_test("Text command processing", overall_success,
                     f"{success_count}/{len(test_commands)} commands processed correctly")
        return overall_success

    def test_voice_commands(self):
        """Test voice command processing with mock audio"""
        if "All LLM service tests" in self.unavailable_tests:
            self.log_test("Voice command processing", False, "LLM service not available")
            return False

        self.print_header("VOICE COMMAND TESTS")

        # Create a simple test WAV file (1 second of silence)
        test_wav_path = self.project_root / "test_audio.wav"

        try:
            # Generate test audio file using ffmpeg
            subprocess.run([
                'ffmpeg', '-f', 'lavfi', '-i', 'sine=frequency=1000:duration=1',
                '-ar', '44100', '-ac', '1', str(test_wav_path)
            ], capture_output=True, timeout=10)

            # Test voice processing endpoint
            with open(test_wav_path, 'rb') as audio_file:
                files = {'audio': ('test.wav', audio_file, 'audio/wav')}
                response = requests.post("http://127.0.0.1:5006/process_audio_command",
                                       files=files, timeout=30)

            if response.status_code == 200:
                result = response.json()
                transcript = result.get('transcript', '')
                confidence = result.get('confidence', 0)

                if transcript and confidence > 0:
                    self.log_test("Voice processing", True,
                                f"Transcript: '{transcript}' (confidence: {confidence:.2f})")
                    return True
                else:
                    self.log_test("Voice processing", False,
                                f"Empty transcript or low confidence: {result}")
                    return False
            else:
                self.log_test("Voice processing", False,
                            f"HTTP {response.status_code}: {response.text}")
                return False

        except subprocess.CalledProcessError as e:
            self.log_test("Voice processing", False, f"Failed to create test audio: {e}")
            return False
        except Exception as e:
            self.log_test("Voice processing", False, f"Error: {e}")
            return False
        finally:
            # Clean up test file
            if test_wav_path.exists():
                test_wav_path.unlink()

    def test_unity_integration(self):
        """Test Unity HTTP server integration (mock)"""
        self.print_header("UNITY INTEGRATION TESTS")

        # Note: This would require Unity to be running
        # For now, we'll just test that the endpoint structure is correct

        test_commands = ["move_forward", "ascend", "turn_left", "stop"]

        for cmd in test_commands:
            try:
                payload = {"command": cmd}
                response = requests.post("http://127.0.0.1:5005/receive_command",
                                       json=payload, timeout=5)

                # Unity might not be running, so we check the error type
                if response.status_code == 200:
                    self.log_test(f"Unity command: {cmd}", True, "Unity responded")
                else:
                    self.log_test(f"Unity command: {cmd}", False,
                                f"Unity not running (HTTP {response.status_code})")

            except requests.exceptions.ConnectionError:
                self.log_test(f"Unity command: {cmd}", False, "Unity not running")
            except Exception as e:
                self.log_test(f"Unity command: {cmd}", False, f"Error: {e}")

            time.sleep(0.1)

        self.log_test("Unity integration", False,
                     "Unity must be running for these tests to pass")

    def run_all_tests(self):
        """Run all test suites"""
        print("🚁 DRONE SIMULATION SYSTEM TEST SUITE")
        print("Testing all components: Dependencies → Services → Commands → Integration")
        print("="*80)

        # Run test suites
        self.test_dependencies()
        ollama_ok = self.test_ollama_service()
        llm_ok = self.test_llm_service()

        if ollama_ok and llm_ok:
            self.test_text_commands()
            self.test_voice_commands()

        self.test_unity_integration()

        # Print summary
        self.print_summary()

    def print_summary(self):
        """Print comprehensive test summary"""
        self.print_header("TEST SUMMARY")

        total_tests = len(self.test_results)
        passed_tests = len([t for t in self.test_results if t['success']])
        failed_tests = total_tests - passed_tests

        print(f"📊 Total Tests Run: {total_tests}")
        print(f"✅ Passed: {passed_tests}")
        print(f"❌ Failed: {failed_tests}")

        if self.unavailable_tests:
            print(f"\n⚠️ Unavailable Test Groups: {len(self.unavailable_tests)}")
            for test in self.unavailable_tests:
                print(f"   • {test}")

        if failed_tests > 0:
            print("\n❌ Failed Tests:")
            for test in self.test_results:
                if not test['success']:
                    print(f"   • {test['name']}: {test['message']}")

        print("\n🎯 Test Results:")
        if failed_tests == 0:
            print("   🎉 ALL TESTS PASSED! System is ready for use.")
        elif failed_tests < total_tests * 0.3:  # Less than 30% failure
            print("   ⚠️ MOST TESTS PASSED - Minor issues detected.")
        else:
            print("   ❌ SIGNIFICANT ISSUES - Check setup and dependencies.")

        print("\n💡 Quick Fixes:")
        print("   • Missing dependencies: pip install -r requirements.txt")
        print("   • Ollama not running: ollama serve")
        print("   • No Llama2: ollama pull llama2")
        print("   • ffmpeg missing: brew install ffmpeg")
        print("   • Services down: python start.py")

def main():
    """Main test runner"""
    tester = DroneSystemTester()
    tester.run_all_tests()

    # Return appropriate exit code
    failed_tests = len([t for t in tester.test_results if not t['success']])
    return 0 if failed_tests == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
