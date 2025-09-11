#!/usr/bin/env python3
"""
Simple test script to verify Unity HTTP server connectivity
Run this while Unity is running to test the HTTP connection
"""

import requests
import time
import json

UNITY_URL = "http://127.0.0.1:5005/receive_command"

def test_unity_connection():
    """Test connection to Unity HTTP server"""
    print("🔗 Testing Unity HTTP Server Connection")
    print("=" * 50)

    try:
        # Test basic connectivity
        print("1. Testing basic connectivity...")
        response = requests.get("http://127.0.0.1:5005/", timeout=5.0)
        if response.status_code == 200:
            print("✓ Unity HTTP server is responding")
        else:
            print(f"✗ Unexpected response: {response.status_code}")
            return False

    except requests.exceptions.ConnectionError:
        print("✗ Cannot connect to Unity HTTP server")
        print("   Make sure Unity is running with the drone scene")
        return False
    except Exception as e:
        print(f"✗ Connection error: {e}")
        return False

    return True

def test_command_sending():
    """Test sending commands to Unity"""
    print("\n2. Testing command sending...")

    test_commands = [
        "move_forward",
        "turn_left",
        "ascend",
        "stop",
        "move_backward"
    ]

    success_count = 0

    for command in test_commands:
        try:
            payload = {"command": command}
            headers = {"Content-Type": "application/json"}

            print(f"   Sending: {command}")
            response = requests.post(UNITY_URL, json=payload, headers=headers, timeout=5.0)

            if response.status_code == 200:
                print(f"   ✓ {command} - Success")
                success_count += 1
            else:
                print(f"   ✗ {command} - Failed (status: {response.status_code})")

        except Exception as e:
            print(f"   ✗ {command} - Error: {e}")

        time.sleep(0.2)  # Small delay between commands

    print(f"\nCommand test results: {success_count}/{len(test_commands)} successful")
    return success_count == len(test_commands)

def main():
    """Main test function"""
    print("🧪 UNITY CONNECTION TEST")
    print("This script tests if Unity's HTTP server is working")
    print("Make sure Unity is running with the drone scene before running this test")
    print()

    # Test connection
    connection_ok = test_unity_connection()

    if connection_ok:
        # Test command sending
        commands_ok = test_command_sending()

        if commands_ok:
            print("\n🎉 All tests passed! Unity integration is working.")
            return True
        else:
            print("\n⚠️  Connection works but some commands failed.")
            return False
    else:
        print("\n❌ Cannot connect to Unity. Check that:")
        print("   1. Unity is running")
        print("   2. The drone scene is active")
        print("   3. The DroneController script is attached to the drone")
        return False

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
