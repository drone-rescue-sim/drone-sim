using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages a history of objects that have been viewed by the user.
/// Provides methods to query and retrieve previously viewed objects by tag.
/// </summary>
public class GazeHistoryManager : MonoBehaviour
{
    [Header("History Settings")]
    [Tooltip("Maximum number of objects to keep in history")]
    public int maxHistorySize = 50;
    
    [Tooltip("Minimum time between adding the same object again (seconds)")]
    public float duplicateCooldown = 0.1f; // Reduced to 0.1 seconds to allow frequent updates
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for history operations")]
    public bool debugMode = true;

    // Singleton instance
    public static GazeHistoryManager Instance { get; private set; }

    // Data structure for storing viewed object information
    [System.Serializable]
    public class ViewedObject
    {
        public string name;
        public string tag;
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
        public float distance;

        public ViewedObject(GameObject obj, Vector3 pos, Quaternion rot, float dist)
        {
            name = obj.name;
            tag = obj.tag;
            position = pos;
            rotation = rot;
            timestamp = Time.time;
            distance = dist;
        }
    }

    // History list
    private List<ViewedObject> viewedObjects = new List<ViewedObject>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds a viewed object to the history with deduplication
    /// </summary>
    /// <param name="obj">The GameObject that was viewed</param>
    /// <param name="position">World position where the object was viewed from</param>
    /// <param name="rotation">Rotation of the object</param>
    /// <param name="distance">Distance from viewer to object</param>
    /// <returns>True if object was added, false if it was a duplicate</returns>
    public bool AddViewedObject(GameObject obj, Vector3 position, Quaternion rotation, float distance)
    {
        if (obj == null) return false;

        // Check for recent duplicates (same object within cooldown period)
        float currentTime = Time.time;
        foreach (var existing in viewedObjects)
        {
            if (existing.name == obj.name && existing.tag == obj.tag)
            {
                float timeSinceLastSeen = currentTime - existing.timestamp;
                if (timeSinceLastSeen < duplicateCooldown)
                {
                    if (debugMode)
                    {
                        Debug.Log($"Skipping duplicate: {obj.name} (tag: {obj.tag}) - seen {timeSinceLastSeen:F1}s ago");
                    }
                    return false; // Don't add duplicate
                }
            }
        }

        // Create new viewed object entry
        ViewedObject viewedObj = new ViewedObject(obj, position, rotation, distance);

        // Add to history
        viewedObjects.Add(viewedObj);

        // Limit history size
        if (viewedObjects.Count > maxHistorySize)
        {
            viewedObjects.RemoveAt(0);
        }

        if (debugMode)
        {
            Debug.Log($"Added to gaze history: {obj.name} (tag: {obj.tag}) at {position} (dist: {distance:F2}m)");
            Debug.Log($"Gaze history now contains {viewedObjects.Count} objects");
        }
        
        return true; // Object was added
    }

    /// <summary>
    /// Gets the most recently viewed object with the specified tag
    /// </summary>
    /// <param name="tag">Unity tag to search for</param>
    /// <returns>Most recent ViewedObject with the tag, or null if none found</returns>
    public ViewedObject GetLastByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;

