#!/usr/bin/env python3
"""
Drone Simulation Services Startup Script
Starts both LLM service and Whisper integration for voice control
"""

import subprocess
import sys
import os
import time
import signal
import requests
from pathlib import Path

class DroneServices:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.llm_process = None
        self.ollama_process = None

    def print_status(self, message: str, color: str = "green"):
        """Print colored status message"""
        colors = {
            "green": "\033[0;32m",
            "yellow": "\033[1;33m",
            "red": "\033[0;31m",
            "blue": "\033[0;34m",
            "nc": "\033[0m"
        }
        print(f"{colors[color]}[INFO]{colors['nc']} {message}")

    def check_dependencies(self):
        """Check if all required dependencies are installed"""
        self.print_status("Checking dependencies...")

        # Check Python packages
        try:
            import flask
            import requests
            import ollama
            import whisper
            self.print_status("✅ Python dependencies OK")
        except ImportError as e:
            self.print_status(f"❌ Missing Python dependency: {e}", "red")
            self.print_status("Install with: pip install -r requirements.txt", "yellow")
            return False

        # Check ffmpeg
        try:
            result = subprocess.run(['ffmpeg', '-version'], capture_output=True, text=True)
            if result.returncode == 0:
                self.print_status("✅ ffmpeg available")
            else:
                raise FileNotFoundError
        except FileNotFoundError:
            self.print_status("❌ ffmpeg not found", "red")
            self.print_status("Install with: brew install ffmpeg", "yellow")
            return False

        return True

    def start_ollama(self):
        """Start Ollama service"""
        self.print_status("Starting Ollama service...")

        try:
            # Check if Ollama is already running
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            if response.status_code == 200:
                self.print_status("✅ Ollama already running")
                return True
        except:
            pass

        try:
            # Start Ollama
            self.ollama_process = subprocess.Popen(
                ['ollama', 'serve'],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE
            )
            time.sleep(3)  # Wait for Ollama to start

            # Verify it's running
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            if response.status_code == 200:
                self.print_status("✅ Ollama started successfully")
                return True
            else:
                self.print_status("❌ Ollama failed to start properly", "red")
                return False

        except FileNotFoundError:
            self.print_status("❌ Ollama not installed", "red")
            self.print_status("Install from: https://ollama.ai/download", "yellow")
            return False
        except Exception as e:
            self.print_status(f"❌ Failed to start Ollama: {e}", "red")
            return False

    def pull_llama_model(self):
        """Pull Llama2 model if not available"""
        self.print_status("Checking Llama2 model...")

        try:
            # Check if model exists
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            if response.status_code == 200:
                models = response.json().get('models', [])
                model_names = [m['name'] for m in models]
                if 'llama2' in ' '.join(model_names):
                    self.print_status("✅ Llama2 model available")
                    return True

            # Pull model
            self.print_status("📥 Pulling Llama2 model (this may take a few minutes)...")
            subprocess.run(['ollama', 'pull', 'llama2'], check=True)
            self.print_status("✅ Llama2 model ready")
            return True

        except subprocess.CalledProcessError:
            self.print_status("❌ Failed to pull Llama2 model", "red")
            return False
        except Exception as e:
            self.print_status(f"❌ Error checking model: {e}", "red")
            return False

    def start_llm_service(self):
        """Start the LLM HTTP service"""
        self.print_status("Starting LLM HTTP Service...")

        # Check if port 5006 is available
        import socket
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            try:
                s.bind(('', 5006))
            except OSError:
                self.print_status("❌ Port 5006 already in use", "red")
                self.print_status("Stop other services or kill process on port 5006", "yellow")
                return False

        # Start LLM service
        try:
            os.chdir(self.project_root / "services" / "llm")
            self.llm_process = subprocess.Popen(
                [sys.executable, 'http_service.py'],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE
            )

            # Wait for service to start
            time.sleep(3)

            # Test health endpoint
            response = requests.get("http://127.0.0.1:5006/health", timeout=5)
            if response.status_code == 200:
                health = response.json()
                self.print_status("✅ LLM HTTP Service started")
                self.print_status(f"   📊 Ollama: {'✅' if health.get('ollama_available') else '❌'}")
                self.print_status(f"   🎤 Whisper: {'✅' if health.get('whisper_available') else '❌'}")
                return True
            else:
                self.print_status("❌ LLM service health check failed", "red")
                return False

        except Exception as e:
            self.print_status(f"❌ Failed to start LLM service: {e}", "red")
            return False
        finally:
            os.chdir(self.project_root)

    def stop_services(self):
        """Stop all running services"""
        self.print_status("Stopping services...")

        if self.llm_process:
            try:
                self.llm_process.terminate()
                self.llm_process.wait(timeout=5)
                self.print_status("✅ LLM service stopped")
            except:
                self.llm_process.kill()
                self.print_status("⚠️ LLM service force killed")

        if self.ollama_process:
            try:
                self.ollama_process.terminate()
                self.ollama_process.wait(timeout=5)
                self.print_status("✅ Ollama stopped")
            except:
                self.ollama_process.kill()
                self.print_status("⚠️ Ollama force killed")

    def show_status(self):
        """Show current service status"""
        print("\n" + "="*50)
        self.print_status("Service Status", "blue")
        print("="*50)

        # Check Ollama
        try:
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=2)
            if response.status_code == 200:
                print("🧠 Ollama:          ✅ RUNNING")
            else:
                print("🧠 Ollama:          ❌ NOT RESPONDING")
        except:
            print("🧠 Ollama:          ❌ NOT RUNNING")

        # Check LLM service
        try:
            response = requests.get("http://127.0.0.1:5006/health", timeout=2)
            if response.status_code == 200:
                health = response.json()
                print("🤖 LLM Service:     ✅ RUNNING")
                print(f"   📊 Ollama:       {'✅' if health.get('ollama_available') else '❌'}")
                print(f"   🎤 Whisper:      {'✅' if health.get('whisper_available') else '❌'}")
            else:
                print("🤖 LLM Service:     ❌ NOT RESPONDING")
        except:
            print("🤖 LLM Service:     ❌ NOT RUNNING")

        # Show URLs
        print("\n📡 Service URLs:")
        print("   LLM Service:     http://127.0.0.1:5006")
        print("   Unity Control:   http://127.0.0.1:5005")
        print("   Ollama API:      http://127.0.0.1:11434")

    def run(self):
        """Main run function"""
        print("="*60)
        self.print_status("🚁 DRONE SIMULATION SERVICES STARTUP", "blue")
        print("="*60)
        print("Starting LLM service with Whisper voice integration")
        print()

        # Check dependencies
        if not self.check_dependencies():
            return False

        # Start services
        success = True

        if not self.start_ollama():
            success = False

        if success and not self.pull_llama_model():
            success = False

        if success and not self.start_llm_service():
            success = False

        if success:
            print("\n" + "="*60)
            self.print_status("🎉 ALL SERVICES STARTED SUCCESSFULLY!", "blue")
            print("="*60)

            self.show_status()

            print("\n🚀 Next Steps:")
            print("1. Open Unity and load your drone scene")
            print("2. Press TAB to open command interface")
            print("3. Type text commands or click 🎤 for voice")
            print("4. Say commands like 'fly forward' or 'go up'")
            print("\n⚡ Press Ctrl+C to stop all services")

            # Keep running
            try:
                while True:
                    time.sleep(1)
            except KeyboardInterrupt:
                self.print_status("Shutting down services...")
                self.stop_services()
                self.print_status("✅ All services stopped")

        return success

def main():
    """Main entry point"""
    # Handle command line arguments
    if len(sys.argv) > 1:
        command = sys.argv[1]

        if command == "stop":
            services = DroneServices()
            services.stop_services()
            return
        elif command == "status":
            services = DroneServices()
            services.show_status()
            return
        elif command == "restart":
            services = DroneServices()
            services.stop_services()
            time.sleep(2)
            services.run()
            return
        else:
            print("Usage: python start.py [start|stop|status|restart]")
            return

    # Default: start services
    services = DroneServices()
    success = services.run()
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()
