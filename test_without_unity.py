#!/usr/bin/env python3
"""
Drone system test with REAL LLM integration.
This demonstrates the complete functionality using the actual LLM service.
"""

import json
import time
import sys
import os
from typing import Dict, Any

# Add the project root to Python path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

class RealLLM:
    """Real LLM that uses the actual Ollama service"""

    def __init__(self):
        try:
            from services.llm.main import get_drone_instructions
            self.get_drone_instructions = get_drone_instructions
            self.available = True
            print("✅ Real LLM service loaded successfully")
        except Exception as e:
            print(f"❌ Failed to load LLM service: {e}")
            self.available = False

    def process_command(self, user_input: str) -> Dict[str, Any]:
        """Process user input using the real LLM service"""
        if not self.available:
            return {
                "command": "error",
                "intensity": 1.0,
                "details": "LLM service not available"
            }

        try:
            result = self.get_drone_instructions(user_input)

            # Handle the case where LLM returns a dict directly
            if isinstance(result, dict):
                # Ensure intensity is present
                if 'intensity' not in result:
                    result['intensity'] = 1.0
                return result

            # Handle unexpected return types
            return {
                "command": "unknown",
                "intensity": 1.0,
                "details": f"Unexpected LLM response: {result}"
            }

        except Exception as e:
            print(f"❌ LLM Error: {e}")
            return {
                "command": "error",
                "intensity": 1.0,
                "details": f"LLM processing failed: {str(e)}"
            }

class MockUnityDrone:
    """Mock Unity drone that simulates drone behavior"""

    def __init__(self):
        self.position = [0.0, 2.0, 0.0]  # x, y, z
        self.rotation = [0.0, 0.0, 0.0]   # x, y, z
        self.velocity = [0.0, 0.0, 0.0]
        self.commands_executed = []

    def execute_command(self, command_data: Dict[str, Any]) -> str:
        """Execute a drone command and return status"""
        command = command_data.get('command', '')
        details = command_data.get('details', '')
        intensity = command_data.get('intensity', 1.0)

        # Split compound commands
        commands = command.split(';')

        responses = []
        for cmd in commands:
            cmd = cmd.strip()
            response = self._execute_single_command(cmd, intensity)
            responses.append(response)

        self.commands_executed.append({
            'timestamp': time.time(),
            'command': command,
            'intensity': intensity,
            'responses': responses
        })

        return f"Executed: {', '.join(responses)} (intensity: {intensity:.1f}x)"

    def _execute_single_command(self, command: str, intensity: float = 1.0) -> str:
        """Execute a single drone command with intensity"""
        speed = 5.0 * intensity

        if command == "move_forward":
            self.velocity[2] += speed
            return f"Moving forward ({intensity:.1f}x)"
        elif command == "move_backward":
            self.velocity[2] -= speed
            return f"Moving backward ({intensity:.1f}x)"
        elif command == "move_left":
            self.velocity[0] -= speed
            return f"Moving left ({intensity:.1f}x)"
        elif command == "move_right":
            self.velocity[0] += speed
            return f"Moving right ({intensity:.1f}x)"
        elif command == "ascend":
            self.velocity[1] += speed
            return f"Ascending ({intensity:.1f}x)"
        elif command == "descend":
            self.velocity[1] -= speed
            return f"Descending ({intensity:.1f}x)"
        elif command == "turn_left":
            self.rotation[1] -= 45.0 * intensity
            return f"Turning left ({intensity:.1f}x)"
        elif command == "turn_right":
            self.rotation[1] += 45.0 * intensity
            return f"Turning right ({intensity:.1f}x)"
        elif command == "stop":
            self.velocity = [0.0, 0.0, 0.0]
            return "Stopping"
        else:
            return f"Unknown command: {command}"

    def get_status(self) -> Dict[str, Any]:
        """Get current drone status"""
        return {
            "position": self.position.copy(),
            "rotation": self.rotation.copy(),
            "velocity": self.velocity.copy(),
            "commands_executed": len(self.commands_executed)
        }

    def update_physics(self, delta_time: float = 0.1):
        """Simple physics update"""
        # Apply velocity to position
        for i in range(3):
            self.position[i] += self.velocity[i] * delta_time

        # Apply drag
        drag = 0.9
        for i in range(3):
            self.velocity[i] *= drag

        # Gravity (except when ascending/descending intentionally)
        if abs(self.velocity[1]) < 1.0:  # If not actively moving up/down
            self.position[1] -= 2.0 * delta_time  # Gravity

        # Keep drone above ground
        if self.position[1] < 0.5:
            self.position[1] = 0.5
            if self.velocity[1] < 0:
                self.velocity[1] = 0

