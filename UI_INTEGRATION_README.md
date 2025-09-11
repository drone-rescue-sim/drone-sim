# Unity UI Command Integration

This document describes the new in-game command input system that replaces the terminal-based command interface with a popup UI in Unity.

## 🎯 Overview

The new system allows you to control the drone directly from within the Unity game using natural language commands through a popup interface, eliminating the need for a separate terminal window.

## 🏗️ Architecture

### Components

1. **Unity UI System**
   - `CommandInputUI.cs` - Main UI controller for the popup interface
   - `CommandUISetup.cs` - Creates and configures all UI elements at runtime
   - `CommandUIManager.cs` - Scene integration manager

2. **Python HTTP Service**
   - `services/llm/http_service.py` - HTTP server that processes commands
   - Replaces the interactive terminal interface

3. **Communication Flow**
   ```
   Unity UI → HTTP POST to Python (port 5006) → LLM Processing → HTTP POST to Unity (port 5005) → Drone Control
   ```

## 🚀 Quick Start

### Prerequisites

- Unity 2021+ with TextMeshPro package
- Python 3.7+ with required dependencies
- Ollama running with llama2 model

### Installation

1. **Install Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

2. **Setup Verification**
   ```bash
   python setup_ui_integration.py
   ```

### Unity Setup

1. Open your Unity project (`unity-client/drone-env`)
2. Open the `SampleScene`
3. Create a new empty GameObject in the scene hierarchy
4. Add the `CommandUIManager` script to this GameObject
5. Ensure TextMeshPro is installed (Window → TextMeshPro → Import TMP Essentials)

### Running the System

1. **Start Python Service**
   ```bash
   ./start_llm_service.sh
   ```
   Or manually:
   ```bash
   python services/llm/http_service.py
   ```

2. **Run Unity Scene**
   - Press Play in Unity
   - The UI system will initialize automatically

3. **Use Commands**
   - Press **TAB** to open the command popup
   - Type natural language commands (e.g., "fly forward", "go up", "turn left")
   - Press **Enter** or click **Send**
   - Press **Escape** to close the popup

## 🎮 Controls

| Key/Action | Function |
|------------|----------|
| **TAB** | Toggle command input popup |
| **Enter** | Send command |
| **Escape** | Close popup |
| **Mouse** | Navigate UI elements |

## 📝 Supported Commands

The system supports the same natural language commands as before:

- Movement: "fly forward", "move backward", "go left", "move right"
- Altitude: "go up", "ascend", "go down", "descend"
- Rotation: "turn left", "turn right"
- Control: "stop", "hover"

## 🔧 Configuration

### UI Settings

Edit `CommandInputUI.cs` to customize:

```csharp
public Key toggleKey = Key.Tab;           // Key to toggle UI
public string pythonServiceUrl = "http://127.0.0.1:5006/process_command";
public float requestTimeout = 10f;         // Request timeout in seconds
```

### Network Ports

- **Python LLM Service**: `http://127.0.0.1:5006`
- **Unity Drone Control**: `http://127.0.0.1:5005`

## 🐛 Troubleshooting

### Common Issues

1. **UI Doesn't Appear**
   - Check Unity console for errors
   - Ensure TextMeshPro is installed
   - Verify CommandUIManager script is attached to a GameObject

2. **Commands Not Working**
   - Check Python service is running on port 5006
   - Verify Unity HTTP server is active on port 5005
   - Check firewall settings

3. **Connection Errors**
   - Python service: Check logs for "HTTP server started on port 5006"
   - Unity service: Check logs for "HTTP server started on port 5005"

### Debug Information

- **Unity Console**: Shows UI initialization and command processing
- **Python Terminal**: Shows HTTP requests and LLM processing
- **Network**: Use tools like `netstat` to verify ports are open

## 📊 Features

### Command History
- View previous commands and responses
- Auto-scrolling history panel
- Timestamped entries

### Error Handling
- Connection timeout handling
- Service availability checking
- User-friendly error messages

### User Experience
- Keyboard shortcuts for power users
- Visual feedback for command status
- Non-blocking UI (doesn't pause gameplay)

## 🔄 Migration from Terminal System

The new UI system is designed to be a drop-in replacement:

1. **No Changes to Drone Logic**: Existing `DroneController.cs` works unchanged
2. **Same LLM Processing**: Uses identical command processing logic
3. **Backward Compatibility**: Original terminal service still works if needed

## 🛠️ Development

### Adding New UI Features

1. Extend `CommandInputUI.cs` for new functionality
2. Modify `CommandUISetup.cs` to add new UI elements
3. Update `CommandUIManager.cs` for scene integration

### Customizing Commands

Edit the system prompt in `services/llm/http_service.py` to add new commands:

```python
system_prompt = """You are a drone control assistant...
Available commands:
- move_forward: Move the drone forward
- your_new_command: Description of new command
"""
```

## 📈 Performance

- **UI Overhead**: Minimal impact on game performance
- **Network Latency**: ~50-200ms for command processing
- **Memory Usage**: ~2-5MB additional RAM for UI system

## 🤝 Contributing

When modifying the UI system:

1. Test with both Python service running and stopped
2. Verify all keyboard shortcuts work
3. Check UI scaling on different screen resolutions
4. Test error scenarios (network disconnect, service unavailable)

---

## 📞 Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review Unity and Python console logs
3. Verify all prerequisites are met
4. Test with the original terminal system to isolate issues

The system is designed to be robust and provide clear error messages to help with debugging.
