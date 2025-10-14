# LLM og Whisper Integrasjon med Unity

Denne README forklarer hvordan LLM (Large Language Model) og Whisper integrasjonen fungerer med Unity og popup-funksjonaliteten.

## 🏗️ Arkitektur Oversikt

```
🎮 Unity Game ──────HTTP──────► 🐍 Python Flask Server ───API───► 🤖 LLM (Ollama/OpenAI)
     │                              │                           │
     │                              │                           │
     │                     ┌────────▼────────┐         ┌────────▼────────┐
     │                     │   Text Commands │         │  Natural Lang   │
     │                     │   (Port 5006)   │         │   Processing    │
     │                     └────────┬───────┘         └────────┬─────────┘
     │                              │                           │
     │                     ┌────────▼────────┐         ┌────────▼────────┐
🎤 Voice Recording         │  Audio Commands │         │   Command        │
     │                     │   (Whisper)     │         │   Generation     │
     │                     └────────┬───────┘          └────────┬─────────┘
     │                              │                           │
     │                              └───────────HTTP───────────┘
     │                                        │
     │                              ┌────────▼────────┐
     │                              │   Drone         │
     │                              │   Controller    │
     │                              │   (Port 5005)   │
     │                              └─────────────────┘
```

## 🚀 Komponenter

### 1. Unity Side (Frontend)
- **CommandInputUI.cs**: Håndterer popup UI for kommandoer
- **DroneController.cs**: Mottar og utfører kommandoer fra LLM
- **CommandUISetup.cs**: Setter opp UI elementer automatisk

### 2. Python Flask Server (Backend)
- **http_service.py**: HTTP API server (Port 5006)
- **main.py**: LLM logikk og kommando prosessering
- **Whisper**: Tale-til-tekst konvertering

### 3. LLM Providers
- **Ollama**: Lokal LLM (anbefalt)
- **OpenAI**: Cloud-basert LLM

## 🎯 Popup Funksjonalitet

### Hvordan popup fungerer:

1. **Aktivering**: Trykk `TAB` tast for å åpne/lukke popup
2. **UI Elementer**:
   - Input felt for tekst kommandoer
   - Send knapp (grønn)
   - Close knapp (rød)
   - Mikrofon knapp (blå) for stemme kommandoer

3. **Kommando Flyt**:
   ```
   Bruker skriver kommando → Send → LLM prosesserer → Drone utfører
   ```

## 🎤 Whisper Integrasjon

### Stemme-til-tekst prosess:

1. **Opptak**: Unity starter mikrofon opptak (maks 5 sekunder)
2. **Konvertering**: Audio konverteres til WAV format
3. **Whisper**: Python server prosesserer audio med Whisper
4. **Transkripsjon**: Tekst returneres til Unity
5. **Auto-send**: Hvis confidence > 80%, sendes kommando automatisk

### Whisper Setup:
```bash
# Installer Whisper
pip install openai-whisper

# Første gang kan ta tid (laster ned modell)
python -m whisper audio.wav --model tiny
```

## 🔧 Konfigurasjon

### 1. LLM Provider Setup

**Ollama (Anbefalt - Lokal)**:
```bash
# Installer Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama
ollama serve

# Last ned modell
ollama pull llama2

# Konfigurer
export LLM_PROVIDER=ollama
export OLLAMA_MODEL=llama2
```

**OpenAI (Cloud)**:
```bash
export LLM_PROVIDER=openai
export OPENAI_API_KEY=your_api_key_here
export OPENAI_MODEL=gpt-4o-mini
```

### 2. Start Services

```bash
# Start Python server
cd services/llm
python http_service.py

# Start Unity scene med drone
# Popup vil være tilgjengelig med TAB tast
```

## 📡 API Endpoints

### Text Commands
```http
POST http://127.0.0.1:5006/process_command
Content-Type: application/json

{
  "command": "fly forward and go up"
}
```

### Audio Commands
```http
POST http://127.0.0.1:5006/process_audio_command
Content-Type: multipart/form-data

audio: [WAV file data]
```

### Health Check
```http
GET http://127.0.0.1:5006/health
```

## 🎮 Unity Integration

### CommandInputUI Funksjoner:

1. **ToggleUI()**: Åpner/lukker popup med TAB
2. **SendCommand()**: Sender tekst kommando til LLM
3. **StartVoiceRecording()**: Starter mikrofon opptak
4. **ProcessRecordedAudioWithWhisperAsync()**: Prosesserer audio

### DroneController Funksjoner:

1. **HTTP Server**: Lytter på port 5005 for kommandoer
2. **Command Queue**: Buffer kommandoer for smooth utførelse
3. **JSON Parsing**: Håndterer både enkelt og multiple kommandoer

## 🔄 Kommando Flyt

### Tekst Kommando:
```
Unity Popup → HTTP POST → Flask Server → LLM → Drone Commands → Unity Drone
```

### Stemme Kommando:
```
Unity Microphone → WAV Audio → Whisper → Text → LLM → Drone Commands → Unity Drone
```

## 🎯 Kommando Eksempler

### Grunnleggende:
- "fly forward"
- "go up"
- "turn left"
- "stop"

### Avanserte (med gaze history):
- "go to the last person I saw"
- "navigate to Tree 1"
- "fly to casual_Male_K"

### Kombinert:
- "move forward and go up"
- "turn right then fly forward"

## 🐛 Troubleshooting

### Popup vises ikke:
- Sjekk at `CommandUISetup` script er på GameObject
- Trykk TAB for å aktivere
- Sjekk Console for feilmeldinger

### LLM svarer ikke:
- Sjekk at Python server kjører på port 5006
- Test med: `curl http://127.0.0.1:5006/health`
- Sjekk LLM provider konfigurasjon

### Whisper fungerer ikke:
- Sjekk at Whisper er installert: `pip install openai-whisper`
- Første gang kan ta lang tid (modell download)
- Sjekk mikrofon tillatelser

### Unity mottar ikke kommandoer:
- Sjekk at DroneController HTTP server kjører på port 5005
- Sjekk Unity Console for HTTP feilmeldinger
- Test med: `curl -X POST http://127.0.0.1:5005/`

## 📝 Logging

Alle komponenter har detaljert logging:
- **Unity**: Console med emoji prefiks (🚀, 📤, ✅, ❌)
- **Python**: Console med emoji prefiks (🎤, 📝, 🔄)
- **LLM**: Kommando prosessering og gaze history

## 🔧 Utvikling

### Legge til nye kommandoer:
1. Oppdater `get_drone_instructions()` i `main.py`
2. Legg til kommando i `DroneController.cs`
3. Test med popup i Unity

### Endre LLM modell:
```bash
# Ollama
ollama pull llama3
export OLLAMA_MODEL=llama3

# OpenAI
export OPENAI_MODEL=gpt-4
```

### Endre Whisper modell:
Endre i `http_service.py`:
```python
'--model', 'base',  # tiny, base, small, medium, large
```

Denne integrasjonen gir en smooth og intuitiv måte å kontrollere drone med både tekst og stemme kommandoer, med intelligent kontekst fra gaze tracking.
