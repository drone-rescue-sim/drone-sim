# 🚁 Drone Simulation with Voice & Natural Language Control

A comprehensive Unity-based drone simulation featuring **voice control** and **natural language processing**. Control your drone using:

🎤 **Voice Commands**: Speak naturally like "fly forward" or "go up"
✍️ **Text Commands**: Type commands through an intuitive UI
🤖 **LLM Integration**: Powered by Llama2 via Ollama
🔊 **Whisper AI**: Advanced speech-to-text using OpenAI's Whisper

## 🌟 Key Features

- **🎤 Real-time Voice Control** - Speak commands naturally
- **🤖 AI-Powered Commands** - LLM translates natural language to drone actions
- **🎮 Manual Controls** - Traditional arrow key movement
- **🔄 Seamless Integration** - Unity ↔ Python ↔ LLM ↔ Whisper
- **📱 User-Friendly UI** - Clean interface for text and voice input
- **⚡ Fast Processing** - Optimized for real-time interaction

## 🚀 Quick Start

### Prerequisites

- **Python 3.11+** with pip
- **Unity 2021+** with Universal Render Pipeline
- **Ollama** (for local LLM processing)
- **ffmpeg** (for audio processing)
- **Llama2 model** (automatically downloaded)
- **Anaconda/Miniconda** (recommended for complete environment)

### 1. Install System Dependencies

#### macOS (with Homebrew)

```bash
# Install Homebrew if not already installed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install ffmpeg (required for Whisper)
brew install ffmpeg

# Install Anaconda (recommended)
curl -O https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-arm64.sh
bash Miniconda3-latest-MacOSX-arm64.sh
```

#### Linux

```bash
# Install ffmpeg
sudo apt update && sudo apt install ffmpeg

# Install Anaconda
wget https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh
bash Miniconda3-latest-Linux-x86_64.sh
```

### 2. Install Python Dependencies

```bash
# Create conda environment (recommended)
conda create -n drone-sim python=3.11
conda activate drone-sim

# Install dependencies
pip install -r requirements.txt

# Alternative: Install manually
pip install flask requests ollama openai-whisper
```

### 3. Install and Start Ollama

```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama service (in background)
ollama serve

# In another terminal, pull the Llama2 model
ollama pull llama2
```

### 4. Start Services

```bash
# Using conda environment (recommended)
conda activate drone-sim
python start.py

# Or with specific commands
python start.py start    # Start all services
python start.py stop     # Stop all services
python start.py status   # Check service status
python start.py restart  # Restart all services
```

### 5. Open Unity Project

1. Open Unity Hub
2. Open project: `unity-client/drone-env/`
3. Load scene: `Assets/Scenes/SampleScene.unity`
4. Press Play

### 6. Test the System

```bash
# Run comprehensive tests to verify everything works
python test.py

# Or test specific components
curl http://127.0.0.1:5006/health  # Check service health
```

### 7. Control the Drone

#### 🎮 Manual Controls (Always Available)

- **↑↓←→** - Move in cardinal directions

#### ✍️ Text Commands (LLM)

1. Press **TAB** to open command interface
2. Type commands like:
   - "fly forward" → moves forward
   - "go up" → ascends
   - "turn left" → rotates left
   - "stop" → halts all movement

#### 🎤 Voice Commands (Whisper + LLM)

1. Press **TAB** to open command interface
2. Click the **🎤 Microphone button** (turns red when recording)
3. **Speak naturally** for 5 seconds (auto-stop) or click again to stop
4. Voice is transcribed and processed automatically
5. Try saying: _"Fly forward"_, _"Go back"_, _"Turn around"_ etc.

**💡 Voice commands work exactly like text commands but use your microphone!**

## 📁 Project Structure

