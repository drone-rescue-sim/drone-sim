using UnityEngine;
using UnityEngine.InputSystem; // NEW input system
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 10f;
    public float ascendSpeed = 6f;
    public float yawSpeed = 120f;
    public float accel = 8f;

    [Header("Optional visuals")]
    public Transform visual;
    public float tiltAmount = 15f;
    public float tiltLerp = 10f;

    private Rigidbody rb;
    private Vector3 velTarget;

    // New Input System actions
    private InputAction moveAction;     // WASD/Arrows or gamepad left stick (Vector2)
    private InputAction ascendPos;      // Space = +1
    private InputAction ascendNeg;      // LeftShift = -1
    private InputAction yawLeft;        // Q
    private InputAction yawRight;       // E

    // LLM Command handling
    private HttpListener listener;
    private Thread listenerThread;
    private bool isRunning = false;

    // LLM command inputs (normalized -1 to 1)
    private float llmMoveForward = 0f;
    private float llmMoveRight = 0f;
    private float llmAscend = 0f;
    private float llmYaw = 0f;

    // Command duration tracking
    private Dictionary<string, float> commandDurations = new Dictionary<string, float>();
    private float commandTimeout = 1.0f; // seconds

    void OnEnable()
    {
        // Move (WASD/arrows + gamepad stick)
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        // Ascend / Descend (Space / LeftShift)
        ascendPos = new InputAction("AscendPos", binding: "<Keyboard>/space");
        ascendNeg = new InputAction("AscendNeg", binding: "<Keyboard>/leftShift");
        ascendPos.Enable(); ascendNeg.Enable();

        // Yaw (Q/E)
        yawLeft  = new InputAction("YawLeft",  binding: "<Keyboard>/q");
        yawRight = new InputAction("YawRight", binding: "<Keyboard>/e");
        yawLeft.Enable(); yawRight.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable(); ascendPos?.Disable(); ascendNeg?.Disable();
        yawLeft?.Disable(); yawRight?.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Start HTTP server for LLM commands
        StartHttpServer();
    }

    void OnDestroy()
    {
        StopHttpServer();
    }

    void FixedUpdate()
    {
        // Update command durations and reset expired commands
        UpdateCommandDurations();

        Vector2 mv = moveAction.ReadValue<Vector2>();
        float upDown = (ascendPos.IsPressed() ? 1f : 0f) + (ascendNeg.IsPressed() ? -1f : 0f);

        // Combine manual and LLM inputs
        float totalForward = Mathf.Clamp(mv.y + llmMoveForward, -1f, 1f);
        float totalRight = Mathf.Clamp(mv.x + llmMoveRight, -1f, 1f);
        float totalUpDown = Mathf.Clamp(upDown + llmAscend, -1f, 1f);

        Vector3 planar   = transform.forward * totalForward * moveSpeed + transform.right * totalRight * moveSpeed;
        Vector3 vertical = Vector3.up * totalUpDown * ascendSpeed;
        velTarget = planar + vertical;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velTarget, Time.fixedDeltaTime * accel);

        float manualYaw = (yawLeft.IsPressed() ? -1f : 0f) + (yawRight.IsPressed() ? 1f : 0f);
        float totalYaw = Mathf.Clamp(manualYaw + llmYaw, -1f, 1f);

        if (Mathf.Abs(totalYaw) > 0.01f)
            transform.Rotate(Vector3.up, totalYaw * yawSpeed * Time.fixedDeltaTime, Space.World);

        if (visual)
        {
            float roll  =  totalRight *  tiltAmount;
            float pitch = -totalForward *  tiltAmount;
            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, target, Time.fixedDeltaTime * tiltLerp);
        }
    }

    // HTTP Server Methods
    private void StartHttpServer()
    {
        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5005/");
            listener.Start();
            isRunning = true;

            listenerThread = new Thread(ListenForCommands);
            listenerThread.IsBackground = true;
            listenerThread.Start();

            Debug.Log("HTTP server started on port 5005");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start HTTP server: {e.Message}");
        }
    }

    private void StopHttpServer()
    {
        isRunning = false;
        if (listener != null)
        {
            listener.Stop();
            listener.Close();
        }
        if (listenerThread != null && listenerThread.IsAlive)
        {
            listenerThread.Join(1000);
        }
        Debug.Log("HTTP server stopped");
    }

    private void ListenForCommands()
    {
        while (isRunning)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"HTTP listener error: {e.Message}");
                }
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            if (request.HttpMethod == "POST")
            {
                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    Debug.Log($"Received command: {body}");

                    // Parse JSON command
                    var commandData = JsonUtility.FromJson<CommandData>(body);
                    if (commandData != null && !string.IsNullOrEmpty(commandData.command))
                    {
                        ProcessCommand(commandData.command);
                    }
                }
            }

            // Send response
            string responseString = "{\"status\": \"ok\"}";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing request: {e.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    // Command Processing
    private void ProcessCommand(string command)
    {
        string lowerCommand = command.ToLower().Trim();
        Debug.Log($"Processing command: {lowerCommand}");

        // Reset all LLM inputs
        llmMoveForward = 0f;
        llmMoveRight = 0f;
        llmAscend = 0f;
        llmYaw = 0f;

        // Parse command
        switch (lowerCommand)
        {
            case "move_forward":
                llmMoveForward = 1f;
                SetCommandDuration("move_forward");
                break;
            case "move_backward":
                llmMoveForward = -1f;
                SetCommandDuration("move_backward");
                break;
            case "move_left":
                llmMoveRight = -1f;
                SetCommandDuration("move_left");
                break;
            case "move_right":
                llmMoveRight = 1f;
                SetCommandDuration("move_right");
                break;
            case "ascend":
            case "go_up":
                llmAscend = 1f;
                SetCommandDuration("ascend");
                break;
            case "descend":
            case "go_down":
                llmAscend = -1f;
                SetCommandDuration("descend");
                break;
            case "turn_left":
                llmYaw = -1f;
                SetCommandDuration("turn_left");
                break;
            case "turn_right":
                llmYaw = 1f;
                SetCommandDuration("turn_right");
                break;
            case "stop":
                // Reset all movements
                break;
            default:
                Debug.LogWarning($"Unknown command: {lowerCommand}");
                break;
        }
    }

    private void SetCommandDuration(string command)
    {
        commandDurations[command] = Time.time + commandTimeout;
    }

    private void UpdateCommandDurations()
    {
        float currentTime = Time.time;
        List<string> expiredCommands = new List<string>();

        foreach (var kvp in commandDurations)
        {
            if (currentTime > kvp.Value)
            {
                expiredCommands.Add(kvp.Key);
            }
        }

        foreach (string cmd in expiredCommands)
        {
            commandDurations.Remove(cmd);
            // Reset the corresponding LLM input
            ResetCommandInput(cmd);
        }
    }

    private void ResetCommandInput(string command)
    {
        switch (command)
        {
            case "move_forward":
            case "move_backward":
                llmMoveForward = 0f;
                break;
            case "move_left":
            case "move_right":
                llmMoveRight = 0f;
                break;
            case "ascend":
            case "descend":
                llmAscend = 0f;
                break;
            case "turn_left":
            case "turn_right":
                llmYaw = 0f;
                break;
        }
    }

    // Data class for JSON parsing
    [System.Serializable]
    private class CommandData
    {
        public string command;
    }
}
