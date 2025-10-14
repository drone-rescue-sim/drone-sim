#!python
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
import platform
from pathlib import Path

class DroneServices:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.llm_process = None
        self.ollama_process = None
        self.tobii_process = None

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
            self.print_status(" Python dependencies OK")
        except ImportError as e:
            self.print_status(f" Missing Python dependency: {e}", "red")
            self.print_status("Install with: pip install -r requirements.txt", "yellow")
            return False

        # Check ffmpeg
        try:
            result = subprocess.run(['ffmpeg', '-version'], capture_output=True, text=True)
            if result.returncode == 0:
                self.print_status(" ffmpeg available")
            else:
                raise FileNotFoundError
        except FileNotFoundError:
            self.print_status(" ffmpeg not found", "red")

            install_msg = ""
            if platform.system() == "Darwin":   # macOS
                install_msg = "brew install ffmpeg"
            elif platform.system() == "Windows":
                install_msg = "choco install ffmpeg   (eller last ned fra https://ffmpeg.org/download.html)"
            else:  # Linux
                install_msg = "sudo apt-get install ffmpeg"

            self.print_status(f"Install with: {install_msg}", "yellow")
            return False

        return True

    def free_port(self, port: int) -> None:
        """Force free a local TCP port if occupied (best effort)."""
        self.print_status(f"Ensuring port {port} is free...")
        # 1) Try macOS/Unix lsof
        try:
            result = subprocess.run(
                ["lsof", f"-tiTCP:{port}", "-sTCP:LISTEN"],
                capture_output=True,
                text=True,
            )
            pids = [p.strip() for p in result.stdout.splitlines() if p.strip()]
            for pid in pids:
                try:
                    os.kill(int(pid), signal.SIGTERM)
                    time.sleep(0.3)
                    # If still alive, SIGKILL
                    try:
                        os.kill(int(pid), 0)
                        os.kill(int(pid), signal.SIGKILL)
                    except OSError:
                        pass
                except Exception:
                    pass
            if pids:
                self.print_status(f" Killed processes on port {port}: {', '.join(pids)}")
        except FileNotFoundError:
            # 2) Try Linux fuser (if available)
            try:
                subprocess.run(["fuser", "-k", f"{port}/tcp"], capture_output=True, text=True)
                self.print_status(f" Freed port {port} via fuser")
            except Exception:
                pass

    def start_ollama(self):
        """Start Ollama service"""
        self.print_status("Starting Ollama service...")

        try:
            # Check if Ollama is already running
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            if response.status_code == 200:
                self.print_status(" Ollama already running")
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
                self.print_status(" Ollama started successfully")
                return True
            else:
                self.print_status(" Ollama failed to start properly", "red")
                return False

        except FileNotFoundError:
            self.print_status(" Ollama not installed", "red")
            self.print_status("Install from: https://ollama.ai/download", "yellow")
            return False
        except Exception as e:
            self.print_status(f" Failed to start Ollama: {e}", "red")
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
                    self.print_status(" Llama2 model available")
                    return True

            # Pull model
            self.print_status(" Pulling Llama2 model (this may take a few minutes)...")
            subprocess.run(['ollama', 'pull', 'llama2'], check=True)
            self.print_status("Llama2 model ready")
            return True

        except subprocess.CalledProcessError:
            self.print_status(" Failed to pull Llama2 model", "red")
            return False
        except Exception as e:
            self.print_status(f" Error checking model: {e}", "red")
            return False

    def start_llm_service(self):
        """Start the LLM HTTP service"""
        self.print_status("Starting LLM HTTP Service...")

        # Proactively free the LLM port to avoid address-in-use errors
        self.free_port(5006)

        # Start LLM service
        try:
            os.chdir(self.project_root / "services" / "llm")
            self.llm_process = subprocess.Popen([sys.executable, 'http_service.py'])

            # Poll health endpoint for up to 30 seconds (Flask debug reloader on Windows can delay readiness)
            start_time = time.time()
            last_error = None
            while time.time() - start_time < 30:
                try:
                    response = requests.get("http://127.0.0.1:5006/health", timeout=2)
                    if response.status_code == 200:
                        health = response.json()
                        self.print_status(" LLM HTTP Service started")
                        self.print_status(f"   Ollama: {'success' if health.get('ollama_available') else 'x'}")
                        self.print_status(f"    Whisper: {'success' if health.get('whisper_available') else 'x'}")
                        return True
                except Exception as e:
                    last_error = e

                # If the child process exited, abort early
                if self.llm_process.poll() is not None:
                    break

                time.sleep(1)

            self.print_status(" LLM service health check failed to become ready in time", "red")
            if last_error:
                self.print_status(f"  Last error: {last_error}", "yellow")
            return False

        except Exception as e:
            self.print_status(f" Failed to start LLM service: {e}", "red")
            return False
        finally:
            os.chdir(self.project_root)

    def start_tobii_service(self):
        """Start the Tobii HTTP service"""
        self.print_status("Starting Tobii HTTP Service...")
        try:
            # Quick import check in this interpreter for clearer error message
            try:
                import tobii_research  # noqa: F401
            except Exception as e:
                self.print_status(" Tobii SDK not available: install 'tobii_research'", "red")
                self.print_status(f"  Error: {e}", "yellow")
                return False

            os.chdir(self.project_root / "services" / "tobii")
            self.tobii_process = subprocess.Popen([sys.executable, 'http_service.py'])

            start_time = time.time()
            last_error = None
            while time.time() - start_time < 20:
                try:
                    r = requests.get("http://127.0.0.1:5007/health", timeout=2)
                    if r.status_code == 200:
                        self.print_status(" Tobii HTTP Service started")
                        return True
                except Exception as e:
                    last_error = e
                if self.tobii_process.poll() is not None:
                    break
                time.sleep(1)

            self.print_status(" Tobii service did not become ready in time", "red")
            if last_error:
                self.print_status(f"  Last error: {last_error}", "yellow")
            return False
        except Exception as e:
            self.print_status(f" Failed to start Tobii service: {e}", "red")
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
                self.print_status(" LLM service stopped")
            except:
                self.llm_process.kill()
                self.print_status(" LLM service force killed")

        if self.ollama_process:
            try:
                self.ollama_process.terminate()
                self.ollama_process.wait(timeout=5)
                self.print_status(" Ollama stopped")
            except:
                self.ollama_process.kill()
                self.print_status(" Ollama force killed")

        if self.tobii_process:
            try:
                self.tobii_process.terminate()
                self.tobii_process.wait(timeout=5)
                self.print_status(" Tobii service stopped")
            except:
                self.tobii_process.kill()
                self.print_status(" Tobii service force killed")

    def show_status(self):
        """Show current service status"""
        print("\n" + "="*50)
        self.print_status("Service Status", "blue")
        print("="*50)

        # Check Ollama
        try:
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=2)
            if response.status_code == 200:
                print(" Ollama:           RUNNING")
            else:
                print(" Ollama:           NOT RESPONDING")
        except:
            print(" Ollama:           NOT RUNNING")

        # Check LLM service
        try:
            response = requests.get("http://127.0.0.1:5006/health", timeout=2)
            if response.status_code == 200:
                health = response.json()
                print(" LLM Service:      RUNNING")
                print(f"   Ollama:       {'success' if health.get('ollama_available') else 'x'}")
                print(f"    Whisper:      {'success' if health.get('whisper_available') else 'x'}")
            else:
                print(" LLM Service:      NOT RESPONDING")
        except:
            print(" LLM Service:      NOT RUNNING")

        # Show URLs
        print("\n Service URLs:")
        print("   LLM Service:     http://127.0.0.1:5006")
        print("   Unity Control:   http://127.0.0.1:5005")
        print("   Ollama API:      http://127.0.0.1:11434")
        print("   Tobii Service:   http://127.0.0.1:5007")

    def run(self):
        """Main run function"""
        print("="*60)
        self.print_status(" DRONE SIMULATION SERVICES STARTUP", "blue")
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

        if success and not self.start_tobii_service():
            self.print_status(" Tobii service optional: continuing without it", "yellow")

        if success:
            print("\n" + "="*60)
            self.print_status(" ALL SERVICES STARTED SUCCESSFULLY!", "blue")
            print("="*60)

            self.show_status()

            print("\n Next Steps:")
            print("1. Open Unity and load your drone scene")
            print("2. Press TAB to open command interface")
            print("3. Type text commands or click for voice")
            print("4. Say commands like 'fly forward' or 'go up'")
            print("\n Press Ctrl+C to stop all services")

            # Keep running
            try:
                while True:
                    time.sleep(1)
            except KeyboardInterrupt:
                self.print_status("Shutting down services...")
                self.stop_services()
                self.print_status("All services stopped")

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
