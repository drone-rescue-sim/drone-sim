using UnityEngine;

/// <summary>
/// Object Labeling Component
/// 
/// This component provides clean, readable names for objects in the gaze tracking system.
/// Instead of showing "Building_01_Office_Tower", it shows "Building: Office Tower".
/// 
/// HOW IT WORKS:
/// - Stores a custom label for the object
/// - Stores a category (Building, Vehicle, Nature, etc.)
/// - Combines them for display: "Category: Label"
/// - Falls back to object name if no custom label is set
/// 
/// This makes the console output much more readable and professional.
/// </summary>
public class HoverLabel : MonoBehaviour
{
    [Header("Label Settings")]
    [Tooltip("Display name for this object. If empty, uses GameObject name.")]
    public string label = "";
    
    [Tooltip("Optional category to prefix the label (e.g., 'House', 'NPC', 'Item').")]
    public string category = "";

    /// <summary>
    /// Gets the effective label for this object.
    /// Returns the set label, or GameObject name if label is empty.
    /// </summary>
    public string GetEffectiveLabel()
    {
        return string.IsNullOrEmpty(label) ? gameObject.name : label;
    }

    /// <summary>
    /// Gets the formatted display text for logging.
    /// Format: "Category: Label" or just "Label" if no category.
    /// </summary>
    public string GetDisplayText()
    {
        string effectiveLabel = GetEffectiveLabel();
        
        if (string.IsNullOrEmpty(category))
            return effectiveLabel;
        else
            return $"{category}: {effectiveLabel}";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-populate label with GameObject name if empty
        if (string.IsNullOrEmpty(label))
        {
            label = gameObject.name;
        }
    }
#endif
}