```
drone-sim/
├── services/                    # 🐍 Python backend services
│   ├── llm/
│   │   ├── http_service.py      # 🌐 Flask server (text + audio processing)
│   │   ├── main.py             # 🤖 LLM command processing logic
│   │   ├── test_*.py           # 🧪 Test files for integration
│   │   └── __pycache__/        # Compiled Python files
│   └── detection/              # 👁️ (Future: object detection)
│       └── main.py
│
├── unity-client/drone-env/     # 🎮 Unity project
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   └── SampleScene.unity    # Main game scene
│   │   ├── Scripts/
│   │   │   ├── CommandInputUI.cs    # 🎤 UI for text + voice input
│   │   │   ├── CommandUISetup.cs    # 🔧 Auto-creates UI elements
│   │   │   ├── CommandUIManager.cs  # 📱 Manages UI components
│   │   │   └── DroneController.cs   # 🚁 Drone movement & integration
│   │   ├── Prefabs/
│   │   │   └── drone Black.prefab   # Drone 3D model
│   │   └── Settings/           # Unity project settings
│   ├── Packages/               # 📦 Unity package dependencies
│   └── ProjectSettings/        # ⚙️ Unity project configuration
│
├── venv/                       # 🐍 Python virtual environment
├── requirements.txt            # 📋 Python dependencies
├── start.py                     # 🚀 Python service startup script
├── test.py                      # 🧪 Comprehensive test suite
├── llm_service.*               # 📝 Service logs and PIDs
└── README.md                   # 📖 This documentation
```

### 🔧 Core Technologies

- **🎮 Unity 2021+** - Game engine and 3D simulation
- **🐍 Python 3.11+** - Backend processing
- **🌐 Flask** - HTTP server for Unity↔Python communication
- **🤖 Ollama + Llama2** - Local LLM for natural language processing
- **🎤 OpenAI Whisper** - Speech-to-text transcription
- **🎵 ffmpeg** - Audio processing and format conversion
- **📡 HTTP APIs** - Inter-service communication

## 🔧 How It Works

### Architecture Overview

```
🎮 Unity Game ──────HTTP──────► 🐍 Python Flask Server ───API───► 🤖 Ollama LLM
     │                              │                           │
     │                              │                           │
     │                     ┌────────▼────────┐         ┌────────▼────────┐
     │                     │   Text Commands │         │  Natural Lang   │
     │                     │   (Port 5006)   │         │   Processing    │
     │                     └────────┬───────┘         └────────┬─────────┘
     │                              │                           │
     │                     ┌────────▼────────┐         ┌────────▼────────┐
🎤 Voice Recording        │  Audio Commands │         │   Command        │
     │                     │   (Port 5006)   │         │   Generation    │
     │                     └────────┬───────┘         └────────┬─────────┘
     │                              │                           │
     └──────────────HTTP────────────┼───────────────────────────┼─────────┐
                                    ▼                           ▼         │
                           🎵 ffmpeg ──► 🔊 Whisper ──► 📝 Transcription  │
                                    │                           │         │
                                    └───────────────────────────┼─────────┘
                                                                ▼
                                                       🚁 Drone Controller
                                                          (Port 5005)
```

### 🔄 Data Flow

1. **🎤 Voice Input**: User speaks → Unity Microphone API → WAV file
2. **📤 Audio Upload**: WAV sent to Flask server via HTTP multipart
3. **🎵 Audio Processing**: ffmpeg converts → Whisper transcribes → Text output
4. **🤖 LLM Processing**: Text sent to Ollama → Natural language understanding
5. **📥 Command Execution**: Processed command sent to Unity → Drone movement
6. **🔄 Feedback Loop**: All steps logged and monitored in real-time

### Key Components

#### 1. 🎤 Voice Recording System (`CommandInputUI.cs`)

- **Unity Microphone API** - Captures audio from system microphone
- **5-second auto-stop** - Prevents endless recordings
- **Real-time feedback** - UI button turns red during recording
- **WAV conversion** - Prepares audio for Whisper processing
- **Error handling** - Graceful fallback if microphone unavailable

#### 2. 🔊 Whisper Audio Processing (`http_service.py`)

- **OpenAI Whisper integration** - Industry-leading speech-to-text
- **Automatic language detection** - Works with multiple languages
- **Confidence scoring** - Quality assessment of transcriptions
- **Model optimization** - Uses 'tiny' model for fast processing
- **Error recovery** - Handles network issues and model downloads

#### 3. 🤖 LLM Command Processing (`main.py`)

- **Ollama + Llama2** - Local AI model, no internet required
- **Natural language understanding** - Translates speech/text to commands
- **Command validation** - Ensures safe and valid drone instructions
- **Extensible prompt engineering** - Easy to add new command types

#### 4. 🚁 Unity Drone Controller (`DroneController.cs`)

