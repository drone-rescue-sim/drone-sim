using UnityEngine;

/// <summary>
/// Interactive Object Component
/// 
/// This component makes objects respond to gaze tracking:
/// - Highlights when looked at (yellow glow)
/// - Reacts when clicked (scale, flash, wiggle animations)
/// - Provides visual feedback for user interactions
/// 
/// HOW IT WORKS:
/// 1. GazeRayInteractor sends messages to this component
/// 2. OnGazeEnter() → Object highlights yellow
/// 3. OnGazeExit() → Object returns to normal color
/// 4. OnGazeClick() → Object animates (scale, flash, wiggle)
/// 
/// This creates the interactive experience where objects "respond" to being looked at.
/// </summary>
public class GazeInteractable : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Renderer targetRenderer; // Optional override
    public Color highlightColor = new Color(1f, 1f, 0.4f, 1f);

    private Color _originalColor;
    private bool _hasColorProperty;
    private string _colorPropertyName; // Supports both _Color and _BaseColor (URP/HDRP)

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer && targetRenderer.material)
        {
            var mat = targetRenderer.material;
            if (mat.HasProperty("_Color"))
            {
                _colorPropertyName = "_Color";
                _originalColor = mat.GetColor(_colorPropertyName);
                _hasColorProperty = true;
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                _colorPropertyName = "_BaseColor";
                _originalColor = mat.GetColor(_colorPropertyName);
                _hasColorProperty = true;
            }
        }
    }

    // Called when user starts looking at this object
    void OnGazeEnter()
    {
        // Highlight the object in yellow to show it's being looked at
        if (_hasColorProperty) targetRenderer.material.SetColor(_colorPropertyName, highlightColor);
    }

    // Called when user stops looking at this object
    void OnGazeExit()
    {
        // Return object to its original color
        if (_hasColorProperty) targetRenderer.material.SetColor(_colorPropertyName, _originalColor);
    }

    // Called when user "clicks" on this object (presses Spacebar while looking at it)
    void OnGazeClick()
    {
        var hoverLabel = GetComponent<HoverLabel>();
        string displayName = hoverLabel != null ? hoverLabel.GetDisplayText() : name;
        
        Debug.Log($"🎯 Clicked: {displayName}");
        
        // Start visual feedback animations
        StartCoroutine(ClickAnimation());  // Scale up/down animation
        AddClickEffects();                 // Color flash + rotation wiggle
    }
    
    private void AddClickEffects()
    {
        // Make the object briefly flash a different color
        if (_hasColorProperty)
        {
            StartCoroutine(ColorFlash());
        }
        
        // Add a subtle rotation
        StartCoroutine(RotationWiggle());
    }
    
    private System.Collections.IEnumerator ColorFlash()
    {
        Color flashColor = Color.white;
        if (_hasColorProperty) targetRenderer.material.SetColor(_colorPropertyName, flashColor);
        yield return new WaitForSeconds(0.1f);
        if (_hasColorProperty) targetRenderer.material.SetColor(_colorPropertyName, highlightColor);
        yield return new WaitForSeconds(0.1f);
        if (_hasColorProperty) targetRenderer.material.SetColor(_colorPropertyName, _originalColor);
    }
    
    private System.Collections.IEnumerator RotationWiggle()
    {
        Vector3 originalRotation = transform.eulerAngles;
        float wiggleAmount = 5f;
        
        // Wiggle left
        transform.eulerAngles = originalRotation + new Vector3(0, -wiggleAmount, 0);
        yield return new WaitForSeconds(0.05f);
        
        // Wiggle right
        transform.eulerAngles = originalRotation + new Vector3(0, wiggleAmount, 0);
        yield return new WaitForSeconds(0.05f);
        
        // Wiggle left again
        transform.eulerAngles = originalRotation + new Vector3(0, -wiggleAmount/2, 0);
        yield return new WaitForSeconds(0.05f);
        
        // Return to original
        transform.eulerAngles = originalRotation;
    }
    
    private System.Collections.IEnumerator ClickAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f; // Make it more noticeable
        
        // Scale up
        float duration = 0.15f; // Slightly longer for better visibility
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}
