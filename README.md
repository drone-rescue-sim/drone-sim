# Drone Rescue Simulator

## Overview

This project explores a new way of controlling drones for **emergency search and rescue** using:

- Natural language commands (LLM-based interpretation)
- Object detection (vision models)
- A simulated environment in Unity
- **NEW:** Integrated LLM-Unity communication system

---

## 🚀 Quick Start

### Prerequisites
- Unity 2020+ (with Universal Render Pipeline)
- Python 3.8+
- Ollama with Llama2 model

### 1. Setup Unity (Frontend)

```bash
# Open Unity Hub
# Add project → select `unity-client/drone-env`
# Open the project and press Play
```

**Controls:**
- `WASD` or `Arrow Keys`: Move drone
- `Space`: Ascend
- `Left Shift`: Descend
- `Q/E`: Turn left/right

### 2. Setup Python Environment

```bash
# Navigate to project root
cd /path/to/drone-sim

# Activate virtual environment
source activate_venv.sh

# Install dependencies
pip install -r requirements.txt
```

### 3. Install and Setup Ollama

```bash
# Install Ollama (if not already installed)
# Download from: https://ollama.ai/

# Pull Llama2 model
ollama pull llama2
```

### 4. Run the System

**Terminal 1: Start Unity**
- Open Unity and press Play in the drone scene

**Terminal 2: Start LLM Service**
```bash
cd services/llm
python main.py
```

Now you can type natural language commands like:
- "fly forward"
- "go up"
- "turn left"
- "stop"

---

## 🧪 Testing

### Run Integration Tests

```bash
# Test LLM command generation
python services/llm/test_llm_commands.py

# Test Unity HTTP connection
python test_unity_connection.py

# Run full integration tests
python services/llm/test_integration.py
```

### Manual Testing

1. **Start Unity** and ensure the drone scene is running
2. **Start LLM service**: `python services/llm/main.py`
3. **Test commands**:
   - Type: "fly forward" → Drone should move forward
   - Type: "go up" → Drone should ascend
   - Type: "turn left" → Drone should rotate left
   - Use keyboard controls simultaneously (they should combine)

---

## 🔧 Architecture

### Components

1. **Unity Drone Controller** (`unity-client/drone-env/Assets/Scripts/DroneController.cs`)
   - Handles physics and movement
   - Runs HTTP server on port 5005
   - Processes LLM commands and keyboard input
   - Combines manual + AI control

2. **LLM Service** (`services/llm/main.py`)
   - Processes natural language to drone commands
   - Uses Ollama/Llama2 for command generation
   - Sends commands to Unity via HTTP

3. **Command System**
   - `move_forward/backward/left/right`
   - `ascend/descend` (or `go_up/go_down`)
   - `turn_left/turn_right`
   - `stop`

### Communication Flow

```
User Input → LLM Service → HTTP POST → Unity Server → Drone Movement
     ↓              ↓              ↓              ↓              ↓
"fly forward" → "move_forward" → {"command": "..."} → Process → Move Forward
```

---

## 📋 Development

### 3. Python Services (Backend)