- **HTTP server** on port 5005 - Receives processed commands
- **Multi-modal input** - Manual controls + LLM commands + voice
- **Movement interpolation** - Smooth drone animations
- **Command queuing** - Handles multiple commands gracefully
- **Error recovery** - Continues operation despite communication issues

#### 5. 🌐 Flask HTTP Service (`http_service.py`)

- **Dual endpoints**: `/process_command` (text) + `/process_audio_command` (voice)
- **Health monitoring** - `/health` endpoint for system status
- **Concurrent processing** - Handles multiple Unity clients
- **Detailed logging** - Comprehensive debugging information
- **Graceful shutdown** - Clean service termination

### 🎯 Supported Commands

#### 🎮 Manual Controls (Always Available):

- **Arrow Keys**: ↑↓←→ for cardinal movement
- **Real-time response** - No processing delay

#### ✍️ Text Commands (LLM Processing):

**Movement:**

- `"fly forward"`, `"go ahead"`, `"move ahead"` → `move_forward`
- `"go back"`, `"fly backward"`, `"reverse"` → `move_backward`
- `"go left"`, `"fly left"`, `"strafe left"` → `move_left`
- `"go right"`, `"fly right"`, `"strafe right"` → `move_right`

**Vertical Movement:**

- `"go up"`, `"fly up"`, `"ascend"`, `"climb"` → `ascend`
- `"go down"`, `"fly down"`, `"descend"`, `"lower"` → `descend`

**Rotation:**

- `"turn left"`, `"rotate left"`, `"spin left"` → `turn_left`
- `"turn right"`, `"rotate right"`, `"spin right"` → `turn_right`

**Special Commands:**

- `"stop"`, `"halt"`, `"emergency stop"` → `stop`
- `"hover"`, `"stay still"`, `"hold position"` → `stop`

#### 🎤 Voice Commands (Whisper + LLM):

**All text commands work with voice!** Simply speak naturally:

- _"Fly forward please"_ → moves forward
- _"Can you go up a bit?"_ → ascends
- _"Turn around for me"_ → rotates 180°
- _"Stop right now"_ → emergency stop

**Voice Features:**

- 🎯 **Natural speech** - No need for exact phrasing
- 🌍 **Multi-language** - Automatic language detection
- ⚡ **Real-time processing** - Results in 2-5 seconds
- 📊 **Confidence scoring** - Quality feedback for transcriptions
- 🔄 **Seamless integration** - Voice → Text → LLM → Drone

**Pro Tips for Voice:**

- Speak clearly but naturally
- Wait for the beep/sound before speaking
- Commands are processed after 5 seconds or manual stop
- Check Unity console for transcription feedback

## 🎮 Controls

### 🎯 Control Methods Overview

| Method     | Input Type    | Processing    | Speed   | Setup Required |
| ---------- | ------------- | ------------- | ------- | -------------- |
| **Manual** | Arrow Keys    | Direct        | Instant | None           |
| **Text**   | Keyboard + UI | LLM           | 1-2 sec | Ollama running |
| **Voice**  | Microphone    | Whisper + LLM | 3-6 sec | All services   |

### 🎮 Manual Controls (Always Available)

- **↑** - Move forward
- **↓** - Move backward
- **←** - Move left
- **→** - Move right
- **No dependencies** - Works even if services are down

### ✍️ Text Commands (LLM Processing)

1. Press **TAB** to open command interface
2. Type natural language: `"fly forward"`, `"go up"`, etc.
3. Press **Enter** or click **Send** button
4. Interface closes automatically
5. Drone moves for 2 seconds, then stops
6. **Result**: Command processed by Llama2, translated to drone action

### 🎤 Voice Commands (Whisper + LLM)

1. Press **TAB** to open command interface
2. Click **🎤 Microphone** button (turns red when recording)
3. **Speak naturally** for up to 5 seconds
4. Recording auto-stops or click microphone again to stop manually
5. **Processing**: Voice → Whisper transcription → LLM processing → Drone action
6. **Visual feedback**: Button color indicates recording status

### 🎛️ UI Controls

- **TAB** - Toggle command interface
- **Enter** - Send text command
- **🎤 Button** - Start/stop voice recording
- **Send Button** - Send command and close UI
- **Close Button** - Close interface without sending

### ⚡ Performance Expectations

