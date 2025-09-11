using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Handles the command input UI for natural language drone control.
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
    public Button micButton;

    private bool isVisible = false;
    private InputAction toggleAction;
    private static readonly HttpClient httpClient = new HttpClient();
    private const string LLM_SERVICE_URL = "http://127.0.0.1:5006/process_command";

    // Voice recording variables
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string microphoneDevice;
    private const float maxRecordingTime = 5f; // Shorter recording time

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

    void OnDisable()
    {
        toggleAction?.Disable();
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

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        if (isVisible)
            HideUI();
        else
            ShowUI();
    }

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

    public void HideUI()
    {
        if (commandCanvas != null)
        {
            commandCanvas.gameObject.SetActive(false);
            isVisible = false;
        }
    }

    public void SendCommand()
    {
        if (commandInput == null || string.IsNullOrEmpty(commandInput.text))
        {
            return;
        }

        string command = commandInput.text.Trim();
        commandInput.text = "";
        HideUI();

        _ = SendToLLMServiceAsync(command);
    }

    public void StartVoiceRecording()
    {
        if (isRecording)
        {
            // If already recording, stop recording
            StopVoiceRecording();
            return;
        }

        // Check if microphone is available
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            return;
        }

        // Use the first available microphone
        microphoneDevice = Microphone.devices[0];
        Debug.Log($"Starting voice recording using device: {microphoneDevice}");

        // Start recording
        recordedClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingTime, 44100);
        isRecording = true;

        // Update microphone button appearance (if we have access to it)
        if (micButton != null)
        {
            // Change button color to indicate recording
            var buttonColors = micButton.colors;
            buttonColors.normalColor = Color.red;
            micButton.colors = buttonColors;
        }

        Debug.Log("Voice recording started. Click the microphone button again or wait for auto-stop.");

        // Auto-stop recording after maxRecordingTime seconds
        StartCoroutine(AutoStopRecording());
    }

    private void StopVoiceRecording()
    {
        if (!isRecording || recordedClip == null)
            return;

        // Stop recording
        Microphone.End(microphoneDevice);
        isRecording = false;

        Debug.Log("Voice recording stopped.");

        // Reset microphone button appearance
        if (micButton != null)
        {
            var buttonColors = micButton.colors;
            buttonColors.normalColor = new Color(0f, 0.5f, 1f, 1f); // Back to blue
            micButton.colors = buttonColors;
        }

        // Process the recorded audio with Whisper
        _ = ProcessRecordedAudioWithWhisperAsync();
    }

    private IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(maxRecordingTime);
        if (isRecording)
        {
            StopVoiceRecording();
        }
    }

    private async Task ProcessRecordedAudioWithWhisperAsync()
    {
        if (recordedClip == null)
        {
            Debug.LogError("No recorded audio to process!");
            return;
        }

        try
        {
            Debug.Log("Processing recorded audio with Whisper...");

            // Convert AudioClip to WAV format bytes
            byte[] audioData = ConvertAudioClipToWAV(recordedClip);

            // Send to Whisper service
            string whisperServiceUrl = "http://127.0.0.1:5006/process_audio_command";

            // Create multipart form data
            var content = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "audio", "voice_command.wav");

            Debug.Log("Sending audio to Whisper service for transcription...");

            // Increase timeout for Whisper processing (5 minutes for model download)
            httpClient.Timeout = TimeSpan.FromMinutes(6);
            HttpResponseMessage response = await httpClient.PostAsync(whisperServiceUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"Whisper service response: {responseContent}");

                // Parse the response to get the transcribed command
                var whisperResponse = JsonUtility.FromJson<WhisperResponse>(responseContent);
                if (whisperResponse != null && !string.IsNullOrEmpty(whisperResponse.transcript))
                {
                    Debug.Log($"Transcribed command: {whisperResponse.transcript}");

                    // Show the transcribed text in the input field
                    if (commandInput != null)
                    {
                        commandInput.text = whisperResponse.transcript;
                        commandInput.ActivateInputField();
                    }

                    // Auto-send the command if transcription confidence is high
                    if (whisperResponse.confidence > 0.8f)
                    {
                        Debug.Log("High confidence transcription, auto-sending command...");
                        SendCommand();
                    }
                    else
                    {
                        Debug.Log($"Transcription ready for review (confidence: {whisperResponse.confidence})");
                    }
                }
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.LogError($"Whisper service error: {errorContent}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to process recorded audio: {e.Message}");
        }
        finally
        {
            // Clean up
            recordedClip = null;
        }
    }

    private byte[] ConvertAudioClipToWAV(AudioClip clip)
    {
        if (clip == null) return null;

        // Get audio data
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert to 16-bit PCM
        byte[] wavData = new byte[44 + samples.Length * 2];

        // WAV header
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavData, 0);
        System.BitConverter.GetBytes(wavData.Length - 8).CopyTo(wavData, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavData, 8);
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavData, 12);
        System.BitConverter.GetBytes(16).CopyTo(wavData, 16); // Subchunk1Size
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 20); // AudioFormat (PCM)
        System.BitConverter.GetBytes((short)clip.channels).CopyTo(wavData, 22); // NumChannels
        System.BitConverter.GetBytes(clip.frequency).CopyTo(wavData, 24); // SampleRate
        System.BitConverter.GetBytes(clip.frequency * clip.channels * 2).CopyTo(wavData, 28); // ByteRate
        System.BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wavData, 32); // BlockAlign
        System.BitConverter.GetBytes((short)16).CopyTo(wavData, 34); // BitsPerSample
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavData, 36);
        System.BitConverter.GetBytes(samples.Length * 2).CopyTo(wavData, 40); // Subchunk2Size

        // Convert samples to 16-bit PCM
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(samples[i] * short.MaxValue);
            System.BitConverter.GetBytes(sample).CopyTo(wavData, 44 + i * 2);
        }

        return wavData;
    }

    private async Task SendToLLMServiceAsync(string command)
    {
        try
        {
            Debug.Log($"Sending command: {command}");

            var payload = new CommandPayload { command = command };
            string jsonPayload = JsonUtility.ToJson(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(LLM_SERVICE_URL, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.Log($"LLM service responded: {responseContent}");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Debug.LogError($"LLM service error: {errorContent}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to send command: {e.Message}");
        }
    }

    [System.Serializable]
    private class CommandPayload
    {
        public string command;
    }

    [System.Serializable]
    private class WhisperResponse
    {
        public string transcript;
        public float confidence;
    }
}
