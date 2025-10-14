using UnityEngine;

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



