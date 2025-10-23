using UnityEngine;

/// <summary>
/// Automatically adds GazeInteractable and HoverLabel components to objects in the scene
/// to make them interactive with the gaze tracking system.
/// </summary>
public class InteractiveObjectSetup : MonoBehaviour
{
    [Header("Auto-Setup Settings")]
    [Tooltip("Automatically setup objects when this script starts")]
    public bool autoSetupOnStart = true;
    
    [Tooltip("Objects with these name patterns will be made interactive")]
    public string[] interactiveNamePatterns = {
        "building", "house", "office", "shop", "store", "bank", "hospital", "school",
        "bench", "chair", "seat",
        "car", "truck", "bus", "vehicle", "police", "ambulance",
        "tree", "bush", "plant", "flower",
        "lamp", "light", "post", "pole",
        "sign", "traffic", "stop", "parking",
        "trash", "bin", "container",
        "door", "gate", "entrance"
    };
    
    [Tooltip("Objects with these tags will be made interactive")]
    public string[] interactiveTags = {
        "Building", "Vehicle", "Prop", "Interactive", "Furniture"
    };
    
    [Header("Highlight Settings")]
    [Tooltip("Color to highlight objects when gazed at")]
    public Color highlightColor = new Color(1f, 1f, 0.4f, 1f);
    
    [Tooltip("Objects that should be ignored (won't be made interactive)")]
    public string[] ignoreNamePatterns = {
        "ground", "floor", "terrain", "sky", "camera", "light", "particle", "effect"
    };

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupInteractiveObjects();
        }
    }
    
    /// <summary>
    /// Manually trigger the setup process
    /// </summary>
    [ContextMenu("Setup Interactive Objects")]
    public void SetupInteractiveObjects()
    {
        Debug.Log("🔧 Setting up interactive objects...");
        
        int objectsSetup = 0;
        var allObjects = FindObjectsOfType<GameObject>();
        
        foreach (var obj in allObjects)
        {
            // Skip if already has components
            if (obj.GetComponent<GazeInteractable>() != null || obj.GetComponent<HoverLabel>() != null)
                continue;
                
            // Skip if should be ignored
            if (ShouldIgnoreObject(obj))
                continue;
                
            // Check if object should be interactive
            if (ShouldMakeInteractive(obj))
            {
                SetupObject(obj);
                objectsSetup++;
            }
        }
        
        Debug.Log($"✅ Interactive setup complete! Made {objectsSetup} objects interactive.");
    }
    
    private bool ShouldIgnoreObject(GameObject obj)
    {
        string objName = obj.name.ToLower();
        
        foreach (string pattern in ignoreNamePatterns)
        {
            if (objName.Contains(pattern.ToLower()))
                return true;
        }
        
        return false;
    }
    
    private bool ShouldMakeInteractive(GameObject obj)
    {
        string objName = obj.name.ToLower();
        string objTag = obj.tag.ToLower();
        
        // Check name patterns
        foreach (string pattern in interactiveNamePatterns)
        {
            if (objName.Contains(pattern.ToLower()))
                return true;
        }
        
        // Check tags
        foreach (string tag in interactiveTags)
        {
            if (objTag == tag.ToLower())
                return true;
        }
        
        // Check if object has a renderer (visible objects)
        if (obj.GetComponent<Renderer>() != null || obj.GetComponentInChildren<Renderer>() != null)
        {
            // Make sure it's not too small or too large
            var renderer = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                
                // Only include objects that are reasonably sized (not tiny particles or huge terrain)
                if (size > 0.1f && size < 100f)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void SetupObject(GameObject obj)
    {
        // Add GazeInteractable component
        var gazeInteractable = obj.AddComponent<GazeInteractable>();
        gazeInteractable.highlightColor = highlightColor;
        
        // Add HoverLabel component with smart naming
        var hoverLabel = obj.AddComponent<HoverLabel>();
        
        // Generate smart label based on object name
        string smartLabel = GenerateSmartLabel(obj);
        hoverLabel.label = smartLabel;
        
        // Set category based on object type
        string category = GenerateCategory(obj);
        hoverLabel.category = category;
        
        Debug.Log($"🎯 Made interactive: {category}: {smartLabel}");
    }
    
    private string GenerateSmartLabel(GameObject obj)
    {
        string name = obj.name;
        
        // Remove common prefixes/suffixes and clean up the name
        name = name.Replace("_", " ");
        name = name.Replace("-", " ");
        
        // Remove numbers at the end
        while (char.IsDigit(name[name.Length - 1]) || name[name.Length - 1] == ' ')
        {
            name = name.Substring(0, name.Length - 1);
        }
        
        // Capitalize first letter of each word
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
    
    private string GenerateCategory(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        if (name.Contains("building") || name.Contains("house") || name.Contains("office") || 
            name.Contains("shop") || name.Contains("store") || name.Contains("bank"))
            return "Building";
            
        if (name.Contains("car") || name.Contains("truck") || name.Contains("bus") || 
            name.Contains("vehicle") || name.Contains("police"))
            return "Vehicle";
            
        if (name.Contains("tree") || name.Contains("bush") || name.Contains("plant") || 
            name.Contains("flower"))
            return "Nature";
            
        if (name.Contains("bench") || name.Contains("chair") || name.Contains("seat"))
            return "Furniture";
            
        if (name.Contains("sign") || name.Contains("traffic") || name.Contains("stop"))
            return "Sign";
            
        if (name.Contains("lamp") || name.Contains("light") || name.Contains("post"))
            return "Street";
            
        return "Object"; // Default category
    }
}
