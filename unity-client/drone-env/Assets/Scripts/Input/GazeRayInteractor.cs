using UnityEngine;

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
    /// Logs hover information for the given GameObject, with de-duplication.
    /// </summary>
    private void LogHoverTarget(GameObject target, RaycastHit hit)
    {
        if (!logHover || target == _lastHoverTarget)
            return;

        // Check if object is too far away for meaningful hover detection
        if (hit.distance > maxHoverDistance)
        {
            return; // Silently ignore objects that are too far away
        }

        // If precise logging is enabled, only log objects that are not ignored
        if (preciseMouseLogging && ShouldIgnoreObject(target))
        {
            return; // Silently ignore objects that aren't meaningful enough
        }

        _lastHoverTarget = target;
        
        // Get display name - try to use HoverLabel component first, then tag, then name
        string displayText = GetDisplayName(target);

        // Build a clean, concise log message
        string logMessage = $"👁️ Looking at: {displayText}";
        
        // Always include distance for now to debug
        logMessage += $" [{hit.distance:F1}m]";

        // Add mouse position for reference
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
        // 1) Use mouse position as fake gaze input (works on macOS, Windows, Linux)
        Vector2 screen = Input.mousePosition;

        // 2) Smooth the movement
        float k = (smoothTime <= 0f) ? 1f :
            1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, smoothTime));
        _smoothedScreen = Vector2.Lerp(_smoothedScreen, screen, k);

        // 3) Raycast from camera through gaze point
        var cam = Camera.main;
        if (!cam) 
        {
            if (debugMode && Time.frameCount % 300 == 0) // Only warn every 5 seconds instead of every second
                Debug.LogWarning("⚠️ No Camera.main found! Make sure your camera has the 'MainCamera' tag.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(_smoothedScreen);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers.value))
        {
            // Store hit information for gizmos and distance logging
            _lastHit = hit;
            
            if (hit.collider.gameObject != _currentTarget)
            {
                if (_currentTarget)
                    _currentTarget.SendMessage("OnGazeExit", SendMessageOptions.DontRequireReceiver);

                _currentTarget = hit.collider.gameObject;
                _currentTarget.SendMessage("OnGazeEnter", SendMessageOptions.DontRequireReceiver);

                // Log the new hover target (with de-duplication and filtering)
                LogHoverTarget(_currentTarget, hit);

                _dwellTimer = 0f;
                _lastScreen = screen;
            }

            // Manual "click" (Space)
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
