// using UnityEngine;
// using Tobii.Gaming;

// public class EyeTracking : MonoBehaviour
// {
//     void Update()
//     {
//         // Hent punktet brukeren ser på (på skjermen)
//         GazePoint gazePoint = TobiiAPI.GetGazePoint();
        
//         if (gazePoint.IsValid)
//         {
//             Vector2 screenPos = gazePoint.Screen; 
//             Debug.Log("Blikk på skjermposisjon: " + screenPos);
//         }

//         // Hent hvilket gameobject brukeren ser på (raycast)
//         GazeRay gazeRay = TobiiAPI.GetGazeRay();
//         RaycastHit hit;
//         if (Physics.Raycast(gazeRay.Origin, gazeRay.Direction, out hit, Mathf.Infinity))
//         {
//             Debug.Log("Bruker ser på: " + hit.collider.gameObject.name);
//         }
//     }
// }
