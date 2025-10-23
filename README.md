# Drone Simulation with AI-Powered Control

Unity-based drone simulation with voice control, natural language processing, and object detection.

## Prerequisites

- Python 3.11+ with pip
- Unity 2021+ with Universal Render Pipeline
- Ollama (for local LLM processing)
- ffmpeg (for audio processing)

## External Data Files & Libraries

### Python Libraries

```bash
pip install -r requirements.txt
```

Core: flask, requests, ollama, openai-whisper, ultralytics, opencv-python

### External Files

- `services/detection/yolo-Weights/yolov8n.pt` - YOLO v8 nano model
- `services/llm/finetune/drone_commands.jsonl` - Training data

### Configuration

- `config.env` - API keys and configuration
  - `LLM_PROVIDER=openai`
  - `OPENAI_MODEL=gpt-3.5-turbo`
  - `OPENAI_API_KEY=your_key_here`

## Setup

### 1. System Dependencies

macOS: `brew install ffmpeg && curl -fsSL https://ollama.ai/install.sh | sh`
Linux: `sudo apt update && sudo apt install ffmpeg && curl -fsSL https://ollama.ai/install.sh | sh`
Windows: `choco install ffmpeg` + download Ollama from https://ollama.ai/download

### 2. Python Environment

```bash
python -m venv venv
source venv/bin/activate  # Linux/macOS
pip install -r requirements.txt
```

### 3. Ollama Setup

```bash
ollama serve
ollama pull llama2
```

### 4. Environment Configuration

Create `config.env` with API keys

### 5. Unity Setup

Open Unity Hub → Open `unity-client/drone-env/` → Load `Assets/Scenes/SampleScene.unity`

### 6. Start Services

```bash
python start.py
```

### 7. Test

```bash
python test.py
curl http://127.0.0.1:5006/health
```

## Usage

- Manual: Arrow keys (↑↓←→)
- Text: Press TAB, type "fly forward"
- Voice: Press TAB, click microphone, speak commands

## Commands

Movement: "fly forward", "go back", "go left", "go right"
Vertical: "go up", "go down"
Rotation: "turn left", "turn right"
Special: "stop", "hover"

## Service Ports

- LLM Service: http://127.0.0.1:5006
- Unity Control: http://127.0.0.1:5005
- Ollama API: http://127.0.0.1:11434

## Troubleshooting

```bash
python start.py status
python start.py restart
curl -X POST http://127.0.0.1:5006/process_command -H "Content-Type: application/json" -d '{"command": "fly forward"}'
```

## Project Structure

```
drone-sim/
├── services/llm/          # LLM and voice processing
├── services/detection/    # Object detection
├── services/tobii/        # Gaze tracking
├── unity-client/drone-env/ # Unity project
├── requirements.txt      # Python dependencies
├── config.env           # Environment configuration
├── start.py             # Service startup
└── test.py              # Test suite
```

## Key Files to Review and to Look at

### Core Unity Scripts (C#)

- `unity-client/drone-env/Assets/Scripts/DroneController.cs` - Main drone movement and HTTP server (934 lines)
- `unity-client/drone-env/Assets/Scripts/CommandInputUI.cs` - Voice/text input interface
- `unity-client/drone-env/Assets/Scripts/CommandUIManager.cs` - UI management
- `unity-client/drone-env/Assets/Scripts/GazeHistoryManager.cs` - Gaze tracking data management
- `unity-client/drone-env/Assets/Scripts/EyeTrackingSimulator.cs` - Mouse-based gaze simulation

### Python Backend Services

- `services/llm/http_service.py` - Main Flask server for voice/text processing (533 lines)
- `services/llm/main.py` - LLM command processing logic (311 lines)
- `services/detection/main.py` - YOLO object detection (71 lines)
- `services/tobii/http_service.py` - Tobii gaze tracking API (132 lines)
- `start.py` - Service startup script (405 lines)

### Configuration & Data

- `config.env` - API keys and environment variables
- `requirements.txt` - Python dependencies
- `services/llm/finetune/drone_commands.jsonl` - Training data (491 examples)
- `services/detection/yolo-Weights/yolov8n.pt` - YOLO model weights

### Unity Project Files

- `unity-client/drone-env/Assets/Scenes/SampleScene.unity` - Main scene
- `unity-client/drone-env/Assets/Prefabs/drone Black.prefab` - Drone model

## License

Educational and research purposes.
