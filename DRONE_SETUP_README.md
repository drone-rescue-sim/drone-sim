# Drone Simulation with LLM Integration

This project integrates a Large Language Model (LLM) with Unity to create an interactive drone simulation where users can give natural language commands to control a professional drone from the PA DronePack asset.

## Architecture Overview

```
User Input (Text) → Unity UI → LLM Service (Ollama) → Command Processing → Unity Drone Control
     ↑                                                                       ↓
     └────────────────── HTTP (Port 5006) ──────────────────┘─────────────────┘
                                                             HTTP (Port 5005)
```

## Features

- **Natural Language Commands**: Type commands like "fly forward", "turn left", "go up" in plain English
- **Real-time Processing**: Commands are processed instantly using Ollama LLM
- **Unity Integration**: Visual drone simulation with professional drone models
- **HTTP Communication**: RESTful API communication between services

## Prerequisites

- Python 3.8+
- Unity 2021.3+ with URP (Universal Render Pipeline)
- Ollama installed and running locally
- TextMeshPro package in Unity (for UI)

## Quick Setup

### 1. Environment Setup

```bash
# Run the automated setup script
./setup_and_run.sh

# Or manually:
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

### 2. Start Ollama

Make sure Ollama is running with the llama2 model:

```bash
ollama serve  # Start Ollama server
ollama pull llama2  # Download the model
```

### 3. Unity Setup

**Option 1: Automated Setup (Recommended)**

1. **Open Unity Project**:
   - Open `unity-client/drone-env/` in Unity Hub
   - Open the `SampleScene`

2. **Automated Setup**:
   - Create an empty GameObject called "SceneSetup"
   - Add the `SetupDemoScene.cs` script to it
   - In the Inspector, right-click on the script and select "Setup Complete Demo Scene"
   - This will automatically create the drone, UI, and connect everything!

**Option 2: Manual Setup**

1. **Open Unity Project**:
   - Open `unity-client/drone-env/` in Unity Hub
   - Open the `SampleScene`

2. **Create Drone**:
   - Create an empty GameObject called "DroneCreator"
   - Add the `CreateSimpleDrone.cs` script
   - Right-click the script in Inspector → "Create Simple Drone"

3. **Create UI**:
   - Create an empty GameObject called "UICreator"
   - Add the `CreateDroneUI.cs` script
   - Right-click the script in Inspector → "Create Drone UI Setup"

4. **Connect Components**:
   - Find the "DroneCommandCanvas" GameObject
   - In the `DroneCommandUI` component, assign the drone GameObject to the "Drone Object" field

5. **Add Professional Drone**:
   - From the menu: Assets → ProfessionalAssets → DronePack → Prefabs
   - Drag one of the drone prefabs (_Drone [Quad].prefab recommended) into the scene
   - Position it appropriately (recommended: y = 2 for starting height)
   - The setup script will automatically find and connect to the drone

## Usage

### Starting the Services

1. **LLM Service** (Terminal 1):
   ```bash
   source venv/bin/activate
   python services/llm/main.py
   ```

2. **Unity** (Unity Editor):
   - Press Play to start the simulation
   - Press Tab to show/hide the command input panel

### Using the System

1. **Type Commands**: In the input field, type natural language commands:
   - "fly forward" or "move forward"
   - "turn left" or "turn right"
   - "go up" or "ascend"
   - "go down" or "descend"
   - "move left" or "move right"
   - "move backward"

2. **Send Command**: Press Enter or click the Send button

3. **Watch the Drone**: The drone should respond to your commands!

## Available Commands

The system supports these basic movement commands:

- **Movement**: `move_forward`, `move_backward`, `move_left`, `move_right`
- **Vertical**: `ascend`, `descend`, `move_up`, `move_down`
- **Rotation**: `turn_left`, `turn_right`
- **Control**: `stop` (stops all movement)

### Natural Language Examples

You can type these natural language commands:
- "fly forward" or "move forward"
- "go up" or "ascend"
- "turn left" or "rotate left"
- "move to the right"
- "stop moving"
- "fly up and turn left" (multiple commands separated by semicolon)

### Command Processing

1. **User Input** → Natural language text
2. **LLM Processing** → Converts to structured commands
3. **Unity Execution** → Drone performs the movements
4. **Feedback** → Status updates in UI

## Technical Details

### LLM Service (`services/llm/main.py`)

- **Port**: 5006
- **Endpoint**: `POST /process_command`
- **Input**: `{"input": "user command"}`
- **Output**: `{"command": "processed_command", "details": "description"}`

### Unity HTTP Server (`SimpleHTTPServer.cs`)

- **Port**: 5005
- **Endpoint**: `POST /receive_command`
- **Input**: `{"command": "drone_command", "details": "description"}`

### Command Processing

1. User types natural language in Unity UI
2. Unity sends request to LLM service
3. LLM processes text and returns structured command
4. LLM sends command to Unity's HTTP server
5. Unity executes command on drone

## Customization

### Adding New Commands

1. **Update LLM Prompt**: Modify the system prompt in `get_drone_instructions()` to include new commands
2. **Update Unity Handler**: Add new cases in `ExecuteSingleCommand()` method
3. **Test**: Try the new commands

### Improving Drone Control

The current implementation uses placeholder methods for drone control. To integrate with the actual drone:

1. Study the `PA_DroneController` class in the ProfessionalAssets
2. Replace the placeholder calls in `ExecuteSingleCommand()` with actual method calls
3. Add necessary parameters (speed, duration, etc.)

### UI Enhancement

- Add command history
- Add voice input
- Add command suggestions
- Improve visual feedback

## Troubleshooting

### Common Issues

1. **LLM Not Responding**:
   - Check if Ollama is running: `ollama list`
   - Verify llama2 model is downloaded: `ollama pull llama2`

2. **Unity HTTP Errors**:
   - Check console for network errors
   - Ensure ports 5005 and 5006 are not blocked

3. **Drone Not Moving**:
   - Check if drone GameObject is assigned
   - Verify drone controller component exists
   - Look for errors in Unity console

### Debug Mode

Run LLM service with debug output:
```bash
python services/llm/main.py --flask
```

### Testing Commands

You can test the LLM directly:
```python
from services.llm.main import get_drone_instructions
result = get_drone_instructions("fly forward")
print(result)  # Should return JSON with command
```

### Unity Testing

1. **Test LLM Connection**: In Unity, attach `SetupDemoScene.cs` to a GameObject, then right-click → "Test LLM Connection"
2. **Check Console**: Look for debug messages in Unity Console and LLM terminal
3. **Manual Commands**: You can also manually call drone methods in Unity's Inspector

## Troubleshooting

### Quick Diagnosis

1. **Check LLM Service**:
   ```bash
   # Test if Ollama is running
   curl http://127.0.0.1:5006/process_command -X POST -H "Content-Type: application/json" -d '{"input": "test"}'
   ```

2. **Check Unity HTTP Server**:
   - Look for "HTTP Server started on port 5005" in Unity Console
   - Check if drone GameObject is assigned in DroneCommandUI

3. **Network Issues**:
   - Ensure ports 5005 and 5006 are not blocked by firewall
   - Check that both services are running on the same machine

### Common Issues

**"LLM not responding"**
- Check if Ollama is running: `ollama list`
- Restart the LLM service: `python services/llm/main.py`

**"Drone not moving"**
- Ensure drone GameObject has Rigidbody component
- Check if SimpleDroneController is attached
- Verify command parsing in Unity Console

**"UI not appearing"**
- Press Tab to toggle UI visibility
- Check if Canvas is enabled in scene
- Ensure TextMeshPro is installed in Unity

**"Connection refused"**
- Make sure both services are started in correct order
- Check IP addresses match (127.0.0.1)
- Verify ports are not in use by other applications

### Debug Mode

Enable verbose logging:
1. In Unity: Check "Development Build" in Build Settings
2. In LLM service: All debug messages are printed to console
3. Use Unity's Profiler to check performance

### Emergency Commands

- **Stop Drone**: Type "stop" or use emergency stop in SimpleDroneController
- **Reset Position**: Manually move drone in Unity Scene view
- **Restart Services**: Stop and restart both LLM and Unity

## Project Structure

```
drone-sim/
├── services/
│   ├── llm/
│   │   └── main.py          # LLM service with Flask API
│   └── detection/
│       └── main.py          # Future detection service
├── unity-client/
│   └── drone-env/
│       ├── Assets/
│       │   ├── Scripts/     # Custom Unity scripts
│       │   │   ├── DroneCommandUI.cs
│       │   │   └── SimpleHTTPServer.cs
│       │   └── ProfessionalAssets/
│       │       └── DronePack/  # Professional drone assets
│       └── Scenes/          # Unity scenes
├── venv/                    # Python virtual environment
├── requirements.txt         # Python dependencies
└── setup_and_run.sh       # Setup script
```

## Future Enhancements

- **Detection Service**: Integrate computer vision for obstacle avoidance
- **Multi-drone Control**: Control multiple drones simultaneously
- **Advanced Commands**: Add complex maneuvers and sequences
- **Voice Control**: Add speech-to-text input
- **Web Interface**: Create a web-based control interface
- **Recording**: Save and replay command sequences

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

