# Unity UI Troubleshooting Guide

## 🎯 Problem: TAB doesn't show the command popup

### Quick Checklist:
- [ ] CommandUIManager script attached to a GameObject in the scene?
- [ ] TextMeshPro installed? (Window → TextMeshPro → Import TMP Essentials)
- [ ] Any yellow/orange warnings in Unity Console?
- [ ] Python LLM service running? (Check with `python start_services.py status`)

### Step-by-Step Troubleshooting:

#### 1. Verify TextMeshPro Installation
1. Open Unity
2. Go to: Window → TextMeshPro → Import TMP Essentials
3. Wait for import to complete
4. Restart Unity

#### 2. Check Scene Setup
1. Open your scene (SampleScene)
2. Create empty GameObject: GameObject → Create Empty
3. Name it "CommandManager"
4. Add CommandUIManager script to it
5. Press Play

#### 3. Use Debug Script (Recommended)
1. Add CommandUIDebug script to any GameObject
2. Press Play
3. Check Unity Console for detailed diagnostics
4. Press TAB and see what happens in console

#### 4. Fallback: Simple UI (No TextMeshPro Required)
If TextMeshPro is the problem:
1. Remove CommandUIManager from GameObject
2. Add SimpleCommandUI script instead
3. Press Play
4. TAB should work now

#### 5. Check Console Errors
Look for these common issues:
- `TextMeshPro not found` → Install TextMeshPro
- `CommandInputUI not found` → Scripts not attached properly
- `Canvas not active` → UI creation failed

### Expected Console Output (Normal):
```
🤖 Command UI Manager initialized. Press Tab to open command input.
🎮 Simple Command UI setup complete!
```

### Test Commands:
- TAB: Toggle UI
- Type "fly forward"
- Enter: Send command
- Escape: Close UI

### Still Not Working?
1. Try the Simple UI fallback
2. Check Unity version (2019.3+ recommended)
3. Restart Unity completely
4. Check if other scripts conflict

### Need Help?
Run: `python demo.py` for complete setup guide!
