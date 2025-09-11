# 🚀 Quick Start Guide - Unity UI Drone Control

Dette er en komplett guide for å komme i gang med det nye Unity UI-systemet for dronekontroll.

## 🎯 Oversikt

Det nye systemet lar deg kontrollere dronen direkte fra Unity med naturlig språk via en popup-grensesnitt, uten behov for terminal.

## ⚡ Hurtigstart (3 minutter)

### 1. Installer avhengigheter
```bash
pip install -r requirements.txt
```

### 2. Start tjenester
```bash
# Alternativ 1: Bash script (anbefalt)
./start_all_services.sh

# Alternativ 2: Python script
python start_services.py --wait
```

### 3. Åpne Unity
- Åpne `unity-client/drone-env` i Unity
- Åpne `SampleScene`
- Opprett et tomt GameObject
- Legg til `CommandUIManager` script
- Sørg for at TextMeshPro er installert

### 4. Kjør og test
- Trykk Play i Unity
- Trykk **TAB** for å åpne kommandopopup
- Skriv "fly forward" og trykk Enter
- Se dronen bevege seg! 🎉

## 📋 Detaljert oppsett

### Krav
- ✅ Python 3.7+
- ✅ Unity 2021+ med TextMeshPro
- ✅ Ollama med llama2 modell
- ✅ Flask, requests, andre avhengigheter

### Tjenester som kjører
- 🤖 **LLM HTTP Service**: Port 5006
- 🎮 **Unity Drone Control**: Port 5005 (intern)

## 🎮 Bruk

### Tastatursnarveier
- **TAB** - Åpne/lukke kommandopopup
- **Enter** - Send kommando
- **Escape** - Lukk popup

### UI-posisjonering
UI-et kan nå plasseres hvor som helst på skjermen:
- **Standard**: Øvre høyre hjørne (anbefalt - mindre forstyrrende)
- **Alternativ**: Midt på skjermen (sett `useTopRight = false` i CommandUISetup)
- **Custom**: Juster `customOffset` for nøyaktig plassering

### Eksempler på kommandoer
- "fly forward" → `move_forward`
- "go up" → `ascend`
- "turn left" → `turn_left`
- "stop" → `stop`

### Feilsøking

#### Tjenester starter ikke?
```bash
# Sjekk status
./start_all_services.sh status

# Start på nytt
./start_all_services.sh restart
```

#### Unity kompileringsfeil?
Hvis du får feil som `UnityWebRequest.Result.TimeoutError` ikke finnes:
- Dette er en Unity versjonskompatibilitetsfeil
- Koden er allerede fikset for å fungere med forskjellige Unity-versjoner
- Restart Unity Editor og prøv på nytt
- Sjekk at du bruker Unity 2019.3+ med TextMeshPro

#### TAB fungerer ikke / UI vises ikke?
- Sørg for at CommandUIManager er lagt til i scenen
- Installer TextMeshPro: Window → TextMeshPro → Import TMP Essentials
- Sjekk Unity Console for feilmeldinger (gul/orange meldinger)
- Legg til CommandUIDebug script for å diagnostisere problemet
- Alternativ: Bruk SimpleCommandUI script (krever ikke TextMeshPro)

#### Unity kobler ikke til?
- Sjekk at Python-tjeneste kjører på port 5006
- Kjør tilkoblingstest: `python test_connection.py`
- Sjekk Unity Console for feilmeldinger
- Sørg for at CommandUIManager er lagt til i scenen

#### Kommandoer fungerer ikke?
- Sjekk at Unity HTTP-server kjører på port 5005
- Se på LLM service logger: `tail -f llm_service.log`

## 🛠️ Avanserte kommandoer

### Bash script
```bash
# Start tjenester
./start_all_services.sh start

# Stopp tjenester
./start_all_services.sh stop

# Sjekk status
./start_all_services.sh status

# Restart
./start_all_services.sh restart
```

### Python script
```bash
# Start og hold kjøring
python start_services.py start --wait

# Bare start (uten å vente)
python start_services.py start

# Status
python start_services.py status

# Stopp
python start_services.py stop
```

## 📁 Filer som ble opprettet

### Scripts
- `start_all_services.sh` - Bash startup script
- `start_services.py` - Python startup script

### Unity Scripts
- `CommandInputUI.cs` - UI kontroller
- `CommandUISetup.cs` - UI oppsett
- `CommandUIManager.cs` - Scene manager

### Python Service
- `services/llm/http_service.py` - HTTP LLM service

### Dokumentasjon
- `UI_INTEGRATION_README.md` - Detaljert dokumentasjon
- `QUICK_START.md` - Denne filen

## 🔧 Tilpasning

### Endre port
Rediger i `services/llm/http_service.py`:
```python
app.run(host='127.0.0.1', port=5006, ...)
```

### Tilpass UI
Rediger `CommandInputUI.cs`:
```csharp
public Key toggleKey = Key.Tab;
public string pythonServiceUrl = "http://127.0.0.1:5006/process_command";
```

## 🎉 Ferdig!

Du har nå et komplett system for å kontrollere dronen med naturlig språk direkte i Unity! 🚁✨

### Neste steg
1. Eksperimenter med forskjellige kommandoer
2. Tilpass UI-et etter behov
3. Legg til nye kommandoer i LLM-prompten
4. Bygg videre på systemet

---

Har du spørsmål? Sjekk `UI_INTEGRATION_README.md` for mer detaljer.
