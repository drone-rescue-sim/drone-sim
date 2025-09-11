#!/usr/bin/env python3
"""
Test script to verify Unity can connect to the Python HTTP service
"""

import requests
import time
import json

def test_llm_service():
    """Test the LLM HTTP service connection"""
    print("🔍 Testing LLM HTTP Service connection...")

    service_url = "http://127.0.0.1:5006"
    test_command = "test command"

    try:
        # Test health endpoint
        print(f"   Testing health endpoint: {service_url}/health")
        response = requests.get(f"{service_url}/health", timeout=5)
        if response.status_code == 200:
            print("   ✅ Health endpoint: OK")
            health_data = response.json()
            print(f"   📊 Service status: {health_data.get('status', 'unknown')}")
        else:
            print(f"   ❌ Health endpoint failed: {response.status_code}")
            return False

        # Test process command endpoint (like Unity would)
        print(f"\n   Testing command processing: {service_url}/process_command")
        payload = {"command": test_command}
        headers = {"Content-Type": "application/json"}

        response = requests.post(f"{service_url}/process_command",
                               json=payload, headers=headers, timeout=10)

        if response.status_code == 200:
            print("   ✅ Command processing: OK")
            result = response.json()
            print(f"   🤖 Processed command: {result.get('processed_command', 'none')}")
            print(f"   📊 Status: {result.get('status', 'unknown')}")
        else:
            print(f"   ❌ Command processing failed: {response.status_code}")
            print(f"   📝 Response: {response.text}")
            return False

        # Test root endpoint
        print(f"\n   Testing root endpoint: {service_url}/")
        response = requests.get(f"{service_url}/", timeout=5)
        if response.status_code == 200:
            print("   ✅ Root endpoint: OK")
        else:
            print(f"   ❌ Root endpoint failed: {response.status_code}")

        print("\n🎉 All tests passed! Unity should be able to connect.")
        return True

    except requests.exceptions.ConnectionError:
        print(f"   ❌ Cannot connect to {service_url}")
        print("   💡 Make sure the Python service is running:")
        print("      ./start_all_services.sh")
        print("      or: python start_services.py --wait")
        return False

    except requests.exceptions.Timeout:
        print(f"   ❌ Connection timeout to {service_url}")
        print("   💡 The service might be busy or slow to respond")
        return False

    except Exception as e:
        print(f"   ❌ Unexpected error: {e}")
        return False

def test_unity_simulation():
    """Simulate what Unity would do when sending commands"""
    print("\n🎮 Simulating Unity command flow...")

    service_url = "http://127.0.0.1:5006"
    test_commands = [
        "fly forward",
        "go up",
        "turn left",
        "stop"
    ]

    for command in test_commands:
        print(f"\n   Testing command: '{command}'")
        try:
            payload = {"command": command}
            headers = {"Content-Type": "application/json"}

            response = requests.post(f"{service_url}/process_command",
                                   json=payload, headers=headers, timeout=10)

            if response.status_code == 200:
                result = response.json()
                processed = result.get('processed_command', 'none')
                print(f"   ✅ '{command}' → {processed}")
            else:
                print(f"   ❌ '{command}' failed: {response.status_code}")

        except Exception as e:
            print(f"   ❌ '{command}' error: {e}")

        time.sleep(0.5)  # Small delay between commands

def main():
    print("=" * 50)
    print("🧪 UNITY CONNECTION TEST")
    print("=" * 50)

    print("This script tests if Unity can connect to the Python LLM service.")
    print("Make sure the Python service is running before running this test.\n")

    # Test basic connection
    if test_llm_service():
        # If basic tests pass, test with actual commands
        test_unity_simulation()
        print("\n" + "=" * 50)
        print("✅ CONNECTION TEST COMPLETE")
        print("Unity should work perfectly with your setup!")
        print("=" * 50)
    else:
        print("\n" + "=" * 50)
        print("❌ CONNECTION TEST FAILED")
        print("Please start the Python service and try again:")
        print("   ./start_all_services.sh")
        print("=" * 50)

if __name__ == "__main__":
    main()
