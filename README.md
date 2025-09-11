# Drone Simulation with Natural Language Control

A Unity-based drone simulation that uses Large Language Models (LLMs) to translate natural language commands into drone movements. Control your drone with commands like "fly forward" or "go up" through an in-game UI.

## 🚀 Quick Start

### Prerequisites
- **Python 3.11+** with pip
- **Unity 2021+** with Universal Render Pipeline
- **Ollama** (for local LLM processing)
- **Llama2 model** (automatically downloaded)

### 1. Install Dependencies

```bash
# Install Python dependencies
pip install flask flask-cors ollama requests

# Or from requirements.txt
pip install -r requirements.txt
```

### 2. Start Services

```bash
# Start all services (LLM + HTTP server)
./start_all_services.sh start

# Or start individual services
./start_llm_service.sh
```

### 3. Open Unity Project

1. Open Unity Hub
2. Open project: `unity-client/drone-env/`
3. Load scene: `Assets/Scenes/SampleScene.unity`
4. Press Play

### 4. Control the Drone

- **Manual Control**: Use arrow keys (↑↓←→) for basic movement
- **LLM Control**: Press TAB to open command interface
- **Natural Language**: Type commands like:
  - "fly forward"
  - "go up"
  - "turn left"
  - "stop"

## 📁 Project Structure

```
drone-sim/
├── services/                    # Python backend services
│   ├── llm/
│   │   ├── http_service.py      # Flask server for LLM processing
│   │   └── main.py             # LLM command processing logic
│   └── detection/              # (Future: object detection)
│
├── unity-client/drone-env/     # Unity project
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   └── SampleScene.unity    # Main game scene
│   │   ├── Scripts/
│   │   │   ├── CommandInputUI.cs    # UI for text input
│   │   │   ├── CommandUISetup.cs    # Auto-creates UI elements
│   │   │   ├── CommandUIManager.cs  # Manages UI components
│   │   │   └── DroneController.cs   # Drone movement & LLM integration
│   │   └── Prefabs/
│   │       └── drone Black.prefab   # Drone 3D model
│   └── Packages/               # Unity package dependencies
│
├── venv/                       # Python virtual environment
├── requirements.txt            # Python dependencies
└── start_*.sh                  # Startup scripts
```

## 🔧 How It Works

### Architecture Overview

```
Unity Game ───HTTP───→ Python Flask Server ──API───→ Ollama LLM
     ↑                       ↓                          ↓
     │                  Processes commands         Generates
     │                  and sends to drone         responses
     │
     └── Drone Controller (receives commands via HTTP on port 5005)
```

### Key Components

#### 1. LLM Service (`services/llm/http_service.py`)
- **Flask web server** running on port 5006
- **Receives** natural language commands from Unity UI
- **Processes** commands using Llama2 model via Ollama
- **Translates** natural language → drone commands
- **Returns** processed commands back to Unity

#### 2. Unity Drone Controller (`DroneController.cs`)
- **HTTP server** running on port 5005
- **Receives** processed commands from LLM service
- **Controls** drone movement and rotation
- **Manual input**: Only arrow keys (↑↓←→)
- **LLM input**: All other movements (up/down, rotation)

#### 3. Unity UI System
- **CommandInputUI.cs**: Handles text input and LLM communication
- **CommandUISetup.cs**: Automatically creates UI elements (canvas, buttons, input field)
- **CommandUIManager.cs**: Ensures UI components are initialized

### Supported Commands

#### Movement Commands (Manual + LLM):
- `move_forward` / `move_backward` / `move_left` / `move_right`
- Arrow keys: ↑↓←→

#### Vertical Movement (LLM only):
- `ascend` / `go_up` / `descend` / `go_down`
- Commands like: "fly up", "go higher", "descend slowly"

#### Rotation (LLM only):
- `turn_left` / `turn_right`
- Commands like: "turn around", "rotate left"

#### Special Commands:
- `stop` - Stops all movement
- Commands like: "hover", "stay still", "emergency stop"

## 🎮 Controls

### Manual Controls (Always Available)
- **↑** - Move forward
- **↓** - Move backward
- **←** - Move left
- **→** - Move right

### LLM Controls (Via UI)
1. Press **TAB** to open command interface
2. Type natural language command
3. Press **Enter** or click **Send**
4. Interface closes automatically after sending
5. Drone executes command for 2 seconds, then stops

### UI Controls
- **TAB** - Toggle command interface
- **Enter** - Send command
- **Send Button** - Send command and close UI
- **Close Button** - Close interface without sending

## 🚀 Advanced Setup

### Installing Ollama

```bash
# macOS/Linux
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama service
ollama serve

# Pull Llama2 model (happens automatically on first use)
ollama pull llama2
```

### Manual Service Startup

```bash
# Terminal 1: Start Ollama
ollama serve

# Terminal 2: Start LLM HTTP service
cd services/llm
python http_service.py

# Terminal 3: Open Unity and play
```

### Port Configuration

- **LLM Service**: `http://127.0.0.1:5006`
- **Unity Drone Control**: `http://127.0.0.1:5005`

## 🔍 Troubleshooting

### Common Issues

#### "LLM service connection failed"
```bash
# Check if services are running
./start_all_services.sh status

# Restart services
./start_all_services.sh restart
```

#### "Ollama not found"
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama
ollama serve

# Pull model
ollama pull llama2
```

#### "Port 5006 already in use"
```bash
# Kill existing process
lsof -ti :5006 | xargs kill -9

# Or change port in scripts
```

#### Unity UI not showing
- Check Console for errors
- Ensure CommandUIManager is in scene
- Verify TMP Essentials are imported

### Debug Tips

1. **Check Unity Console** for error messages
2. **Monitor Python logs** in `llm_service.log`
3. **Test LLM directly**:
   ```bash
   curl -X POST http://127.0.0.1:5006/process_command \
        -H "Content-Type: application/json" \
        -d '{"command": "fly forward"}'
   ```

## 📚 API Reference

### LLM Service Endpoints

#### POST `/process_command`
Process natural language drone command.

**Request:**
```json
{
  "command": "fly forward"
}
```

**Response:**
```json
{
  "processed_command": "move_forward",
  "status": "success"
}
```

#### GET `/health`
Check service health.

**Response:**
```json
{
  "status": "healthy",
  "service": "drone-llm-service",
  "ollama_available": true
}
```

### Unity HTTP Server

The Unity drone controller runs an HTTP server on port 5005 that receives commands from the LLM service.

**Expected format:**
```json
{
  "command": "move_forward"
}
```

## 🎯 Next Steps

### Customization Ideas

1. **Add More Commands**: Extend the command list in `http_service.py`
2. **Improve AI**: Fine-tune prompts for better command understanding
3. **Add Voice Control**: Integrate speech-to-text
4. **Multiple Drones**: Support controlling multiple drones
5. **Object Detection**: Add computer vision for obstacle avoidance

### Performance Optimization

- Reduce LLM response time by using smaller models
- Cache common command translations
- Implement command prediction
- Add offline fallback mode

## 📝 License

This project is for educational purposes. Feel free to modify and extend!

---

**Need Help?** Check the Unity Console and Python logs for error messages. The system is designed to be robust with automatic error recovery.</contents>
</xai:function_call_1>Check if there are any linter errors in the README.md file