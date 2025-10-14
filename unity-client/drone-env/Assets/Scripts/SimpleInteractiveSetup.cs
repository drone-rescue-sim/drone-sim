using UnityEngine;

/// <summary>
/// Automatic Object Setup Script
/// 
/// This script automatically finds objects in the scene and makes them interactive:
/// - Adds GazeInteractable component (for highlighting and animations)
/// - Adds HoverLabel component (for better display names)
/// - Categorizes objects (Building, Vehicle, Nature, etc.)
/// 
/// HOW IT WORKS:
/// 1. Scans all GameObjects in the scene
/// 2. Finds objects with Renderer components (visible objects)
/// 3. Filters out cameras, lights, ground objects
/// 4. Adds interactive components to remaining objects
/// 5. Gives objects clean names and categories
/// 
/// This saves manual work and makes the entire scene interactive automatically.
/// </summary>
public class SimpleInteractiveSetup : MonoBehaviour
{
    [Header("Setup Settings")]
    public bool autoSetupOnStart = true;
    public Color highlightColor = new Color(1f, 1f, 0.4f, 1f);

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupObjects();
        }
    }
    
    [ContextMenu("Setup Objects Now")]
    public void SetupObjects()
    {
        Debug.Log("🔧 Setting up interactive objects...");
        
        int count = 0;
        var allObjects = FindObjectsOfType<GameObject>();
        
        // DEMO: Loop through all objects in the scene
        foreach (var obj in allObjects)
        {
            // DEMO: Skip objects that are already set up or not suitable
            if (obj.GetComponent<GazeInteractable>() != null || 
                obj.GetComponent<HoverLabel>() != null ||
                obj.name.ToLower().Contains("camera") ||
                obj.name.ToLower().Contains("light") ||
                obj.name.ToLower().Contains("ground"))
                continue;
                
            // DEMO: Only make objects with renderers interactive (visible objects)
            if (obj.GetComponent<Renderer>() != null || obj.GetComponentInChildren<Renderer>() != null)
            {
                // DEMO: Add GazeInteractable component for highlighting and animations
                var gazeComp = obj.AddComponent<GazeInteractable>();
                gazeComp.highlightColor = highlightColor;
                
                // DEMO: Add HoverLabel component for better display names
                var labelComp = obj.AddComponent<HoverLabel>();
                labelComp.label = CleanObjectName(obj.name);
                labelComp.category = GetCategory(obj.name);
                
                count++;
                Debug.Log($"🎯 Made interactive: {labelComp.category}: {labelComp.label}");
            }
        }
        
        Debug.Log($"✅ Setup complete! Made {count} objects interactive.");
    }
    
    private string CleanObjectName(string name)
    {
        // Remove underscores, numbers, and clean up
        name = name.Replace("_", " ");
        name = name.Replace("-", " ");
        
        // Remove trailing numbers
        while (name.Length > 0 && (char.IsDigit(name[name.Length - 1]) || name[name.Length - 1] == ' '))
        {
            name = name.Substring(0, name.Length - 1);
        }
        
        // Capitalize words
        string[] words = name.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        
        return string.Join(" ", words);
    }
    
    private string GetCategory(string name)
    {
        name = name.ToLower();
        
        if (name.Contains("building") || name.Contains("house") || name.Contains("office"))
            return "Building";
        if (name.Contains("car") || name.Contains("truck") || name.Contains("vehicle"))
            return "Vehicle";
        if (name.Contains("tree") || name.Contains("bush") || name.Contains("plant"))
            return "Nature";
        if (name.Contains("bench") || name.Contains("chair"))
            return "Furniture";
        if (name.Contains("sign") || name.Contains("traffic"))
            return "Sign";
            
        return "Object";
    }
}
