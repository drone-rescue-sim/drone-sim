using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Handles the command input UI for natural language drone control.
/// Manages UI visibility, input handling, and communication with the LLM service.
/// </summary>
public class CommandInputUI : MonoBehaviour
{
    public Canvas commandCanvas;
    private TMP_InputField _commandInput;
    public TMP_InputField commandInput
    {
        get { return _commandInput; }
        set
        {
            _commandInput = value;
            if (_commandInput != null)
            {
                SetupInputFieldEvents();
            }
        }
    }
    public Button sendButton;
    public Button closeButton;

    private bool isVisible = false;
    private InputAction toggleAction;
    private static readonly HttpClient httpClient = new HttpClient();
    private const string LLM_SERVICE_URL = "http://127.0.0.1:5006/process_command";

    // Serializable class for JSON payload
    [System.Serializable]
    private class CommandPayload
    {
        public string command;
    }

    void Awake()
    {
        toggleAction = new InputAction("ToggleUI", binding: "<Keyboard>/tab");
        toggleAction.Enable();

        if (commandCanvas != null)
            commandCanvas.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        toggleAction?.Enable();
        SetupInputFieldEvents();
    }

    void SetupInputFieldEvents()
    {
        if (commandInput != null)
        {
            // Remove existing listeners to avoid duplicates
            commandInput.onSubmit.RemoveAllListeners();
            commandInput.onEndEdit.RemoveAllListeners();

            commandInput.onSubmit.AddListener(delegate { SendCommand(); });
            commandInput.onEndEdit.AddListener(delegate(string text) {
                // Keep focus on input field for quick successive commands
                if (!string.IsNullOrEmpty(text))
                {
                    commandInput.ActivateInputField();
                }
            });
        }
    }

    void OnDisable()
    {
        toggleAction?.Disable();
    }

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            ToggleUI();
        }
    }

    /// <summary>
    /// Toggles the visibility of the command input UI.
    /// Called when the TAB key is pressed.
    /// </summary>
    public void ToggleUI()
    {
        if (isVisible)
            HideUI();
        else
            ShowUI();
    }

    /// <summary>
    /// Shows the command input UI and focuses the input field.
    /// </summary>
    public void ShowUI()
    {
        if (commandCanvas != null)
        {
            commandCanvas.gameObject.SetActive(true);
            isVisible = true;

            if (commandInput != null)
            {
                commandInput.Select();
                commandInput.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// Hides the command input UI.
    /// </summary>
    public void HideUI()
    {
        if (commandCanvas != null)
        {
            commandCanvas.gameObject.SetActive(false);
            isVisible = false;
        }
    }

    /// <summary>
    /// Processes and sends the user's command to the LLM service.
    /// Clears the input field and hides the UI immediately after sending.
    /// </summary>
    public void SendCommand()
    {
        if (commandInput == null)
        {
            Debug.LogError("❌ commandInput is null!");
            return;
        }

        if (string.IsNullOrEmpty(commandInput.text))
        {
            return;
        }

        string command = commandInput.text.Trim();
        commandInput.text = ""; // Clear input

        // Hide UI immediately after sending command
        HideUI();

        // Send to LLM service for processing
        _ = SendToLLMServiceAsync(command);
    }


    /// <summary>
    /// Asynchronously sends the command to the LLM service for processing.
    /// Handles HTTP communication and error logging.
    /// </summary>
    /// <param name="command">The natural language command to send</param>
    private async Task SendToLLMServiceAsync(string command)
    {
        try
        {
            Debug.Log($"📤 Sending command to LLM service: {command}");

            var payload = new CommandPayload { command = command };
            string jsonPayload = JsonUtility.ToJson(payload);
            // Debug.Log($"📄 JSON payload: {jsonPayload}"); // Commented out to reduce log spam
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(LLM_SERVICE_URL, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"✅ LLM service responded: {responseContent}");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.LogError($"❌ LLM service error ({response.StatusCode}): {errorContent}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to send command to LLM service: {e.Message}");
            Debug.Log("💡 Make sure the LLM service is running on port 5006");
        }
    }

}
