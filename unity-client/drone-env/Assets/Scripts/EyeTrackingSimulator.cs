using UnityEngine;

public class EyeTrackingSimulator : MonoBehaviour
{
    public Camera mainCamera;     
    public GameObject gazeMarker; // En liten sphere som viser blikkpunkt

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

            // Clean console logging
            Debug.Log($"👁️ Looking at: {hit.collider.name} | Distance: {hit.distance:F1}m | Position: {hit.point}");

        }
    }
}
