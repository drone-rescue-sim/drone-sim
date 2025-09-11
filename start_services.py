#!/usr/bin/env python3
"""
Drone Simulation Services Manager
Python script to start/stop all necessary services for Unity UI integration
"""

import os
import sys
import time
import signal
import subprocess
import requests
import psutil
import argparse
from pathlib import Path

class ServiceManager:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.llm_pid_file = self.project_root / "llm_service.pid"
        self.llm_log_file = self.project_root / "llm_service.log"
        self.llm_pid = None

    def print_status(self, message: str):
        print(f"[INFO] {message}")

    def print_warning(self, message: str):
        print(f"[WARNING] {message}")

    def print_error(self, message: str):
        print(f"[ERROR] {message}")

    def print_success(self, message: str):
        print(f"[SUCCESS] {message}")

    def check_port_in_use(self, port: int) -> bool:
        """Check if a port is in use"""
        try:
            import socket
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                return s.connect_ex(('127.0.0.1', port)) == 0
        except:
            return False

    def check_ollama(self) -> bool:
        """Check if Ollama is running"""
        try:
            response = requests.get("http://127.0.0.1:11434/api/tags", timeout=5)
            return response.status_code == 200
        except:
            return False

    def check_dependencies(self) -> bool:
        """Check Python dependencies"""
        self.print_status("Checking Python dependencies...")

        try:
            import flask
            import flask_cors
            import ollama
            import requests
            self.print_success("All Python dependencies are available")
            return True
        except ImportError as e:
            self.print_error(f"Missing dependency: {e}")
            self.print_status("Run: pip install -r requirements.txt")
            return False

    def start_llm_service(self) -> bool:
        """Start the LLM HTTP service"""
        self.print_status("Starting LLM HTTP Service on port 5006...")

        if self.check_port_in_use(5006):
            self.print_error("Port 5006 is already in use!")
            self.print_error("Please stop the service that's using port 5006")
            return False

        try:
            # Start the service
            process = subprocess.Popen([
                sys.executable, "services/llm/http_service.py"
            ], stdout=open(self.llm_log_file, 'w'),
               stderr=subprocess.STDOUT,
               cwd=self.project_root)

            self.llm_pid = process.pid

            # Wait a moment and check if it's still running
            time.sleep(2)
            if process.poll() is None:
                # Save PID
                with open(self.llm_pid_file, 'w') as f:
                    f.write(str(self.llm_pid))

                self.print_success(f"LLM HTTP Service started successfully (PID: {self.llm_pid})")
                return True
            else:
                self.print_error("Failed to start LLM HTTP Service")
                return False

        except Exception as e:
            self.print_error(f"Error starting LLM service: {e}")
            return False

    def stop_llm_service(self):
        """Stop the LLM HTTP service"""
        if self.llm_pid_file.exists():
            try:
                with open(self.llm_pid_file, 'r') as f:
                    pid = int(f.read().strip())

                if psutil.pid_exists(pid):
                    os.kill(pid, signal.SIGTERM)
                    time.sleep(1)
                    if psutil.pid_exists(pid):
                        os.kill(pid, signal.SIGKILL)
                    self.print_success("LLM HTTP Service stopped")
                else:
                    self.print_status("LLM service was not running")

                self.llm_pid_file.unlink()

            except Exception as e:
                self.print_warning(f"Error stopping LLM service: {e}")
        else:
            self.print_status("No LLM service PID file found")

    def show_status(self):
        """Show current service status"""
        print("\n" + "="*50)
        print("SERVICE STATUS")
        print("="*50)

        # Check LLM service
        if self.llm_pid_file.exists():
            try:
                with open(self.llm_pid_file, 'r') as f:
                    pid = int(f.read().strip())

                if psutil.pid_exists(pid):
                    print("🤖 LLM HTTP Service: RUNNING (PID: {})".format(pid))
                else:
                    print("🤖 LLM HTTP Service: STOPPED (PID file exists but process not running)")
            except:
                print("🤖 LLM HTTP Service: ERROR reading PID file")
        else:
            print("🤖 LLM HTTP Service: NOT STARTED")

        # Check ports
        ports_status = []
        for port in [5006]:
            status = "IN USE" if self.check_port_in_use(port) else "FREE"
            ports_status.append(f"📡 Port {port}: {status}")
        print("\n".join(ports_status))

        # Check Ollama
        ollama_status = "RUNNING" if self.check_ollama() else "NOT RUNNING"
        print(f"🧠 Ollama: {ollama_status}")

        print("\n📋 Unity Connection Info:")
        print("  📡 LLM Service: http://127.0.0.1:5006")
        print("  🎮 Drone Control: http://127.0.0.1:5005 (Unity internal)")

    def start_all_services(self) -> bool:
        """Start all services"""
        self.print_status("Starting Drone Simulation Services...")
        print()

        # Check dependencies
        if not self.check_dependencies():
            return False

        # Start services
        if not self.start_llm_service():
            return False

        print()
        self.print_success("🎉 All services started successfully!")
        self.show_status()

        return True

    def stop_all_services(self):
        """Stop all services"""
        self.print_status("Stopping all services...")
        self.stop_llm_service()
        self.print_success("All services stopped")

def main():
    parser = argparse.ArgumentParser(description='Drone Simulation Services Manager')
    parser.add_argument('action', choices=['start', 'stop', 'status', 'restart'],
                       default='start', nargs='?',
                       help='Action to perform (default: start)')
    parser.add_argument('--wait', action='store_true',
                       help='Keep running and wait (only for start action)')

    args = parser.parse_args()

    manager = ServiceManager()

    if args.action == 'start':
        if manager.start_all_services():
            if args.wait:
                print("\n" + "="*60)
                print("SERVICES ARE RUNNING")
                print("="*60)
                print("Next steps:")
                print("1. Open Unity and load your drone scene")
                print("2. Make sure CommandUIManager is attached to a GameObject")
                print("3. Press TAB in Unity to open the command interface")
                print("4. Enjoy controlling your drone with natural language!")
                print("\nPress Ctrl+C to stop all services")
                print("="*60)

                try:
                    # Keep running until interrupted
                    while True:
                        time.sleep(1)
                except KeyboardInterrupt:
                    print("\nShutting down services...")
                    manager.stop_all_services()
            else:
                print("\n💡 Tip: Use --wait to keep services running")
        else:
            sys.exit(1)

    elif args.action == 'stop':
        manager.stop_all_services()

    elif args.action == 'status':
        manager.show_status()

    elif args.action == 'restart':
        manager.print_status("Restarting all services...")
        manager.stop_all_services()
        time.sleep(2)
        manager.start_all_services()

if __name__ == "__main__":
    main()