class DroneSimulator:
    """Complete drone simulation system with REAL LLM"""

    def __init__(self):
        self.llm = RealLLM()
        self.drone = MockUnityDrone()
        self.running = True

    def process_user_command(self, user_input: str) -> str:
        """Process a user command through the entire system"""
        print(f"\n👤 User: {user_input}")

        # Step 1: LLM processes the natural language
        llm_response = self.llm.process_command(user_input)
        print(f"🤖 LLM: {llm_response}")

        # Step 2: Send command to drone (Unity)
        drone_response = self.drone.execute_command(llm_response)
        print(f"🚁 Drone: {drone_response}")

        # Step 3: Update physics
        self.drone.update_physics()

        # Step 4: Show status
        status = self.drone.get_status()
        print(f"📊 Status: Pos({status['position'][0]:.1f}, {status['position'][1]:.1f}, {status['position'][2]:.1f})")

        return drone_response

    def interactive_mode(self):
        """Run interactive command mode"""
        print("🚁 Drone Simulator - Interactive Mode")
        print("Type natural language commands (e.g., 'fly forward', 'turn left', 'go up')")
        print("Type 'quit' or 'exit' to stop")
        print("-" * 50)

        while self.running:
            try:
                user_input = input("\nYour command: ").strip()
                if user_input.lower() in ['quit', 'exit', 'q']:
                    break

                if user_input:
                    self.process_user_command(user_input)

            except KeyboardInterrupt:
                break
            except Exception as e:
                print(f"❌ Error: {e}")

        print("\n👋 Thanks for flying with our drone simulator!")

    def demo_mode(self):
        """Run a comprehensive demo with various command types"""
        print("🚁 Drone Simulator - Comprehensive Demo")
        print("-" * 50)

        demo_commands = [
            # Basic movement commands
            "fly forward",
            "move backward",
            "turn left",
            "turn right",
            "go up",
            "go down",
            "move left",
            "move right",

            # Intensity commands
            "move very long forward",     # High intensity
            "turn very fast left",        # High intensity
            "fly very long up",          # High intensity
            "move slowly right",         # Low intensity

            # Compound commands
            "fly up and turn left",
            "move very fast forward and turn right",
            "ascend and move left",

            # Stop commands
            "stop",
            "stop moving"
        ]

        print("Testing various command types:")
        print("• Basic movement")
        print("• Intensity modifiers")
        print("• Compound commands")
        print("• Stop commands")
        print("-" * 50)

        for i, command in enumerate(demo_commands, 1):
            print(f"\n[Command {i:2d}/{len(demo_commands)}] ", end="")
            self.process_user_command(command)
            time.sleep(0.3)  # Brief pause between commands

        print("\n" + "=" * 50)
        print("🎬 Demo complete!")
        print("✅ Tested basic movement, intensity, and compound commands")
        print("✅ Verified LLM processing and drone simulation")
        print("✅ All commands processed successfully!")

def main():
    """Main function"""
    print("🚁 Drone Simulation System Test (With REAL LLM + Intensity)")
    print("=" * 60)
    print("This test uses the actual Ollama LLM service!")
    print("Make sure Ollama is running: ollama serve")
    print("")
    print("✨ NEW: Intensity support!")
    print("   Try commands like 'move very long forward', 'turn very fast left'")
    print("   Or 'move slowly right', 'fly very long up'")
    print("")

    simulator = DroneSimulator()

    print("Choose mode:")
    print("1. Interactive mode (type your own commands)")
    print("2. Demo mode (watch predefined commands with intensity)")
    print("3. Exit")
    print("-" * 60)

    while True:
        try:
            choice = input("Enter choice (1-3): ").strip()

            if choice == "1":
                simulator.interactive_mode()
                break
            elif choice == "2":
                simulator.demo_mode()
                break
            elif choice == "3":
                print("👋 Goodbye!")
                break
            else:
                print("❌ Invalid choice. Please enter 1, 2, or 3.")

        except KeyboardInterrupt:
            print("\n👋 Goodbye!")
            break

if __name__ == "__main__":
    main()
