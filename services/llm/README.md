# Drone LLM Service

This service provides natural language processing for drone commands using either Ollama or OpenAI.

## Configuration

### Environment Variables

Set these environment variables to configure the service:

```bash
# Choose LLM provider: "ollama" or "openai"
export LLM_PROVIDER=ollama

# OpenAI Configuration (only needed if using OpenAI)
export OPENAI_API_KEY=your_openai_api_key_here
export OPENAI_MODEL=gpt-3.5-turbo

# Ollama Configuration (only needed if using Ollama)
export OLLAMA_MODEL=llama2
```

### Quick Setup

1. **Using Ollama (default):**
   ```bash
   export LLM_PROVIDER=ollama
   python http_service.py
   ```

2. **Using OpenAI:**
   ```bash
   export LLM_PROVIDER=openai
   export OPENAI_API_KEY=your_api_key_here
   python http_service.py
   ```

## Usage

### Start the HTTP Service
```bash
python http_service.py
```

The service will start on `http://127.0.0.1:5006`

### Available Endpoints

- `POST /process_command` - Process text commands
- `POST /process_audio_command` - Process audio commands (with Whisper)
- `GET /health` - Health check

### Switch Providers

Use the switch script to easily change providers:

```bash
# Switch to Ollama
python switch_provider.py ollama

# Switch to OpenAI
python switch_provider.py openai
```

## Features

- **Dual LLM Support**: Choose between Ollama (local) or OpenAI (cloud)
- **Gaze History Integration**: Commands can reference previously viewed objects
- **Audio Processing**: Whisper integration for voice commands
- **Coordinate Navigation**: Precise positioning using stored object coordinates

## Examples

The service can handle commands like:
- "Move to casual_Male_K" (navigates to specific object)
- "Go to Tree 1" (navigates to specific tree)
- "Fly to the last person I saw" (navigates to most recent person)
- "Move forward and go up" (basic movement commands)
