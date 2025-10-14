using UnityEngine;
using UnityEngine.InputSystem; // NEW input system
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Controls drone movement and handles natural language commands from LLM service.
/// Manual input: 
/// - Arrow keys (↑↓←→) for movement
/// - Q key for turning left, E key for turning right
/// - Space for ascending, Shift for descending
/// LLM commands support: 
/// - Basic movement: move_forward/backward/left/right, ascend/descend, turn_left/right
/// - Precise turning: turn_180, turn_90_left, turn_45_right, turn_360
/// - Speed control: speed_50, speed_100, speed_25 (10-200% range)
/// - Hover mode: hover, no_hover
/// - Emergency stop: stop
/// Runs an HTTP server on port 5005 to receive commands from the LLM service.
/// Features hover mode for precise turning and configurable speed multipliers.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 10f;
    public float ascendSpeed = 6f;
    public float yawSpeed = 120f;
    public float accel = 8f;
    
    [Header("Advanced Controls")]
    [Tooltip("Speed multiplier for LLM commands (0.1 = 10% speed, 1.0 = 100% speed)")]
    public float llmSpeedMultiplier = 1.0f;
    
    [Tooltip("Enable hover mode - drone maintains position while turning")]
    public bool hoverMode = true;

    [Header("Collision Detection")]
    [Tooltip("Enable collision detection to prevent going through buildings")]
    public bool enableCollisionDetection = true;
    
    [Tooltip("Distance to check for collisions ahead of drone")]
    public float collisionCheckDistance = 2.0f;
    
    [Tooltip("Layers to check for collisions (buildings, obstacles)")]
    public LayerMask collisionLayers = -1;

    [Header("Optional visuals")]
    public Transform visual;
    public float tiltAmount = 15f;
    public float tiltLerp = 10f;

    private Rigidbody rb;
    private Vector3 velTarget;

    // New Input System actions
    private InputAction moveAction;     // ONLY Arrow keys or gamepad left stick (Vector2)
    private InputAction turnLeftAction;  // Cmd+Left Arrow for turning left
    private InputAction turnRightAction; // Cmd+Right Arrow for turning right
    private InputAction ascendPos;      // Cmd+Up Arrow for ascending
    private InputAction ascendNeg;      // Cmd+Down Arrow for descending
    // private InputAction ascendPos;      // DISABLED - Space = +1
    // private InputAction ascendNeg;      // DISABLED - LeftShift = -1
    // private InputAction yawLeft;        // DISABLED - Q
    // private InputAction yawRight;       // DISABLED - E

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
    private float commandTimeout = 2.0f; // seconds - increased for better control
    private Queue<string> commandQueue = new Queue<string>(); // Thread-safe command queue

    // Precise turning system
    private bool isTurningToAngle = false;
    private float targetYawAngle = 0f;
    private float turnSpeed = 90f; // degrees per second for precise turns
    private float turnTolerance = 2f; // degrees tolerance for reaching target

    void OnEnable()
    {
        // Move (only arrow keys + gamepad stick)
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        // Turn controls with Q and E keys (classic game controls)
        turnLeftAction = new InputAction("TurnLeft", binding: "<Keyboard>/q");
        turnRightAction = new InputAction("TurnRight", binding: "<Keyboard>/e");
        turnLeftAction.Enable();
        turnRightAction.Enable();

        // Ascend/Descend with Space and Shift keys
        ascendPos = new InputAction("AscendPos", binding: "<Keyboard>/space");
        ascendNeg = new InputAction("AscendNeg", binding: "<Keyboard>/leftShift");
        ascendPos.Enable();
        ascendNeg.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        turnLeftAction?.Disable();
        turnRightAction?.Disable();
        ascendPos?.Disable();
        ascendNeg?.Disable();
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
        // Process commands from the queue (thread-safe)
        while (commandQueue.Count > 0)
        {
            string command = commandQueue.Dequeue();
            ProcessCommand(command);
        }

        // Update command durations and reset expired commands
        UpdateCommandDurations();

        Vector2 mv = moveAction.ReadValue<Vector2>();
        
        // Manual ascend/descend with Space and Shift
        float upDown = 0f;
        if (ascendPos.IsPressed()) upDown += 1f;
        if (ascendNeg.IsPressed()) upDown -= 1f;

        // Combine manual (arrow keys only) and LLM inputs
        float totalForward = Mathf.Clamp(mv.y + llmMoveForward, -1f, 1f);
        float totalRight = Mathf.Clamp(mv.x + llmMoveRight, -1f, 1f);
        float totalUpDown = Mathf.Clamp(upDown + llmAscend, -1f, 1f);

        // Apply speed multiplier to LLM commands
        float manualForward = mv.y;
        float manualRight = mv.x;
        float manualUpDown = upDown;
        
        float llmForward = llmMoveForward * llmSpeedMultiplier;
        float llmRight = llmMoveRight * llmSpeedMultiplier;
        float llmUpDown = llmAscend * llmSpeedMultiplier;
        
        float finalForward = Mathf.Clamp(manualForward + llmForward, -1f, 1f);
        float finalRight = Mathf.Clamp(manualRight + llmRight, -1f, 1f);
        float finalUpDown = Mathf.Clamp(manualUpDown + llmUpDown, -1f, 1f);

        Vector3 planar   = transform.forward * finalForward * moveSpeed + transform.right * finalRight * moveSpeed;
        Vector3 vertical = Vector3.up * finalUpDown * ascendSpeed;
        
        // Hover mode: stop horizontal movement when turning precisely
        if (hoverMode && isTurningToAngle)
        {
            planar = Vector3.zero;
        }
        
        // Collision detection: prevent movement into buildings
        if (enableCollisionDetection)
        {
            planar = CheckCollisionAndAdjustMovement(planar);
        }
        
        velTarget = planar + vertical;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velTarget, Time.fixedDeltaTime * accel);

        // Handle rotation - either precise turning or continuous yaw
        float manualYaw = 0f;
        if (turnLeftAction.IsPressed()) manualYaw -= 1f;
        if (turnRightAction.IsPressed()) manualYaw += 1f;
        
        if (isTurningToAngle)
        {
            // Precise turning to specific angle
            HandlePreciseTurning();
        }
        else
        {
            // Continuous yaw from manual and LLM commands
            float totalYaw = Mathf.Clamp(manualYaw + llmYaw, -1f, 1f);
            if (Mathf.Abs(totalYaw) > 0.01f)
                transform.Rotate(Vector3.up, totalYaw * yawSpeed * Time.fixedDeltaTime, Space.World);
        }

        if (visual)
        {
            float roll  =  totalRight *  tiltAmount;
            float pitch = -totalForward *  tiltAmount;
            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, target, Time.fixedDeltaTime * tiltLerp);
        }
    }

    /// <summary>
    /// Starts the HTTP server to listen for commands from the LLM service.
    /// Runs on port 5005 and processes incoming drone movement commands.
    /// </summary>
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
                        // Add command to queue for processing on main thread
                        lock (commandQueue)
                        {
                            commandQueue.Enqueue(commandData.command);
                        }
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

    /// <summary>
    /// Checks for collisions ahead of the drone and adjusts movement to prevent going through buildings.
    /// Uses raycasting to detect obstacles in the movement direction.
    /// </summary>
    /// <param name="intendedMovement">The intended movement vector</param>
    /// <returns>Adjusted movement vector that avoids collisions</returns>
    private Vector3 CheckCollisionAndAdjustMovement(Vector3 intendedMovement)
    {
        if (intendedMovement.magnitude < 0.01f)
            return intendedMovement; // No movement, no collision check needed

        Vector3 movementDirection = intendedMovement.normalized;
        Vector3 checkPosition = transform.position + Vector3.up * 0.5f; // Check from center of drone
        
        // Cast ray in movement direction
        RaycastHit hit;
        if (Physics.Raycast(checkPosition, movementDirection, out hit, collisionCheckDistance, collisionLayers))
        {
            // Collision detected - reduce movement or stop
            float distanceToObstacle = hit.distance;
            float safetyMargin = 0.5f; // Stop 0.5 units before hitting obstacle
            
            if (distanceToObstacle <= safetyMargin)
            {
                // Too close to obstacle - stop movement in that direction
                return Vector3.zero;
            }
            else
            {
                // Reduce movement to stop before hitting obstacle
                float maxAllowedDistance = distanceToObstacle - safetyMargin;
                float speedReduction = maxAllowedDistance / collisionCheckDistance;
                return intendedMovement * speedReduction;
            }
        }
        
        return intendedMovement; // No collision detected, allow full movement
    }

    /// <summary>
    /// Handles precise turning to a specific angle.
    /// Automatically stops when the target angle is reached.
    /// </summary>
    private void HandlePreciseTurning()
    {
        float currentYaw = transform.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentYaw, targetYawAngle);
        
        if (Mathf.Abs(angleDifference) <= turnTolerance)
        {
            // Reached target angle
            isTurningToAngle = false;
            llmYaw = 0f;
            Debug.Log($"Reached target angle: {targetYawAngle:F1}°");
            return;
        }
        
        // Determine turn direction (shortest path)
        float turnDirection = Mathf.Sign(angleDifference);
        float turnAmount = turnSpeed * Time.fixedDeltaTime;
        
        // Don't overshoot
        if (Mathf.Abs(angleDifference) < turnAmount)
        {
            turnAmount = Mathf.Abs(angleDifference);
        }
        
        transform.Rotate(Vector3.up, turnDirection * turnAmount, Space.World);
    }

    /// <summary>
    /// Processes a natural language command and converts it to drone movement inputs.
    /// Supports multiple simultaneous commands for complex maneuvers.
    /// Now supports degree-based turning commands like "turn_180" or "turn_90_left".
    /// </summary>
    /// <param name="command">The command string from the LLM service</param>
    private void ProcessCommand(string command)
    {
        string lowerCommand = command.ToLower().Trim();
        Debug.Log($"Processing command: {lowerCommand}");

        // Check for degree-based turning commands first
        if (TryParseDegreeTurnCommand(lowerCommand))
        {
            return; // Command was handled as degree turn
        }
        
        // Check for speed control commands
        if (TryParseSpeedCommand(lowerCommand))
        {
            return; // Command was handled as speed control
        }

        // Parse standard commands - don't reset other commands, allow multiple simultaneous commands
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
                llmMoveForward = 0f;
                llmMoveRight = 0f;
                llmAscend = 0f;
                llmYaw = 0f;
                commandDurations.Clear();
                isTurningToAngle = false; // Stop precise turning too
                break;
            case "hover":
                hoverMode = true;
                Debug.Log("Hover mode enabled");
                break;
            case "no_hover":
                hoverMode = false;
                Debug.Log("Hover mode disabled");
                break;
            case "collision_on":
                enableCollisionDetection = true;
                Debug.Log("Collision detection enabled");
                break;
            case "collision_off":
                enableCollisionDetection = false;
                Debug.Log("Collision detection disabled");
                break;
            default:
                Debug.LogWarning($"Unknown command: {lowerCommand}");
                break;
        }
    }

    /// <summary>
    /// Attempts to parse degree-based turning commands.
    /// Supports formats like: "turn_180", "turn_90_left", "turn_45_right", "turn_360"
    /// </summary>
    /// <param name="command">The command string to parse</param>
    /// <returns>True if the command was a degree turn command</returns>
    private bool TryParseDegreeTurnCommand(string command)
    {
        // Pattern: turn_[degrees] or turn_[degrees]_[direction]
        if (!command.StartsWith("turn_"))
            return false;

        string[] parts = command.Split('_');
        if (parts.Length < 2)
            return false;

        // Try to parse the degree value
        if (float.TryParse(parts[1], out float degrees))
        {
            // Determine direction if specified
            string direction = parts.Length > 2 ? parts[2] : "";
            
            float targetAngle = transform.eulerAngles.y;
            
            if (direction == "left")
            {
                targetAngle -= degrees;
            }
            else if (direction == "right")
            {
                targetAngle += degrees;
            }
            else
            {
                // No direction specified, assume shortest path
                float currentYaw = transform.eulerAngles.y;
                float angleDifference = Mathf.DeltaAngle(currentYaw, currentYaw + degrees);
                targetAngle = currentYaw + angleDifference;
            }
            
            // Normalize angle to 0-360 range
            targetAngle = targetAngle % 360f;
            if (targetAngle < 0f) targetAngle += 360f;
            
            // Start precise turning
            isTurningToAngle = true;
            targetYawAngle = targetAngle;
            llmYaw = 0f; // Stop continuous yaw
            
            Debug.Log($"Starting precise turn to {targetYawAngle:F1}° (current: {transform.eulerAngles.y:F1}°)");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Attempts to parse speed control commands.
    /// Supports formats like: "speed_50", "speed_100", "speed_25"
    /// </summary>
    /// <param name="command">The command string to parse</param>
    /// <returns>True if the command was a speed control command</returns>
    private bool TryParseSpeedCommand(string command)
    {
        // Pattern: speed_[percentage]
        if (!command.StartsWith("speed_"))
            return false;

        string[] parts = command.Split('_');
        if (parts.Length < 2)
            return false;

        // Try to parse the speed percentage
        if (float.TryParse(parts[1], out float speedPercent))
        {
            // Clamp speed between 10% and 200%
            speedPercent = Mathf.Clamp(speedPercent, 10f, 200f);
            llmSpeedMultiplier = speedPercent / 100f;
            
            Debug.Log($"LLM speed multiplier set to {llmSpeedMultiplier:F2} ({speedPercent}%)");
            return true;
        }
        
        return false;
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

    /// <summary>
    /// Public method to stop all drone movement commands.
    /// Can be called from UI buttons or other components to emergency stop the drone.
    /// </summary>
    public void StopAllCommands()
    {
        llmMoveForward = 0f;
        llmMoveRight = 0f;
        llmAscend = 0f;
        llmYaw = 0f;
        commandDurations.Clear();
        isTurningToAngle = false; // Stop precise turning too
        Debug.Log("All drone commands stopped");
    }

    // Data class for JSON parsing
    [System.Serializable]
    private class CommandData
    {
        public string command;
    }
}
