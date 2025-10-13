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
        Debug.Log($"{name} OnGazeClick()");
        // Add your own logic here (open door, pick up item, trigger animation, etc.)
    }
}
