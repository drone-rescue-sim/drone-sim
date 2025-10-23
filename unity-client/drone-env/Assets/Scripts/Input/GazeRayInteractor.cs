using UnityEngine;

/// <summary>
/// Main Gaze Tracking System
/// 
/// This is the core component that simulates eye tracking using mouse input.
/// It converts mouse position to a 3D ray and detects what objects the user is "looking at".
/// 
/// SYSTEM FLOW:
/// 1. Mouse position → 2D screen coordinates
/// 2. Screen coordinates → 3D ray from camera
/// 3. Raycast → Detect hit objects in 3D world
/// 4. Log what user is looking at + distance
/// 5. Handle interactions (clicking, highlighting)
/// 
/// KEY FEATURES:
/// - Mouse smoothing for natural movement
/// - Object filtering (ignores ground/terrain)
/// - Console logging with distance and mouse position
/// - Manual clicking with Spacebar
/// - Visual highlighting of gazed objects
/// </summary>
public class GazeRayInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public LayerMask interactableLayers = ~0; // Default: Everything
    public float maxDistance = 25f;

    [Header("Smoothing (seconds)")]
    [Tooltip("0 = no smoothing. Typical 0.10–0.20 for a smoother pointer.")]
    public float smoothTime = 0.12f;

    [Header("Dwell Click (seconds)")]
    [Tooltip("0 = disabled. Hold gaze to trigger click.")]
    public float dwellTime = 20f; // Very long dwell time - effectively disabled
    public float dwellMoveTolerance = 6f; // pixels

    [Header("Hover Logging")]
    [Tooltip("Enable/disable hover target logging to console.")]
    public bool logHover = true;
    [Tooltip("Include distance in hover logs.")]
    public bool includeDistance = true;
    [Tooltip("Only log when mouse is directly over an object (not just raycast hits).")]
    public bool preciseMouseLogging = false;

    [Header("Debug Visualization")]
    [Tooltip("Show ray and hit point gizmos in Scene view.")]
    public bool drawGizmos = true;
    [Tooltip("Enable detailed debug logging to help troubleshoot issues.")]
    public bool debugMode = true;

    [Header("Filtering")]
    [Tooltip("Objects with these tags will be ignored for hover logging.")]
    public string[] ignoredTags = { "Ground", "Floor", "Terrain" };
    [Tooltip("Maximum distance to consider for hover logging (objects too far won't be logged).")]
    public float maxHoverDistance = 100f;

    private GameObject _currentTarget;
    private GameObject _lastHoverTarget; // For de-duplication
    private Vector2 _smoothedScreen;
    private float _dwellTimer;
    private Vector2 _lastScreen;
    private RaycastHit _lastHit; // Store last hit for gizmos and distance

    /// <summary>
    /// Resolve the GameObject that should receive gaze events by preferring
    /// a parent which contains a GazeInteractable component. This ensures
    /// clicks/highlights affect the logical object, not just a mesh child.
    /// </summary>
    private GameObject ResolveInteractionTarget(GameObject raw)
    {
        if (raw == null) return null;
        var interactable = raw.GetComponentInParent<GazeInteractable>();
        return interactable != null ? interactable.gameObject : raw;
    }

    /// <summary>
    /// Checks if the given GameObject should be ignored for hover logging.
    /// </summary>
    private bool ShouldIgnoreObject(GameObject target)
    {
        if (target == null) return true;
        
        string objectTag = target.tag;
        
        // Check if object tag is in the ignored list (case-insensitive)
        for (int i = 0; i < ignoredTags.Length; i++)
        {
            if (string.Equals(objectTag, ignoredTags[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Console Logging Function
    /// 
    /// This function creates the clean console output that shows:
    /// - What object the user is looking at
    /// - Distance to the object
    /// - Mouse position coordinates
    /// 
    /// Example output: "👁️ Looking at: Building: Office Tower [25.7m] | Mouse: (640, 200)"
    /// </summary>
    private void LogHoverTarget(GameObject target, RaycastHit hit)
    {
        // Skip if logging is disabled or we already logged this object
        if (!logHover || target == _lastHoverTarget)
            return;

        // Filter out objects that are too far away (not meaningful for gaze tracking)
        if (hit.distance > maxHoverDistance)
        {
            return; // Silently ignore objects that are too far away
        }

        // Filter out uninteresting objects (ground, terrain, etc.)
        if (preciseMouseLogging && ShouldIgnoreObject(target))
        {
            return; // Silently ignore objects that aren't meaningful enough
        }

        _lastHoverTarget = target;
        
        // Get a nice display name for the object
        // Priority: HoverLabel component > Unity tag > object name
        string displayText = GetDisplayName(target);

        // Build the console message with all relevant information
        string logMessage = $"👁️ Looking at: {displayText}";
        
        // Include distance to show how far away the object is
        logMessage += $" [{hit.distance:F1}m]";

        // Include mouse position for debugging and analysis
        Vector2 mousePos = Input.mousePosition;
        logMessage += $" | Mouse: ({mousePos.x:F0}, {mousePos.y:F0})";

        Debug.Log(logMessage);
    }

    /// <summary>
    /// Gets a clean display name for the target object.
    /// Priority: HoverLabel display text > tag > object name
    /// </summary>
    private string GetDisplayName(GameObject target)
    {
        // First, try to get display text from HoverLabel component
        HoverLabel hoverLabel = target.GetComponent<HoverLabel>();
        if (hoverLabel != null)
        {
            return hoverLabel.GetDisplayText();
        }

        // Fall back to tag if it's meaningful
        if (!string.IsNullOrEmpty(target.tag) && target.tag != "Untagged")
        {
            return target.tag;
        }

        // Finally, use the object name
        return target.name;
    }

    void Update()
    {
        // 1: Get mouse position as "gaze point"
        // This simulates where the user is looking with their eyes
        Vector2 screen = Input.mousePosition;

        // 2: Smooth mouse movement for natural eye tracking feel
        // Real eye movement isn't jittery like mouse movement, so we smooth it
        float k = (smoothTime <= 0f) ? 1f :
            1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, smoothTime));
        _smoothedScreen = Vector2.Lerp(_smoothedScreen, screen, k);

        // 3: Convert 2D screen position to 3D ray from camera
        // This creates a "line of sight" from the camera through the mouse position
        var cam = Camera.main;
        if (!cam) 
        {
            if (debugMode && Time.frameCount % 300 == 0)
                Debug.LogWarning("⚠️ No Camera.main found! Make sure your camera has the 'MainCamera' tag.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(_smoothedScreen);
        
        // 4: Cast ray into 3D world to see what we're "looking at"
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers.value))
        {
            // Store hit information for visual feedback and logging
            _lastHit = hit;
            
            // 5: Handle gaze enter/exit events
            // When we start looking at a new object, trigger events
            var hitObject = hit.collider.gameObject;
            var resolvedTarget = ResolveInteractionTarget(hitObject);
            if (resolvedTarget != _currentTarget)
            {
                // Tell previous object we're no longer looking at it
                if (_currentTarget)
                    _currentTarget.SendMessage("OnGazeExit", SendMessageOptions.DontRequireReceiver);

                // Set new target and tell it we're now looking at it
                _currentTarget = resolvedTarget;
                _currentTarget.SendMessage("OnGazeEnter", SendMessageOptions.DontRequireReceiver);

                // 6: Log what we're looking at with distance and mouse position
                LogHoverTarget(_currentTarget, hit);

                _dwellTimer = 0f;
                _lastScreen = screen;
            }

            // 7: Handle manual clicking with Spacebar
            // This simulates "clicking" on what you're looking at
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"🔍 Spacebar pressed! Current target: {(_currentTarget != null ? _currentTarget.name : "null")}");
                if (_currentTarget != null)
                {
                    _currentTarget.SendMessage("OnGazeClick", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    Debug.Log("⚠️ No current target to click!");
                }
            }

            // 7b: Handle real mouse left-click to trigger interaction
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"🖱️ Mouse click! Current target: {(_currentTarget != null ? _currentTarget.name : "null")}");
                if (_currentTarget != null)
                {
                    _currentTarget.SendMessage("OnGazeClick", SendMessageOptions.DontRequireReceiver);
                }
            }

            // Dwell click (optional)
            if (dwellTime > 0f)
            {
                if ((screen - _lastScreen).sqrMagnitude < dwellMoveTolerance * dwellMoveTolerance)
                {
                    _dwellTimer += Time.deltaTime;
                    if (_dwellTimer >= dwellTime)
                    {
                        _currentTarget.SendMessage("OnGazeClick", SendMessageOptions.DontRequireReceiver);
                        _dwellTimer = 0f;
                    }
                }
                else
                {
                    _dwellTimer = 0f;
                    _lastScreen = screen;
                }
            }
        }
        else
        {
            // No objects hit - clear current target
            if (_currentTarget)
            {
                _currentTarget.SendMessage("OnGazeExit", SendMessageOptions.DontRequireReceiver);
                _currentTarget = null;
                _lastHoverTarget = null; // Clear hover target for logging
                _dwellTimer = 0f;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        var cam = Camera.main;
        if (!cam) return;
        
        Ray ray = cam.ScreenPointToRay(_smoothedScreen);
        Gizmos.color = Color.cyan;
        
        // Draw ray
        Gizmos.DrawRay(ray.origin, ray.direction * Mathf.Min(maxDistance, 5f));
        
        // Draw hit point sphere if we have a valid hit
        if (_currentTarget != null && _lastHit.collider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_lastHit.point, 0.05f); // Small wire sphere at hit point
        }
    }
#endif
}
