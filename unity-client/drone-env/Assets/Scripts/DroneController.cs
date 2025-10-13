using UnityEngine;
using UnityEngine.InputSystem; // NEW input system
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controls drone movement and handles natural language commands from LLM service.
/// Manual input: Arrow keys (↑↓←→) for movement, Q/E for rotation.
/// LLM commands support: move_forward/backward/left/right, ascend/descend, turn_left/right.
/// Runs an HTTP server on port 5005 to receive commands from the LLM service.
/// </summary>
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
    private InputAction moveAction;     // ONLY Arrow keys or gamepad left stick (Vector2)
    // private InputAction ascendPos;      // DISABLED - Space = +1
    // private InputAction ascendNeg;      // DISABLED - LeftShift = -1
    private InputAction yawLeft;        // Q key for left rotation
    private InputAction yawRight;       // E key for right rotation

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

    // Navigation system
    private bool isNavigating = false;
    private Vector3 navigationTarget;
    private Vector3 lookAtTarget;
    private float navigationSpeed = 8f;
    
    // Rotation
    private bool isRotating = false;
    private Quaternion rotationTarget;
    private float navigationTolerance = 1f; // How close to get to target
    private float lookAtTolerance = 5f; // How close to get to look-at target

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

        // Ascend / Descend DISABLED - only LLM commands for vertical movement
        // ascendPos = new InputAction("AscendPos", binding: "<Keyboard>/space");
        // ascendNeg = new InputAction("AscendNeg", binding: "<Keyboard>/leftShift");

        // Yaw controls - Q and E keys for rotation
        yawLeft  = new InputAction("YawLeft",  binding: "<Keyboard>/q");
        yawRight = new InputAction("YawRight", binding: "<Keyboard>/e");
        yawLeft.Enable();
        yawRight.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        // ascendPos?.Disable(); ascendNeg?.Disable();
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
        // Process commands from the queue (thread-safe)
        while (commandQueue.Count > 0)
        {
            string command = commandQueue.Dequeue();
            ProcessCommand(command);
        }

        // Update command durations and reset expired commands
        UpdateCommandDurations();

        // Handle navigation
        if (isNavigating)
        {
            HandleNavigation();
        }
        
        // Handle rotation
        if (isRotating)
        {
            HandleRotation();
        }

        // Manual movement controls (disabled when command UI is visible)
        Vector2 mv = Vector2.zero;
        if (!CommandInputUI.IsUIVisible)
        {
            mv = moveAction.ReadValue<Vector2>();
        }
        
        // Manual ascend/descend DISABLED - only LLM commands control vertical movement
        float upDown = 0f; // Space/Shift keys disabled

        // Combine manual (arrow keys only) and LLM inputs
        float totalForward = Mathf.Clamp(mv.y + llmMoveForward, -1f, 1f);
        float totalRight = Mathf.Clamp(mv.x + llmMoveRight, -1f, 1f);
        float totalUpDown = Mathf.Clamp(upDown + llmAscend, -1f, 1f);

        Vector3 planar   = transform.forward * totalForward * moveSpeed + transform.right * totalRight * moveSpeed;
        Vector3 vertical = Vector3.up * totalUpDown * ascendSpeed;
        velTarget = planar + vertical;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velTarget, Time.fixedDeltaTime * accel);

        // Manual yaw controls - Q and E keys for rotation (disabled when command UI is visible)
        float manualYaw = 0f;
        if (!CommandInputUI.IsUIVisible)
        {
            if (yawLeft.IsPressed()) manualYaw -= 1f;
            if (yawRight.IsPressed()) manualYaw += 1f;
        }
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
            // Handle different endpoints
            if (request.Url.AbsolutePath == "/gaze_history")
            {
                HandleGazeHistoryRequest(request, response);
                return;
            }
            else if (request.Url.AbsolutePath == "/gaze_history_by_name")
            {
                HandleGazeHistoryByNameRequest(request, response);
                return;
            }
            else if (request.Url.AbsolutePath == "/gaze_history_recent")
            {
                HandleGazeHistoryRecentRequest(request, response);
                return;
            }
            else if (request.HttpMethod == "POST")
            {
                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    Debug.Log($"📥 [DRONE] Received command from LLM: {body}");

                    // Parse JSON command
                    var commandData = JsonUtility.FromJson<CommandData>(body);
                    if (commandData != null)
                    {
                        // Handle multiple commands (new format)
                        if (commandData.commands != null && commandData.commands.Length > 0)
                        {
                            Debug.Log($"🎯 [DRONE] Processing {commandData.commands.Length} commands: [{string.Join(", ", commandData.commands)}]");
                            
                            // Add all commands to queue for processing on main thread
                            lock (commandQueue)
                            {
                                foreach (string cmd in commandData.commands)
                                {
                                    if (!string.IsNullOrEmpty(cmd))
                                    {
                                        Debug.Log($"➕ [DRONE] Adding command to queue: '{cmd}'");
                                        commandQueue.Enqueue(cmd);
                                    }
                                }
                            }
                        }
                        // Handle single command (backward compatibility)
                        else if (!string.IsNullOrEmpty(commandData.command))
                        {
                            lock (commandQueue)
                            {
                                commandQueue.Enqueue(commandData.command);
                            }
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
    /// Processes a natural language command and converts it to drone movement inputs.
    /// Supports multiple simultaneous commands for complex maneuvers.
    /// </summary>
    /// <param name="command">The command string from the LLM service</param>
    private void ProcessCommand(string command)
    {
        string lowerCommand = command.ToLower().Trim();
        Debug.Log($"⚡ [DRONE] Processing command: '{lowerCommand}'");

        // Parse command - don't reset other commands, allow multiple simultaneous commands
        switch (lowerCommand)
        {
            case "move_forward":
                llmMoveForward = 1f;
                SetCommandDuration("move_forward");
                Debug.Log($"🚁 [DRONE] Executing: move_forward (llmMoveForward = {llmMoveForward})");
                break;
            case "move_backward":
                llmMoveForward = -1f;
                SetCommandDuration("move_backward");
                Debug.Log($"🚁 [DRONE] Executing: move_backward (llmMoveForward = {llmMoveForward})");
                break;
            case "move_left":
                llmMoveRight = -1f;
                SetCommandDuration("move_left");
                Debug.Log($"🚁 [DRONE] Executing: move_left (llmMoveRight = {llmMoveRight})");
                break;
            case "move_right":
                llmMoveRight = 1f;
                SetCommandDuration("move_right");
                Debug.Log($"🚁 [DRONE] Executing: move_right (llmMoveRight = {llmMoveRight})");
                break;
            case "ascend":
            case "go_up":
                llmAscend = 1f;
                SetCommandDuration("ascend");
                Debug.Log($"🚁 [DRONE] Executing: ascend (llmAscend = {llmAscend})");
                break;
            case "descend":
            case "go_down":
                llmAscend = -1f;
                SetCommandDuration("descend");
                Debug.Log($"🚁 [DRONE] Executing: descend (llmAscend = {llmAscend})");
                break;
            case "turn_left":
                llmYaw = -1f;
                SetCommandDuration("turn_left");
                Debug.Log($"🚁 [DRONE] Executing: turn_left (llmYaw = {llmYaw})");
                break;
            case "turn_right":
                llmYaw = 1f;
                SetCommandDuration("turn_right");
                Debug.Log($"🚁 [DRONE] Executing: turn_right (llmYaw = {llmYaw})");
                break;
            case "stop":
                // Reset all movements
                llmMoveForward = 0f;
                llmMoveRight = 0f;
                llmAscend = 0f;
                llmYaw = 0f;
                commandDurations.Clear();
                // Also stop navigation
                isNavigating = false;
                Debug.Log($"🛑 [DRONE] Executing: stop (all movements reset)");
                break;
            default:
                // Check if it's a navigation command
                if (lowerCommand.StartsWith("navigate_to_position:"))
                {
                    Debug.Log($"🧭 [DRONE] Processing navigation command: {lowerCommand}");
                    ProcessNavigationCommand(lowerCommand);
                }
                // Check if it's a precise coordinate command
                else if (lowerCommand.StartsWith("move_to_coordinates:"))
                {
                    ProcessCoordinateCommand(lowerCommand);
                }
                // Check if it's a precise rotation command
                else if (lowerCommand.StartsWith("rotate_to:"))
                {
                    ProcessRotationCommand(lowerCommand);
                }
                else
                {
                    Debug.LogWarning($"Unknown command: {lowerCommand}");
                }
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
        Debug.Log("All drone commands stopped");
    }

    /// <summary>
    /// Processes navigation commands in format: navigate_to_position:x,y,z,lookAtX,lookAtY,lookAtZ
    /// </summary>
    private void ProcessNavigationCommand(string command)
    {
        try
        {
            // Parse the command: navigate_to_position:x,y,z,lookAtX,lookAtY,lookAtZ
            string[] parts = command.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogError($"Invalid navigation command format: {command}");
                return;
            }

            string[] coords = parts[1].Split(',');
            if (coords.Length != 6)
            {
                Debug.LogError($"Invalid navigation coordinates format: {parts[1]}");
                return;
            }

            // Parse coordinates
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            float z = float.Parse(coords[2]);
            float lookAtX = float.Parse(coords[3]);
            float lookAtY = float.Parse(coords[4]);
            float lookAtZ = float.Parse(coords[5]);

            navigationTarget = new Vector3(x, y, z);
            lookAtTarget = new Vector3(lookAtX, lookAtY, lookAtZ);
            isNavigating = true;

            Debug.Log($"Starting navigation to {navigationTarget}, looking at {lookAtTarget}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing navigation command '{command}': {e.Message}");
        }
    }

    /// <summary>
    /// Processes precise coordinate commands in format: move_to_coordinates:x,y,z
    /// </summary>
    private void ProcessCoordinateCommand(string command)
    {
        try
        {
            // Parse the command: move_to_coordinates:x,y,z
            string[] parts = command.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogError($"Invalid coordinate command format: {command}");
                return;
            }

            string[] coords = parts[1].Split(',');
            if (coords.Length != 3)
            {
                Debug.LogError($"Invalid coordinate format: {parts[1]}");
                return;
            }

            // Parse coordinates
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            float z = float.Parse(coords[2]);

            navigationTarget = new Vector3(x, y, z);
            // Don't set lookAtTarget for coordinate commands - just move to position
            isNavigating = true;

            Debug.Log($"Starting coordinate navigation to {navigationTarget}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing coordinate command '{command}': {e.Message}");
        }
    }

    /// <summary>
    /// Processes rotation commands in format: rotate_to:x,y,z,w
    /// </summary>
    private void ProcessRotationCommand(string command)
    {
        try
        {
            // Parse the command: rotate_to:x,y,z,w
            string[] parts = command.Split(':');
            if (parts.Length != 2)
            {
                Debug.LogError($"Invalid rotation command format: {command}");
                return;
            }

            string[] coords = parts[1].Split(',');
            if (coords.Length != 4)
            {
                Debug.LogError($"Invalid rotation format: {parts[1]}");
                return;
            }

            // Parse rotation quaternion
            float x = float.Parse(coords[0]);
            float y = float.Parse(coords[1]);
            float z = float.Parse(coords[2]);
            float w = float.Parse(coords[3]);

            Quaternion targetRotation = new Quaternion(x, y, z, w);
            
            // Set rotation target (we'll handle this in FixedUpdate)
            rotationTarget = targetRotation;
            isRotating = true;

            Debug.Log($"Starting rotation to {targetRotation.eulerAngles}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing rotation command '{command}': {e.Message}");
        }
    }

    /// <summary>
    /// Handles smooth rotation to target rotation
    /// </summary>
    private void HandleRotation()
    {
        if (!isRotating) return;

        float rotationSpeed = 90f; // degrees per second
        float angleThreshold = 5f; // degrees

        // Calculate the angle between current and target rotation
        float angle = Quaternion.Angle(transform.rotation, rotationTarget);
        
        if (angle <= angleThreshold)
        {
            // Close enough, snap to target and stop rotating
            transform.rotation = rotationTarget;
            isRotating = false;
            Debug.Log("Rotation completed");
        }
        else
        {
            // Rotate towards target
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationTarget, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Handles smooth navigation to target position while looking at the specified point
    /// </summary>
    private void HandleNavigation()
    {
        if (!isNavigating) return;

        Vector3 currentPos = transform.position;
        Vector3 directionToTarget = (navigationTarget - currentPos).normalized;
        float distanceToTarget = Vector3.Distance(currentPos, navigationTarget);

        // Check if we've reached the target
        if (distanceToTarget <= navigationTolerance)
        {
            // We've reached the target, now look at the object
            Vector3 directionToLookAt = (lookAtTarget - currentPos).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToLookAt);
            
            // Smoothly rotate to face the target
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 2f);
            
            // Check if we're looking at the target
            float angleToLookAt = Vector3.Angle(transform.forward, directionToLookAt);
            if (angleToLookAt <= lookAtTolerance)
            {
                // Navigation complete
                isNavigating = false;
                Debug.Log("Navigation completed successfully");
                return;
            }
        }
        else
        {
            // Move towards the target
            Vector3 movement = directionToTarget * navigationSpeed * Time.fixedDeltaTime;
            rb.MovePosition(currentPos + movement);

            // Also rotate towards the target while moving
            Vector3 directionToLookAt = (lookAtTarget - currentPos).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToLookAt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 1f);
        }
    }

    /// <summary>
    /// Public method to start navigation to a specific position
    /// </summary>
    /// <param name="targetPosition">Position to navigate to</param>
    /// <param name="lookAtPosition">Position to look at once arrived</param>
    public void NavigateToPosition(Vector3 targetPosition, Vector3 lookAtPosition)
    {
        navigationTarget = targetPosition;
        lookAtTarget = lookAtPosition;
        isNavigating = true;
        Debug.Log($"Manual navigation started to {targetPosition}, looking at {lookAtPosition}");
    }

    /// <summary>
    /// Handles gaze history requests from the Python backend
    /// </summary>
    private void HandleGazeHistoryRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Get tag parameter from query string
            string tag = request.QueryString["tag"];
            
            if (string.IsNullOrEmpty(tag))
            {
                // Return all tags if no specific tag requested
                var allTags = GazeHistoryManager.Instance?.GetAllTags() ?? new List<string>();
                string responseJson = JsonUtility.ToJson(new { tags = allTags });
                SendJsonResponse(response, responseJson);
                return;
            }

            // Get the last object with the specified tag
            Debug.Log($"🔍 [GAZE] Looking for tag: '{tag}'");
            Debug.Log($"🔍 [GAZE] GazeHistoryManager.Instance: {GazeHistoryManager.Instance != null}");
            var lastObject = GazeHistoryManager.Instance?.GetLastByTag(tag);
            Debug.Log($"🔍 [GAZE] Found object: {lastObject != null}");
            
            if (lastObject != null)
            {
                // Create response with object data
                var responseData = new
                {
                    found = true,
                    name = lastObject.name,
                    tag = lastObject.tag,
                    position = new { x = lastObject.position.x, y = lastObject.position.y, z = lastObject.position.z },
                    timestamp = lastObject.timestamp,
                    distance = lastObject.distance
                };
                
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log($"Returning gaze history for tag '{tag}': {lastObject.name} at {lastObject.position}");
            }
            else
            {
                // No object found with that tag
                var responseData = new { found = false, message = $"No objects found with tag '{tag}'" };
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log($"No objects found with tag '{tag}' in gaze history");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling gaze history request: {e.Message}");
            response.StatusCode = 500;
            var errorData = new { error = e.Message };
            string errorJson = JsonUtility.ToJson(errorData);
            SendJsonResponse(response, errorJson);
        }
    }

    /// <summary>
    /// Handles gaze history requests by object name from the Python backend
    /// </summary>
    private void HandleGazeHistoryByNameRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Get name parameter from query string
            string objectName = request.QueryString["name"];
            
            if (string.IsNullOrEmpty(objectName))
            {
                var responseData = new { found = false, message = "No object name provided" };
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                return;
            }

            // Get the object with the specified name
            Debug.Log($"🔍 [GAZE] Looking for object name: '{objectName}'");
            Debug.Log($"🔍 [GAZE] GazeHistoryManager.Instance: {GazeHistoryManager.Instance != null}");
            var foundObject = GazeHistoryManager.Instance?.GetByName(objectName);
            Debug.Log($"🔍 [GAZE] Found object: {foundObject != null}");
            
            if (foundObject != null)
            {
                // Create response with object data
                var responseData = new
                {
                    found = true,
                    name = foundObject.name,
                    tag = foundObject.tag,
                    position = new { x = foundObject.position.x, y = foundObject.position.y, z = foundObject.position.z },
                    timestamp = foundObject.timestamp,
                    distance = foundObject.distance
                };
                
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log($"Returning gaze history for object '{objectName}': {foundObject.name} at {foundObject.position}");
            }
            else
            {
                var responseData = new { found = false, message = $"No objects found with name '{objectName}'" };
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log($"No objects found with name '{objectName}' in gaze history");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling gaze history by name request: {e.Message}");
            response.StatusCode = 500;
            var errorData = new { error = e.Message };
            string errorJson = JsonUtility.ToJson(errorData);
            SendJsonResponse(response, errorJson);
        }
    }

    /// <summary>
    /// Handles requests for recent gaze history objects
    /// </summary>
    private void HandleGazeHistoryRecentRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Get count parameter from query string (default: 30)
            string countParam = request.QueryString["count"];
            int count = 30; // default
            if (!string.IsNullOrEmpty(countParam) && int.TryParse(countParam, out int parsedCount))
            {
                count = parsedCount;
            }

            // Get the recent objects
            Debug.Log($"🔍 [GAZE] GazeHistoryManager.Instance: {GazeHistoryManager.Instance != null}");
            var recentObjects = GazeHistoryManager.Instance?.GetLastViewedObjects(count);
            Debug.Log($"🔍 [GAZE] Recent objects: {recentObjects?.Count ?? 0}");
            
            if (recentObjects != null && recentObjects.Count > 0)
            {
                // Create response with object data
                var responseData = new
                {
                    found = true,
                    count = recentObjects.Count,
                    objects = recentObjects.Select(obj => new
                    {
                        name = obj.name,
                        tag = obj.tag,
                        position = new { x = obj.position.x, y = obj.position.y, z = obj.position.z },
                        rotation = new { x = obj.rotation.x, y = obj.rotation.y, z = obj.rotation.z, w = obj.rotation.w },
                        timestamp = obj.timestamp,
                        distance = obj.distance
                    }).ToArray()
                };
                
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log($"Returning {recentObjects.Count} recent objects from gaze history");
            }
            else
            {
                var responseData = new { found = false, count = 0, objects = new object[0] };
                string responseJson = JsonUtility.ToJson(responseData);
                SendJsonResponse(response, responseJson);
                Debug.Log("No objects found in gaze history");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling gaze history recent request: {e.Message}");
            response.StatusCode = 500;
            var errorData = new { error = e.Message };
            string errorJson = JsonUtility.ToJson(errorData);
            SendJsonResponse(response, errorJson);
        }
    }

    /// <summary>
    /// Sends a JSON response
    /// </summary>
    private void SendJsonResponse(HttpListenerResponse response, string json)
    {
        response.ContentType = "application/json";
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    // Data class for JSON parsing
    [System.Serializable]
    private class CommandData
    {
        public string command;  // For backward compatibility
        public string[] commands;  // For multiple commands
    }
}
