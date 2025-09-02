#!/usr/bin/env python3
"""
Simple test script that works without Unity or Ollama.
This demonstrates the core functionality of the drone system.
"""

import json
import time
from typing import Dict, Any

class MockLLM:
    """Mock LLM that simulates the Ollama responses"""

    def __init__(self):
        self.command_map = {
            "fly forward": "move_forward",
            "move forward": "move_forward",
            "go forward": "move_forward",
            "forward": "move_forward",

            "fly backward": "move_backward",
            "move backward": "move_backward",
            "go backward": "move_backward",
            "backward": "move_backward",

            "fly left": "move_left",
            "move left": "move_left",
            "go left": "move_left",
            "left": "move_left",

            "fly right": "move_right",
            "move right": "move_right",
            "go right": "move_right",
            "right": "move_right",

            "fly up": "ascend",
            "go up": "ascend",
            "ascend": "ascend",
            "up": "ascend",

            "fly down": "descend",
            "go down": "descend",
            "descend": "descend",
            "down": "descend",

            "turn left": "turn_left",
            "rotate left": "turn_left",

            "turn right": "turn_right",
            "rotate right": "turn_right",

            "stop": "stop",
            "stop moving": "stop",
            "halt": "stop"
        }

    def process_command(self, user_input: str) -> Dict[str, Any]:
        """Process user input and return drone command"""
        input_lower = user_input.lower().strip()

        # Check for compound commands (e.g., "fly up and turn left")
        if " and " in input_lower:
            parts = input_lower.split(" and ")
            commands = []
            for part in parts:
                part = part.strip()
                if part in self.command_map:
                    commands.append(self.command_map[part])

            if commands:
                return {
                    "command": "; ".join(commands),
                    "details": f"Executing {len(commands)} commands: {', '.join(commands)}"
                }

        # Single command
        if input_lower in self.command_map:
            command = self.command_map[input_lower]
            return {
                "command": command,
                "details": f"Drone command: {command}"
            }

        # Fallback for unknown commands
        return {
            "command": "unknown",
            "details": f"Unknown command: {user_input}"
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

        # Split compound commands
        commands = command.split(';')

        responses = []
        for cmd in commands:
            cmd = cmd.strip()
            response = self._execute_single_command(cmd)
            responses.append(response)

        self.commands_executed.append({
            'timestamp': time.time(),
            'command': command,
            'responses': responses
        })

        return f"Executed: {', '.join(responses)}"

    def _execute_single_command(self, command: str) -> str:
        """Execute a single drone command"""
        speed = 5.0

        if command == "move_forward":
            self.velocity[2] += speed
            return "Moving forward"
        elif command == "move_backward":
            self.velocity[2] -= speed
            return "Moving backward"
        elif command == "move_left":
            self.velocity[0] -= speed
            return "Moving left"
        elif command == "move_right":
            self.velocity[0] += speed
            return "Moving right"
        elif command == "ascend":
            self.velocity[1] += speed
            return "Ascending"
        elif command == "descend":
            self.velocity[1] -= speed
            return "Descending"
        elif command == "turn_left":
            self.rotation[1] -= 45.0
            return "Turning left"
        elif command == "turn_right":
            self.rotation[1] += 45.0
            return "Turning right"
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
    """Complete drone simulation system"""

    def __init__(self):
        self.llm = MockLLM()
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
        """Run a predefined demo"""
        print("🚁 Drone Simulator - Demo Mode")
        print("-" * 50)

        demo_commands = [
            "fly forward",
            "turn left",
            "go up",
            "move right",
            "stop",
            "fly up and turn left",
            "stop moving"
        ]

        for command in demo_commands:
            self.process_user_command(command)
            time.sleep(0.5)  # Brief pause between commands

        print("\n🎬 Demo complete!")

def main():
    """Main function"""
    simulator = DroneSimulator()

    print("🚁 Drone Simulation System Test (No Unity/Ollama Required)")
    print("=" * 60)
    print("Choose mode:")
    print("1. Interactive mode (type your own commands)")
    print("2. Demo mode (watch predefined commands)")
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
