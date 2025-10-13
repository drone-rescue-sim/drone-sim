using UnityEngine;

public class EyeTrackingSimulator : MonoBehaviour
{
    public Camera mainCamera;     
    public GameObject gazeMarker; // En liten sphere som viser blikkpunkt
    
    private GameObject _lastDetectedObject; // Track last detected object to avoid spam logging

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;

        // Tobii: 
        // Vector2 gazePoint = Tobii.Gaming.TobiiAPI.GetGazePoint().Screen; 
        // Ray ray = Camera.main.ScreenPointToRay(gazePoint);

        // Konverter museposisjon til en ray fra kamera
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Hvis ray treffer et objekt i scenen
        if (Physics.Raycast(ray, out hit))
        {
            // Flytt markøren dit
            gazeMarker.transform.position = hit.point;

            // Only log when a new object is detected (not every frame)
            if (hit.collider.gameObject != _lastDetectedObject)
            {
                Debug.Log("Ser på: " + hit.collider.name + " på posisjon: " + hit.collider.transform.position);
                
                // Add to gaze history if available
                if (GazeHistoryManager.Instance != null)
                {
                    GazeHistoryManager.Instance.AddViewedObject(hit.collider.gameObject, hit.collider.transform.position, hit.collider.transform.rotation, hit.distance);
                }
                else
                {
                    // Auto-create GazeHistoryManager if it doesn't exist
                    Debug.LogWarning("GazeHistoryManager.Instance is null - creating one automatically");
                    GameObject gazeHistoryGO = new GameObject("GazeHistoryManager");
                    gazeHistoryGO.AddComponent<GazeHistoryManager>();
                    
                    // Try to add the object again
                    if (GazeHistoryManager.Instance != null)
                    {
                        GazeHistoryManager.Instance.AddViewedObject(hit.collider.gameObject, hit.collider.transform.position, hit.collider.transform.rotation, hit.distance);
                    }
                }
                
                _lastDetectedObject = hit.collider.gameObject;
            }
        }
        else
        {
            // Clear the last detected object when raycast misses
            _lastDetectedObject = null;
        }
    }
}
