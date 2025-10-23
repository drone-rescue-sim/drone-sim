using UnityEngine;

public class EyeTrackingSimulator : MonoBehaviour
{
    public Camera mainCamera;     
    public GameObject gazeMarker; // En liten sphere som viser blikkpunkt

    void Update()
    {
        // DISABLED: Using GazeRayInteractor instead for advanced gaze tracking
        // This simple system is kept for reference but disabled to avoid conflicts
        
        /*
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

            // Clean console logging
            Debug.Log($"👁️ Looking at: {hit.collider.name} | Distance: {hit.distance:F1}m | Position: {hit.point}");

        }
        */
        
        // Still update the visual marker position for visual feedback
        Vector3 mousePos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            gazeMarker.transform.position = hit.point;
        }
    }
}