- **Manual**: Instant response
- **Text**: 1-2 seconds (LLM processing)
- **Voice**: 3-6 seconds (Recording + Whisper + LLM)
- **First voice command**: May take longer (Whisper model download)

## 🚀 Advanced Setup

### Installing Ollama

```bash
# macOS/Linux
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama service (background)
ollama serve

# Pull Llama2 model (happens automatically, but you can pre-download)
ollama pull llama2
```

### Installing ffmpeg (Required for Voice)

#### macOS with Homebrew:

```bash
# Install Homebrew if needed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install ffmpeg
brew install ffmpeg

# Verify installation
ffmpeg -version
```

#### Linux:

```bash
# Ubuntu/Debian
sudo apt update && sudo apt install ffmpeg

# CentOS/RHEL
sudo yum install ffmpeg
```

#### Windows:

Download from https://ffmpeg.org/download.html and add to PATH

### Python Environment Setup

#### Using Conda (Recommended):

```bash
# Install Miniconda/Anaconda
# macOS ARM64
curl -O https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-arm64.sh
bash Miniconda3-latest-MacOSX-arm64.sh

# Create environment
conda create -n drone-sim python=3.11
conda activate drone-sim

# Install dependencies
pip install -r requirements.txt
```

#### Using venv:

```bash
# Create virtual environment
python -m venv venv
source venv/bin/activate  # Linux/macOS
# venv\Scripts\activate     # Windows

# Install dependencies
pip install -r requirements.txt
```

### Manual Service Startup

```bash
# Terminal 1: Start Ollama (if not already running)
ollama serve

# Terminal 2: Activate environment and start LLM service
conda activate drone-sim  # or source venv/bin/activate
cd services/llm
python http_service.py

# Terminal 3: Open Unity project and play
```

### Port Configuration

- **LLM Service**: `http://127.0.0.1:5006`
- **Unity Drone Control**: `http://127.0.0.1:5005`

## 🔍 Troubleshooting

### Common Issues

#### "LLM service connection failed"

```bash
# Check if services are running
python start.py status

# Restart services
python start.py restart
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

#### 🎤 Voice Recording Issues

**"Microphone not found"**

```bash
# Check available microphones
python -c "import pyaudio; p = pyaudio.PyAudio(); [print(f'{i}: {p.get_device_info_by_index(i)[\"name\"]}') for i in range(p.get_device_count())]"
```

- Ensure microphone permissions in macOS System Settings
- Try different microphone devices
- Restart Unity after changing microphone settings

**"ffmpeg not found"**

```bash
# macOS
brew install ffmpeg

# Linux
sudo apt install ffmpeg

# Verify
ffmpeg -version
```

**"Whisper model download timeout"**

- First voice command may take 2-5 minutes to download model
- Check internet connection
- Model is cached after first download
- Use `tiny` model for faster processing

**"Audio processing timeout"**

- Increase timeout in `CommandInputUI.cs` if needed
- Check Python service logs for detailed errors
- Ensure sufficient RAM (4GB+ recommended)

#### Unity Issues

**"HttpClient timeout error"**

- Service restarted while Unity was running
- Restart services: `python start.py restart`
- Check service health: `curl http://127.0.0.1:5006/health`

**UI not showing**

- Check Console for errors
- Ensure CommandUIManager is in scene
- Verify TMP Essentials are imported
- Press TAB to manually toggle UI

**Compilation errors**

- Ensure all C# scripts are in Assets/Scripts/
- Check for missing using statements
- Verify Unity version compatibility

### Debug Tips

1. **🎮 Check Unity Console** for error messages and transcription feedback
2. **🐍 Monitor Python logs** in terminal where service is running
3. **🧪 Run comprehensive tests**: `python test.py`
4. **🔊 Test voice transcription**:
   ```bash
   # Record a test audio file and test Whisper
   ffmpeg -f avfoundation -i ":0" -t 3 test.wav
   python -c "import whisper; model = whisper.load_model('tiny'); result = model.transcribe('test.wav'); print(result['text'])"
   ```
5. **🤖 Test LLM directly**:
   ```bash
   curl -X POST http://127.0.0.1:5006/process_command \
        -H "Content-Type: application/json" \
        -d '{"command": "fly forward"}'
   ```
6. **💚 Check service health**:
   ```bash
   curl http://127.0.0.1:5006/health
   ```
