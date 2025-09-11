# Button Setup Guide for CommandInputUI

This guide explains how to manually add Send and Close buttons to the CommandInputUI in Unity Editor.

## Prerequisites

1. Make sure you have the CommandInputUI script attached to a GameObject in your scene
2. Ensure you have a Canvas with a Panel containing the TMP_InputField
3. The CommandInputUI component should have references to:
   - `commandCanvas` (the Canvas GameObject)
   - `commandInput` (the TMP_InputField component)

## Step 1: Create the Buttons in Unity Editor

### For Send Button:
1. Right-click on your Panel GameObject in the Hierarchy
2. Select `UI > Button - TextMeshPro`
3. Name the button "SendButton"
4. Position it at the bottom of the panel (suggested: Anchor Preset "Bottom Center", Position Y: -60)

### For Close Button:
1. Right-click on your Panel GameObject in the Hierarchy
2. Select `UI > Button - TextMeshPro`
3. Name the button "CloseButton"
4. Position it next to the Send button (suggested: Anchor Preset "Bottom Center", Position X: 100, Y: -60)

## Step 2: Configure Button Appearance

### Send Button:
- **Text**: "Send"
- **Font Size**: 14
- **Color**: White text on green background
- **Size**: Width: 80, Height: 30

### Close Button:
- **Text**: "Close"
- **Font Size**: 14
- **Color**: White text on red background
- **Size**: Width: 80, Height: 30

## Step 3: Set Up Button Event Listeners

### Send Button Events:
1. Select the Send Button in the Hierarchy
2. In the Inspector, scroll down to the `Button` component
3. Click the `+` button under "On Click ()"
4. Drag the GameObject with `CommandInputUI` script into the Object field
5. From the dropdown, select `CommandInputUI > SendCommand`

### Close Button Events:
1. Select the Close Button in the Hierarchy
2. In the Inspector, scroll down to the `Button` component
3. Click the `+` button under "On Click ()"
4. Drag the GameObject with `CommandInputUI` script into the Object field
5. From the dropdown, select `CommandInputUI > HideUI`

## Step 4: Verify Setup

1. **Send Button Behavior**:
   - When clicked, should:
     - Send the command text to the LLM service
     - Clear the input field
     - Hide the UI popup immediately

2. **Close Button Behavior**:
   - When clicked, should:
     - Hide the UI popup
     - Keep the command text (no clearing)

3. **Input Field Behavior**:
   - Pressing Enter should also trigger SendCommand
   - Pressing Tab should toggle the UI visibility

## Troubleshooting

### Buttons Not Working:
1. Check that the CommandInputUI script is attached to the correct GameObject
2. Verify that the event listeners are properly assigned
3. Ensure the buttons are interactable (check the Button component)
4. Make sure there's an EventSystem in the scene

### UI Not Hiding After Send:
1. The SendCommand method automatically calls HideUI() after sending
2. Check the Unity Console for any error messages
3. Verify the commandCanvas reference is properly set

### LLM Service Connection Issues:
1. Make sure the LLM service is running on port 5006
2. Check the Unity Console for connection error messages
3. Verify the endpoint URL in the script matches your service

## File Structure After Setup

Your Hierarchy should look like this:
```
Canvas (with CommandInputUI script)
├── Panel
│   ├── Input (TMP_InputField)
│   ├── SendButton
│   │   └── Text (TMP)
│   └── CloseButton
│       └── Text (TMP)
```

## Additional Notes

- The UI starts hidden and can be toggled with the Tab key
- The Send button will hide the UI immediately after clicking
- All button logic has been removed from the programmatic setup
- Buttons must be created and configured manually in the Unity Editor
