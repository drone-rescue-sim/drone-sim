using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class DroneCommandUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField commandInputField;
    public Button sendButton;
    public TextMeshProUGUI statusText;
    public GameObject inputPanel;

    [Header("Drone Controller")]
    public GameObject droneObject;

    [Header("Settings")]
    private string llmUrl = "http://127.0.0.1:5006/process_command";

    private void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(SendCommand);

        if (commandInputField != null)
        {
            commandInputField.onEndEdit.AddListener(delegate { SendCommand(); });
        }

        UpdateStatus("Ready to receive commands");
    }

    private void Update()
    {
        // Toggle UI with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputPanel != null)
                inputPanel.SetActive(!inputPanel.activeSelf);
        }
    }

    public void SendCommand()
    {
        string command = commandInputField.text.Trim();
        if (string.IsNullOrEmpty(command))
        {
            UpdateStatus("Please enter a command");
            return;
        }

        UpdateStatus("Sending command to LLM...");
        StartCoroutine(SendCommandToLLM(command));
        commandInputField.text = "";
    }

    private IEnumerator SendCommandToLLM(string userInput)
    {
        var requestData = new CommandRequest { input = userInput };
        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(llmUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    CommandResponse response = JsonUtility.FromJson<CommandResponse>(request.downloadHandler.text);
                    UpdateStatus("Command processed successfully");
                    ExecuteDroneCommand(response.command);
                }
                catch (System.Exception e)
                {
                    UpdateStatus("Error parsing LLM response: " + e.Message);
                    Debug.LogError("JSON Parse Error: " + request.downloadHandler.text);
                }
            }
            else
            {
                UpdateStatus("Failed to send command: " + request.error);
                Debug.LogError("HTTP Error: " + request.error);
            }
        }
    }

    private void ExecuteDroneCommand(string command)
    {
        if (droneObject == null) return;

        // Try to find PA_DroneController from professional drone pack
        try
        {
            var paDroneController = droneObject.GetComponentInChildren<PA_DronePack.PA_DroneController>();
            if (paDroneController != null)
            {
                ExecutePADroneCommand(paDroneController, command);
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PA_DronePack not available: {e.Message}");
        }

        // Fallback: Try to find SimpleDroneController
        var simpleDrone = droneObject.GetComponent<SimpleDroneController>();
        if (simpleDrone != null)
        {
            ExecuteSimpleDroneCommand(simpleDrone, command);
            return;
        }

        UpdateStatus("No compatible drone controller found");
    }

    private void ExecuteSimpleDroneCommand(SimpleDroneController controller, string command)
    {
        // Parse and execute command
        string[] parts = command.ToLower().Split(';');
        foreach (string part in parts)
        {
            string cmd = part.Trim();
            ExecuteSingleSimpleCommand(controller, cmd);
        }
    }

    private void ExecutePADroneCommand(PA_DronePack.PA_DroneController controller, string command)
    {
        // Parse and execute command
        string[] parts = command.ToLower().Split(';');
        foreach (string part in parts)
        {
            string cmd = part.Trim();
            ExecuteSinglePACommand(controller, cmd);
        }
    }

    // Method to simulate input on the professional drone controller
    private void SimulateDroneInput(PA_DronePack.PA_DroneController controller, float forward, float right, float up, float yaw)
    {
        // Try to find input components and simulate input
        try
        {
            // Look for PA_DroneAxisInput component
            var axisInput = controller.GetComponent<PA_DronePack.PA_DroneAxisInput>();
            if (axisInput != null)
            {
                // Use reflection to call input methods
                var inputType = axisInput.GetType();

                // Try different input method names that might exist
                string[] methodNames = { "SetInput", "UpdateInput", "ProcessInput", "HandleInput" };

                foreach (string methodName in methodNames)
                {
                    var method = inputType.GetMethod(methodName);
                    if (method != null)
                    {
                        // Try different parameter combinations
                        try
                        {
                            method.Invoke(axisInput, new object[] { forward, right, up, yaw });
                            Debug.Log($"Called {methodName} on PA_DroneAxisInput with forward:{forward}, right:{right}, up:{up}, yaw:{yaw}");
                            return;
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log($"Failed to call {methodName}: {e.Message}");
                        }
                    }
                }
            }

            // Try to find and use PAVR_3DJoystick if it exists
            var joystick = controller.GetComponentInChildren<PA_DronePack.PAVR_3DJoystick>();
            if (joystick != null)
            {
                // Simulate joystick input by directly setting values and invoking events
                joystick.zInput = forward;  // Forward/backward
                joystick.xInput = yaw;     // Rotation

                if (joystick.cyclicZAxis != null)
                    joystick.cyclicZAxis.Invoke(forward);

                if (joystick.cyclicXAxis != null)
                    joystick.cyclicXAxis.Invoke(yaw);

                Debug.Log("Simulated joystick input on professional drone");
            }
            else
            {
                Debug.LogWarning("No PAVR_3DJoystick found on drone");
            }

            // Alternative: Try to find UnityEvents and invoke them
            var controllerType = controller.GetType();

            // Look for input-related fields
            var fields = controllerType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.Contains("Input") || field.Name.Contains("Axis"))
                {
                    Debug.Log($"Found input field: {field.Name}");
                }
            }

            // Try to send messages to the controller
            if (up > 0)
                controller.SendMessage("Ascend", SendMessageOptions.DontRequireReceiver);
            else if (up < 0)
                controller.SendMessage("Descend", SendMessageOptions.DontRequireReceiver);

            if (forward > 0)
                controller.SendMessage("MoveForward", SendMessageOptions.DontRequireReceiver);
            else if (forward < 0)
                controller.SendMessage("MoveBackward", SendMessageOptions.DontRequireReceiver);

            if (right > 0)
                controller.SendMessage("MoveRight", SendMessageOptions.DontRequireReceiver);
            else if (right < 0)
                controller.SendMessage("MoveLeft", SendMessageOptions.DontRequireReceiver);

            if (yaw > 0)
                controller.SendMessage("TurnRight", SendMessageOptions.DontRequireReceiver);
            else if (yaw < 0)
                controller.SendMessage("TurnLeft", SendMessageOptions.DontRequireReceiver);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error simulating input on PA_DroneController: {e.Message}");
        }
    }

    private void ExecuteSingleSimpleCommand(SimpleDroneController controller, string command)
    {
        switch (command)
        {
            case "move_forward":
                controller.MoveForward();
                UpdateStatus("Moving forward");
                break;
            case "move_backward":
                controller.MoveBackward();
                UpdateStatus("Moving backward");
                break;
            case "move_left":
                controller.MoveLeft();
                UpdateStatus("Moving left");
                break;
            case "move_right":
                controller.MoveRight();
                UpdateStatus("Moving right");
                break;
            case "ascend":
            case "move_up":
                controller.MoveUp();
                UpdateStatus("Ascending");
                break;
            case "descend":
            case "move_down":
                controller.MoveDown();
                UpdateStatus("Descending");
                break;
            case "turn_left":
                controller.TurnLeft();
                UpdateStatus("Turning left");
                break;
            case "turn_right":
                controller.TurnRight();
                UpdateStatus("Turning right");
                break;
            case "stop":
                controller.StopMovement();
                UpdateStatus("Stopping");
                break;
            default:
                UpdateStatus("Unknown command: " + command);
                break;
        }
    }

    private void ExecuteSinglePACommand(PA_DronePack.PA_DroneController controller, string command)
    {
        // Use simulated input to control the professional drone
        float forward = 0f;
        float right = 0f;
        float up = 0f;
        float yaw = 0f;
        float intensity = 1f; // Default intensity

        switch (command)
        {
            case "move_forward":
                forward = intensity;
                UpdateStatus("Moving forward");
                break;
            case "move_backward":
                forward = -intensity;
                UpdateStatus("Moving backward");
                break;
            case "move_left":
                right = -intensity;
                UpdateStatus("Moving left");
                break;
            case "move_right":
                right = intensity;
                UpdateStatus("Moving right");
                break;
            case "ascend":
            case "move_up":
                up = intensity;
                UpdateStatus("Ascending");
                break;
            case "descend":
            case "move_down":
                up = -intensity;
                UpdateStatus("Descending");
                break;
            case "turn_left":
                yaw = -intensity;
                UpdateStatus("Turning left");
                break;
            case "turn_right":
                yaw = intensity;
                UpdateStatus("Turning right");
                break;
            case "stop":
                // Stop all movement by setting all inputs to 0
                UpdateStatus("Stopping");
                break;
            default:
                UpdateStatus($"Unknown command: {command}");
                return;
        }

        // Apply the simulated input
        SimulateDroneInput(controller, forward, right, up, yaw);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log("Drone UI: " + message);
    }

    [System.Serializable]
    private class CommandRequest
    {
        public string input;
    }

    [System.Serializable]
    private class CommandResponse
    {
        public string command;
        public string details;
    }
}