7. **🎤 Test microphone**:
   ```bash
   # macOS: List available devices
   ffmpeg -f avfoundation -list_devices true -i ""
   ```

## 📚 API Reference

### LLM Service Endpoints

#### POST `/process_command`

Process natural language text commands from Unity.

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

#### POST `/process_audio_command`

Process voice commands from Unity (WAV audio files).

**Request:** (multipart/form-data)

- `audio`: WAV audio file (voice command)

**Response:**

```json
{
  "transcript": "fly forward",
  "confidence": 0.95,
  "processed_command": "move_forward",
  "status": "success"
}
```

**Error Response:**

```json
{
  "error": "Audio processing timeout"
}
```

#### GET `/health`

Check service health and component availability.

**Response:**

```json
{
  "status": "healthy",
  "service": "drone-llm-service",
  "ollama_available": true,
  "whisper_available": true
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

**Supported Commands:**

- `move_forward`, `move_backward`, `move_left`, `move_right`
- `ascend`, `descend`
- `turn_left`, `turn_right`
- `stop`

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

## 🎯 System Architecture Summary

### 🏗️ Complete Integration Stack

```
🎤 User Voice ──► 🎮 Unity Recording ──► 🌐 HTTP Multipart ──► 🐍 Flask Server
                                                                          │
                                                                          ▼
🎵 Audio WAV ──► 🎵 ffmpeg Processing ──► 🔊 OpenAI Whisper ──► 📝 Transcription
                                                                          │
                                                                          ▼
📄 Text Command ──► 🤖 Ollama Llama2 ──► 🎯 Intent Analysis ──► 🚁 Drone Action
                                                                          │
                                                                          ▼
📡 HTTP Response ──► 🎮 Unity Controller ──► 🚁 Drone Movement ──► ✅ User Feedback
```

### ⚡ Performance Characteristics

| Component              | Technology           | Processing Time | Resource Usage    |
| ---------------------- | -------------------- | --------------- | ----------------- |
| **Voice Recording**    | Unity Microphone API | Instant         | Low               |
| **Audio Processing**   | ffmpeg               | <1 sec          | Medium            |
| **Speech Recognition** | OpenAI Whisper       | 2-3 sec         | High (first time) |
| **LLM Processing**     | Ollama Llama2        | 1-2 sec         | Medium            |
| **Drone Control**      | Unity Engine         | Instant         | Low               |

### 🔧 Configuration Files

- **`requirements.txt`** - Python dependencies (Flask, Ollama, OpenAI Whisper)
- **`start_llm_service.sh`** - Service startup with conda environment
- **`http_service.py`** - Main Flask server with dual endpoints
- **`CommandInputUI.cs`** - Unity voice recording and UI management
- **`DroneController.cs`** - Drone movement and HTTP server

## 🚀 Future Enhancements

### 🎯 Planned Features

- **🎥 Object Detection** - Computer vision for obstacle avoidance
- **📊 Telemetry Logging** - Flight data recording and analysis
- **🎮 Multi-Drone Control** - Coordinate multiple drones
- **🔄 Offline Mode** - Fallback when services unavailable
- **🌐 Multi-Language Support** - Extended Whisper language models

### 🔧 Technical Improvements

- **⚡ Performance Optimization** - Reduce latency and resource usage
- **🛡️ Error Recovery** - Enhanced fault tolerance
- **📱 Mobile Support** - iOS/Android deployment
- **☁️ Cloud Integration** - Remote processing capabilities

## 📝 License & Credits

### License

This project is for educational and research purposes. Feel free to modify and extend!

### 🙏 Credits

- **🎤 OpenAI Whisper** - Industry-leading speech recognition
- **🤖 Ollama** - Local LLM deployment made easy
- **🎮 Unity Technologies** - Powerful game engine
- **🐍 Python Community** - Flask, requests, and ecosystem

### 📞 Support

- **Unity Console** - Real-time error messages and transcription feedback
- **Python Logs** - Detailed service processing information
- **Health Endpoint** - System status monitoring
- **Debug Tools** - Comprehensive testing utilities

---

**🎉 Ready to fly?** Your voice-controlled drone is now fully operational with cutting-edge AI integration!</contents>
</xai:function_call_1>Check if there are any linter errors in the README.md file
