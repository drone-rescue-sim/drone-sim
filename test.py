import sys
import os
import time
import requests
import json
import subprocess
import platform  
from pathlib import Path
from typing import Dict, List, Optional

class DroneSystemTester:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.test_results = []
        self.unavailable_tests = []

    ...

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
                self.log_test(
                    f"Python package: {package}",
                    False,
                    "Install with: pip install -r requirements.txt"
                )
                return False

        # Test ffmpeg
        try:
            result = subprocess.run(['ffmpeg', '-version'],
                                    capture_output=True, text=True, timeout=5)
            if result.returncode == 0:
                self.log_test("ffmpeg availability", True)
            else:
                raise FileNotFoundError
        except (FileNotFoundError, subprocess.TimeoutExpired):
            install_msg = ""
            if platform.system() == "Darwin":   # macOS
                install_msg = "Install with: brew install ffmpeg"
            elif platform.system() == "Windows":
                install_msg = "Install with: choco install ffmpeg (or download from ffmpeg.org)"
            else:  # Linux
                install_msg = "Install with: sudo apt-get install ffmpeg"
            self.log_test("ffmpeg availability", False, install_msg)
            return False

        return True
