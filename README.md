# Drone Simulation with AI-Powered Control

A Unity-based drone simulation with voice control, natural language processing, and object detection.

## Key Features

- **Voice Control** - Speak commands using OpenAI Whisper
- **AI Commands** - LLM translates natural language to drone actions
- **Object Detection** - YOLO-based real-time object detection
- **Manual Controls** - Arrow key movement

## Prerequisites

- **Python 3.11+** with pip
- **Unity 2021+** with Universal Render Pipeline
- **Ollama** (for local LLM processing)
- **ffmpeg** (for audio processing)

## External Data Files & Libraries

### Required Python Libraries

```bash
pip install -r requirements.txt
```

**Core Dependencies:**

- `flask` - Web framework for HTTP services
- `requests` - HTTP client for API communication
- `ollama` - Local LLM integration
- `openai-whisper` - Speech-to-text processing
- `ultralytics` - YOLO object detection
- `opencv-python` - Computer vision processing

### External Model Files

- `services/detection/yolo-Weights/yolov8n.pt` - Pre-trained YOLO v8 nano model
- `services/llm/finetune/drone_commands.jsonl` - Training data for fine-tuning

### Configuration Files

- `config.env` - Contains API keys and configuration
  - `LLM_PROVIDER` - LLM provider selection (openai/ollama)
  - `OPENAI_MODEL` - OpenAI model specification
  - `OPENAI_API_KEY` - OpenAI API key (sensitive)

## Setup Instructions

### 1. System Dependencies

#### macOS

```bash
brew install ffmpeg
curl -fsSL https://ollama.ai/install.sh | sh
```

#### Linux

```bash
sudo apt update && sudo apt install ffmpeg
curl -fsSL https://ollama.ai/install.sh | sh
```

#### Windows

```bash
choco install ffmpeg
# Download Ollama from https://ollama.ai/download
```

### 2. Python Environment

```bash
python -m venv venv
source venv/bin/activate  # Linux/macOS
# venv\Scripts\activate   # Windows
pip install -r requirements.txt
```

### 3. Ollama Setup

```bash
ollama serve
ollama pull llama2
```

### 4. Environment Configuration

Create `config.env`:

```bash
LLM_PROVIDER=openai
OPENAI_MODEL=gpt-3.5-turbo
OPENAI_API_KEY=your_openai_api_key_here
```

### 5. Unity Project Setup

1. Open Unity Hub
2. Open project: `unity-client/drone-env/`
3. Load scene: `Assets/Scenes/SampleScene.unity`

### 6. Start Services

```bash
python start.py
```

### 7. Test System

```bash
python test.py
curl http://127.0.0.1:5006/health
```

## Usage

### Control Methods

- **Manual**: Arrow keys (↑↓←→)
- **Text**: Press TAB, type commands like "fly forward"
- **Voice**: Press TAB, click microphone button, speak commands

### Supported Commands

- Movement: "fly forward", "go back", "go left", "go right"
- Vertical: "go up", "go down"
- Rotation: "turn left", "turn right"
- Special: "stop", "hover"

## Service Architecture

### Ports

- **LLM Service**: `http://127.0.0.1:5006`
- **Unity Control**: `http://127.0.0.1:5005`
- **Ollama API**: `http://127.0.0.1:11434`

## Troubleshooting

### Common Issues

```bash
# Check services
python start.py status

# Restart services
python start.py restart

# Test LLM
curl -X POST http://127.0.0.1:5006/process_command \
     -H "Content-Type: application/json" \
     -d '{"command": "fly forward"}'
```

### Debug Commands

```bash
# Test voice transcription
ffmpeg -f avfoundation -i ":0" -t 3 test.wav
python -c "import whisper; model = whisper.load_model('tiny'); result = model.transcribe('test.wav'); print(result['text'])"

# Check service health
curl http://127.0.0.1:5006/health
```

## Project Structure

```
drone-sim/
├── services/
│   ├── llm/                     # LLM and voice processing
│   ├── detection/              # Object detection service
│   └── tobii/                  # Gaze tracking service
├── unity-client/drone-env/     # Unity project
├── requirements.txt            # Python dependencies
├── config.env                  # Environment configuration
├── start.py                    # Service startup script
└── test.py                     # Test suite
```

## License

This project is for educational and research purposes.
