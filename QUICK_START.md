# 🚁 Drone Simulation - Quick Start Guide

## For noen som har klonet repoet

### 📋 **Forutsetninger:**
- ✅ Unity installert
- ✅ Ollama installert med llama2 modell
- ✅ Python 3.8+ (sjekk med `python3 --version`)

### 🛠️ **Steg-for-steg oppsett:**

#### **1. Installer Python-avhengigheter:**

```bash
# Naviger til prosjektmappen
cd drone-sim

# Opprett virtual environment
python3 -m venv venv

# Aktiver virtual environment
source venv/bin/activate

# Installer alle biblioteker
pip install -r requirements.txt
```

#### **2. Start Ollama og last ned modell:**

```bash
# Start Ollama (i en egen terminal)
ollama serve

# Last ned llama2 modellen (i en annen terminal)
ollama pull llama2
```

#### **3. Start LLM-tjenesten:**

```bash
# I prosjektmappen, aktiver venv og start tjenesten
cd drone-sim
source venv/bin/activate
python services/llm/main.py --flask
```

#### **4. I Unity:**

1. **Åpne prosjektet:**
   - Åpne `unity-client/drone-env/` i Unity Hub

2. **Åpne SampleScene:**
   - Finn og åpne `SampleScene`

3. **Kjør automatisk oppsett:**
   - Opprett et nytt GameObject kalt "SceneSetup"
   - Legg til `SetupDemoScene.cs` script på det
   - Høyreklikk på scriptet → **"Setup Complete Demo Scene"**

4. **Alternativt manuelt oppsett:**
   - Legg til drone fra `Assets/ProfessionalAssets/DronePack/Prefabs/_Drone [Quad]`
   - Legg til `DroneCommandUI.cs` og `SimpleHTTPServer.cs` på et GameObject
   - Koble dronen til `DroneCommandUI.droneObject`

#### **5. Test systemet:**

```bash
# I Unity: Trykk Play
# Trykk Tab for å vise kommando-UI
# Skriv: "fly forward"
# Trykk Enter
```

### 🎯 **Hvis noe går galt:**

#### **Problem: "ModuleNotFoundError"**
```bash
# Sørg for at venv er aktivert
source venv/bin/activate
pip install -r requirements.txt
```

#### **Problem: "ollama command not found"**
```bash
# Finn hvor ollama er installert
which ollama
# Eller legg til i PATH
export PATH="/usr/local/bin:$PATH"
```

#### **Problem: LLM-tjenesten starter ikke**
```bash
# Sjekk at Ollama kjører
ollama list
# Restart LLM-tjeneste
python services/llm/main.py --flask
```

#### **Problem: Unity viser feilmeldinger**
```
# Sjekk Console i Unity for feilmeldinger
# Sørg for at alle scripts er lagt til riktige GameObjects
# Sjekk at dronen har PA_DroneController komponent
```

### 📁 **Prosjektstruktur:**

```
drone-sim/
├── services/llm/main.py          # LLM-tjeneste
├── unity-client/drone-env/       # Unity-prosjekt
│   ├── Assets/Scripts/           # Unity-scripts
│   └── ProfessionalAssets/       # Drone-pakke
├── venv/                         # Python virtual environment
└── requirements.txt              # Python-avhengigheter
```

### 🚀 **Kjør alle kommandoer i riktig rekkefølge:**

```bash
# Terminal 1: Start Ollama
ollama serve

# Terminal 2: Last ned modell
ollama pull llama2

# Terminal 3: Python oppsett og start
cd drone-sim
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
python services/llm/main.py --flask

# Unity: Åpne og spill
```

### 🎉 **Ferdig!**

Når alt er satt opp, kan du:
- Skrive naturlige språk-kommandoer i Unity
- Bruke intensity-kommandoer som "fly very long forward"
- Se dronen reagere på kommandoene dine!

### 💡 **Tips:**

- Hold LLM-tjenesten kjørende i bakgrunnen
- Bruk `--flask` flagget for å kjøre kun HTTP-server
- Test først med enkle kommandoer som "fly forward"
- Sjekk Unity Console for debug-meldinger