        // Find all objects with the specified tag, ordered by timestamp (newest first)
        var matches = viewedObjects
            .Where(obj => string.Equals(obj.tag, tag, System.StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(obj => obj.timestamp)
            .ToList();

        if (matches.Count > 0)
        {
            if (debugMode)
            {
                Debug.Log($"Found {matches.Count} objects with tag '{tag}', returning most recent: {matches[0].name}");
            }
            return matches[0];
        }

        if (debugMode)
        {
            Debug.Log($"No objects found with tag '{tag}' in gaze history");
        }
        return null;
    }

    /// <summary>
    /// Gets the most recently viewed object with the specified name
    /// </summary>
    /// <param name="name">Object name to search for</param>
    /// <returns>Most recent ViewedObject with the name, or null if none found</returns>
    public ViewedObject GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // Find all objects with the specified name, ordered by timestamp (newest first)
        var matches = viewedObjects
            .Where(obj => string.Equals(obj.name, name, System.StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(obj => obj.timestamp)
            .ToList();

        if (matches.Count > 0)
        {
            if (debugMode)
            {
                Debug.Log($"Found {matches.Count} objects with name '{name}', returning most recent: {matches[0].name}");
            }
            return matches[0];
        }

        if (debugMode)
        {
            Debug.Log($"No objects found with name '{name}' in gaze history");
        }
        return null;
    }

    /// <summary>
    /// Gets all viewed objects with the specified tag, ordered by most recent first
    /// </summary>
    /// <param name="tag">Unity tag to search for</param>
    /// <returns>List of ViewedObjects with the tag, ordered by timestamp (newest first)</returns>
    public List<ViewedObject> GetAllByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return new List<ViewedObject>();

        var matches = viewedObjects
            .Where(obj => string.Equals(obj.tag, tag, System.StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(obj => obj.timestamp)
            .ToList();

        if (debugMode)
        {
            Debug.Log($"Found {matches.Count} objects with tag '{tag}'");
        }

        return matches;
    }

    /// <summary>
    /// Gets all unique tags that have been viewed
    /// </summary>
    /// <returns>List of unique tags</returns>
    public List<string> GetAllTags()
    {
        var tags = viewedObjects
            .Select(obj => obj.tag)
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();

        if (debugMode)
        {
            Debug.Log($"Found {tags.Count} unique tags in history: {string.Join(", ", tags)}");
        }

        return tags;
    }

    /// <summary>
    /// Clears the entire gaze history
    /// </summary>
    public void Clear()
    {
        int count = viewedObjects.Count;
        viewedObjects.Clear();
        
        if (debugMode)
        {
            Debug.Log($"Cleared gaze history ({count} objects removed)");
        }
    }

    /// <summary>
    /// Gets the total number of objects in history
    /// </summary>
    /// <returns>Number of objects in history</returns>
    public int GetHistoryCount()
    {
        return viewedObjects.Count;
    }

    /// <summary>
    /// Gets the most recently viewed object regardless of tag
    /// </summary>
    /// <returns>Most recent ViewedObject, or null if history is empty</returns>
    public ViewedObject GetLastViewed()
    {
        if (viewedObjects.Count == 0) return null;

        var last = viewedObjects.OrderByDescending(obj => obj.timestamp).First();
        
        if (debugMode)
        {
            Debug.Log($"Last viewed object: {last.name} (tag: {last.tag})");
        }
        
        return last;
    }

    /// <summary>
    /// Gets the last N viewed objects, ordered by most recent first
    /// </summary>
    /// <param name="count">Number of objects to return (default: 30)</param>
    /// <returns>List of most recent ViewedObjects</returns>
    public List<ViewedObject> GetLastViewedObjects(int count = 30)
    {
        var recentObjects = viewedObjects
            .OrderByDescending(obj => obj.timestamp)
            .Take(count)
            .ToList();

        if (debugMode)
        {
            Debug.Log($"Returning {recentObjects.Count} most recent objects from gaze history");
        }

        return recentObjects;
    }

    /// <summary>
    /// Prints the current list of tracked objects to console
    /// </summary>
    public void PrintTrackedObjects()
    {
        if (viewedObjects.Count == 0)
        {
            Debug.Log("📋 Tracked Objects: (empty)");
            return;
        }

        // Group by tag and show count
        var groupedByTag = viewedObjects
            .GroupBy(obj => obj.tag)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key);

        Debug.Log($"📋 Tracked Objects ({viewedObjects.Count} total):");
        
        foreach (var group in groupedByTag)
        {
            var latest = group.OrderByDescending(obj => obj.timestamp).First();
            Debug.Log($"   • {group.Key}: {group.Count()} object(s) - Latest: {latest.name}");
        }
    }
}
