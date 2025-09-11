#!/usr/bin/env python3
"""
Integration tests for Drone LLM Control System
Tests HTTP communication, command parsing, and LLM integration
"""

import requests
import time
import json
import subprocess
import sys
import os
from typing import Dict, List, Optional

# Test configuration
UNITY_URL = "http://127.0.0.1:5005/receive_command"
TEST_TIMEOUT = 5.0

class IntegrationTester:
    def __init__(self):
        self.test_results = []
        self.unavailable_tests = []

    def log_test(self, test_name: str, success: bool, message: str = ""):
        """Log a test result"""
        status = "✓ PASS" if success else "✗ FAIL"
        self.test_results.append({
            'name': test_name,
            'success': success,
            'message': message
        })
        print(f"{status}: {test_name}")
        if message:
            print(f"      {message}")

    def test_unity_connection(self) -> bool:
        """Test if Unity HTTP server is responding"""
        try:
            response = requests.get(UNITY_URL.replace('/receive_command', '/'), timeout=TEST_TIMEOUT)
            return response.status_code == 200
        except:
            return False

    def test_command_sending(self, command: str) -> bool:
        """Test sending a command to Unity"""
        try:
            payload = {"command": command}
            headers = {"Content-Type": "application/json"}
            response = requests.post(UNITY_URL, json=payload, headers=headers, timeout=TEST_TIMEOUT)
            return response.status_code == 200
        except:
            return False

    def test_multiple_commands(self, commands: List[str]) -> Dict[str, bool]:
        """Test multiple commands"""
        results = {}
        for cmd in commands:
            results[cmd] = self.test_command_sending(cmd)
            time.sleep(0.1)  # Small delay between commands
        return results

    def test_llm_service(self) -> bool:
        """Test if LLM service can be imported and basic functions work"""
        try:
            # Test import
            import ollama
            return True
        except ImportError:
            return False

    def run_communication_tests(self):
        """Run HTTP communication tests"""
        print("\n🔗 COMMUNICATION TESTS")
        print("=" * 50)

        # Test 1: Unity Connection
        unity_available = self.test_unity_connection()
        self.log_test("Unity HTTP Server Connection",
                     unity_available,
                     "Unity server is responding" if unity_available else "Unity server not available - start Unity first")

        if not unity_available:
            self.unavailable_tests.extend([
                "Command Sending Test",
                "Multiple Commands Test",
                "Integration Test"
            ])
            return

        # Test 2: Single Command
        command_success = self.test_command_sending("stop")
        self.log_test("Single Command Sending",
                     command_success,
                     "Successfully sent 'stop' command")

        # Test 3: Multiple Commands
        test_commands = ["move_forward", "turn_left", "ascend", "stop", "move_backward"]
        multi_results = self.test_multiple_commands(test_commands)

        all_passed = all(multi_results.values())
        failed_commands = [cmd for cmd, success in multi_results.items() if not success]

        self.log_test("Multiple Commands Test",
                     all_passed,
                     f"All commands sent successfully" if all_passed else f"Failed commands: {failed_commands}")

    def run_llm_tests(self):
        """Run LLM-related tests"""
        print("\n🧠 LLM TESTS")
        print("=" * 50)

        # Test 1: LLM Service Availability
        llm_available = self.test_llm_service()
        self.log_test("LLM Service Import",
                     llm_available,
                     "Ollama library available" if llm_available else "Ollama not installed")

        if not llm_available:
            self.unavailable_tests.append("LLM Command Generation Test")
            return

        # Test 2: Command Generation (requires LLM to be running)
        try:
            from main import get_drone_instructions

            test_inputs = [
                "fly forward",
                "go up",
                "turn left",
                "stop moving",
                "move to the right"
            ]

            valid_commands = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop"
            ]

            generated_commands = []
            for test_input in test_inputs:
                try:
                    cmd = get_drone_instructions(test_input)
                    generated_commands.append(cmd)
                    is_valid = cmd in valid_commands
                    self.log_test(f"LLM Command Generation: '{test_input}'",
                                 is_valid,
                                 f"Generated: {cmd}")
                except Exception as e:
                    self.log_test(f"LLM Command Generation: '{test_input}'",
                                 False,
                                 f"Error: {str(e)}")

        except ImportError:
            self.log_test("LLM Command Generation Test",
                         False,
                         "Cannot import main.py functions")

    def run_integration_tests(self):
        """Run full integration tests"""
        print("\n🔄 INTEGRATION TESTS")
        print("=" * 50)

        # Skip if Unity not available
        if not self.test_unity_connection():
            self.log_test("Full Integration Test",
                         False,
                         "Unity not available - cannot run integration test")
            return

        # Test LLM -> Unity pipeline
        try:
            from main import get_drone_instructions, send_to_unity

            test_instructions = [
                "fly forward",
                "go up",
                "turn right",
                "stop"
            ]

            success_count = 0
            for instruction in test_instructions:
                try:
                    # Generate command
                    command = get_drone_instructions(instruction)
                    if not command:
                        continue

                    # Send to Unity
                    success = send_to_unity(command)
                    if success:
                        success_count += 1

                    self.log_test(f"Integration: '{instruction}' -> {command}",
                                 success,
                                 "Complete pipeline success" if success else "Pipeline failed")

                    time.sleep(0.5)  # Delay between tests

                except Exception as e:
                    self.log_test(f"Integration: '{instruction}'",
                                 False,
                                 f"Error: {str(e)}")

            overall_success = success_count == len(test_instructions)
            self.log_test("Complete Integration Pipeline",
                         overall_success,
                         f"{success_count}/{len(test_instructions)} commands processed successfully")

        except ImportError:
            self.log_test("Integration Pipeline Test",
                         False,
                         "Cannot import required functions")

    def print_summary(self):
        """Print test summary"""
        print("\n📊 TEST SUMMARY")
        print("=" * 50)

        total_tests = len(self.test_results)
        passed_tests = len([t for t in self.test_results if t['success']])
        failed_tests = total_tests - passed_tests

        print(f"Total Tests: {total_tests}")
        print(f"Passed: {passed_tests}")
        print(f"Failed: {failed_tests}")

        if self.unavailable_tests:
            print(f"\n⚠️  Unavailable Tests: {len(self.unavailable_tests)}")
            for test in self.unavailable_tests:
                print(f"   - {test}")

        if failed_tests > 0:
            print(f"\n❌ Failed Tests:")
            for test in self.test_results:
                if not test['success']:
                    print(f"   - {test['name']}: {test['message']}")

        print(f"\n{'🎉 All tests passed!' if failed_tests == 0 else '⚠️  Some tests failed - check output above'}")

def main():
    """Main test runner"""
    print("🧪 DRONE INTEGRATION TEST SUITE")
    print("Testing LLM-Unity communication and command processing")
    print("=" * 60)

    tester = IntegrationTester()

    # Run all test suites
    tester.run_communication_tests()
    tester.run_llm_tests()
    tester.run_integration_tests()

    # Print summary
    tester.print_summary()

    # Return exit code based on results
    failed_tests = len([t for t in tester.test_results if not t['success']])
    return 0 if failed_tests == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
