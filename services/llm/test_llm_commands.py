#!/usr/bin/env python3
"""
Test script for LLM command generation
Tests the LLM's ability to generate valid drone commands
"""

import sys
import os

def test_llm_commands():
    """Test LLM command generation with various inputs"""
    print("🧪 LLM COMMAND GENERATION TESTS")
    print("=" * 50)

    try:
        # Import the main functions
        from main import get_drone_instructions
        import ollama

        # Test cases with expected results
        test_cases = [
            ("fly forward", ["move_forward"]),
            ("go up", ["ascend", "go_up"]),
            ("move left", ["move_left"]),
            ("turn right", ["turn_right"]),
            ("stop moving", ["stop"]),
            ("go down", ["descend", "go_down"]),
            ("move backward", ["move_backward"]),
            ("hover", ["stop"]),
            ("fly higher", ["ascend", "go_up"]),
            ("rotate left", ["turn_left"]),
        ]

        valid_commands = [
            "move_forward", "move_backward", "move_left", "move_right",
            "ascend", "go_up", "descend", "go_down",
            "turn_left", "turn_right", "stop"
        ]

        results = []

        for test_input, expected in test_cases:
            try:
                print(f"\nTesting: '{test_input}'")
                command = get_drone_instructions(test_input)
                print(f"Generated: {command}")

                is_valid = command in valid_commands
                is_expected = command in expected

                if is_valid and is_expected:
                    print("✓ PASS: Valid and expected command")
                    results.append(True)
                elif is_valid and not is_expected:
                    print(f"✓ PARTIAL: Valid command but not expected (got {command}, expected {expected})")
                    results.append(True)  # Still valid
                else:
                    print(f"✗ FAIL: Invalid command '{command}'")
                    results.append(False)

            except Exception as e:
                print(f"✗ ERROR: {str(e)}")
                results.append(False)

        # Summary
        passed = sum(results)
        total = len(results)
        print("\n📊 RESULTS")
        print(f"Passed: {passed}/{total}")
        print(f"Success rate: {passed/total*100:.1f}%")

        return passed == total

    except ImportError as e:
        print(f"❌ Cannot import required modules: {e}")
        print("Make sure ollama is installed: pip install ollama")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return False

def main():
    """Main test function"""
    success = test_llm_commands()
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()
