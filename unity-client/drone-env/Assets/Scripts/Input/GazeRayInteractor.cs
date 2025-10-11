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
    public float dwellTime = 0.6f;
    public float dwellMoveTolerance = 6f; // pixels

    [Header("Hover Logging")]
    [Tooltip("Enable/disable hover target logging to console.")]
    public bool logHover = true;
    [Tooltip("Include distance in hover logs.")]
    public bool includeDistance = false;
    [Tooltip("Only log when mouse is directly over an object (not just raycast hits).")]
    public bool preciseMouseLogging = true;

    [Header("Debug Visualization")]
    [Tooltip("Show ray and hit point gizmos in Scene view.")]
    public bool drawGizmos = true;
    [Tooltip("Enable detailed debug logging to help troubleshoot issues.")]
    public bool debugMode = true;

    [Header("Filtering")]
    [Tooltip("Objects with these tags will be ignored for hover logging.")]
    public string[] ignoredTags = { "Untagged", "Ground", "Floor", "Terrain" };
    [Tooltip("Maximum distance to consider for hover logging (objects too far won't be logged).")]
    public float maxHoverDistance = 50f;

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
            if (debugMode)
            {
                Debug.Log($"Ignoring '{target.name}' - too far away ({hit.distance:F2}m > {maxHoverDistance}m)");
            }
            return;
        }

        // If precise logging is enabled, only log objects that are not ignored
        if (preciseMouseLogging && ShouldIgnoreObject(target))
        {
            if (debugMode)
            {
                Debug.Log($"Precise logging: Ignoring '{target.name}' (tag: '{target.tag}') - not meaningful enough");
            }
            return;
        }

        _lastHoverTarget = target;
        
        // Use the GameObject's tag for logging
        string displayText = target.tag;

        // Build the log message
        string logMessage = $"Hover: {displayText}";
        
        if (includeDistance)
        {
            logMessage += $" (dist: {hit.distance:F2}m)";
        }

        Debug.Log(logMessage);
        
        if (debugMode)
        {
            Debug.Log($"DEBUG - Object: {target.name}, Tag: {target.tag}, Distance: {hit.distance:F2}m");
        }
    }

    void Update()
    {
        // 1) Use mouse position as fake gaze input (works on macOS, Windows, Linux)
        Vector2 screen = Input.mousePosition;
        
        if (debugMode && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"Mouse Position: {screen}, Screen Size: {Screen.width}x{Screen.height}");
        }

        // 2) Smooth the movement
        float k = (smoothTime <= 0f) ? 1f :
            1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, smoothTime));
        _smoothedScreen = Vector2.Lerp(_smoothedScreen, screen, k);

        // 3) Raycast from camera through gaze point
        var cam = Camera.main;
        if (!cam) 
        {
            if (debugMode && Time.frameCount % 60 == 0)
                Debug.LogWarning("No Camera.main found! Make sure your camera has the 'MainCamera' tag.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(_smoothedScreen);
        
        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Ray: origin={ray.origin}, direction={ray.direction}, maxDistance={maxDistance}");
        }
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers.value))
        {
            // Store hit information for gizmos and distance logging
            _lastHit = hit;
            
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"Raycast HIT: {hit.collider.gameObject.name} (tag: {hit.collider.gameObject.tag}) at distance {hit.distance:F2}m");
            }
            
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
                _currentTarget.SendMessage("OnGazeClick", SendMessageOptions.DontRequireReceiver);

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
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log("Raycast MISS - no objects hit");
            }
            
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
