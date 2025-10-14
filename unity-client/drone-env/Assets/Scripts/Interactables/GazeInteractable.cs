using UnityEngine;

public class GazeInteractable : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Renderer targetRenderer; // Optional override
    public Color highlightColor = new Color(1f, 1f, 0.4f, 1f);

    private Color _originalColor;
    private bool _hasColorProperty;

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer && targetRenderer.material && targetRenderer.material.HasProperty("_Color"))
        {
            _originalColor = targetRenderer.material.color;
            _hasColorProperty = true;
        }
    }

    // Called by the GazeRayInteractor
    void OnGazeEnter()
    {
        if (_hasColorProperty) targetRenderer.material.color = highlightColor;
    }

    void OnGazeExit()
    {
        if (_hasColorProperty) targetRenderer.material.color = _originalColor;
    }

    void OnGazeClick()
    {
        var hoverLabel = GetComponent<HoverLabel>();
        string displayName = hoverLabel != null ? hoverLabel.GetDisplayText() : name;
        
        Debug.Log($"🎯 Clicked: {displayName}");
        
        // Add visual feedback (scale animation)
        StartCoroutine(ClickAnimation());
        
        // Add some fun interaction effects
        AddClickEffects();
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
        targetRenderer.material.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        targetRenderer.material.color = highlightColor;
        yield return new WaitForSeconds(0.1f);
        targetRenderer.material.color = _originalColor;
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
