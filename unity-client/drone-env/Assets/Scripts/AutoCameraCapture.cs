using UnityEngine;
using System.Collections;

public class AutoCameraCapture : MonoBehaviour
{
    [Header("Target Objects")]
    public Transform[] targetsToCapture;
    
    [Header("Capture Settings")]
    public float captureInterval = 2f;
    public int capturesPerTarget = 10;
    public float orbitRadius = 15f;
    public float minHeight = 2f;
    public float maxHeight = 8f;
    
    [Header("Reference")]
    public TrainingDataCollector dataCollector;
    
    private bool isCapturing = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        if (dataCollector == null)
            dataCollector = FindObjectOfType<TrainingDataCollector>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && !isCapturing)
        {
            StartCoroutine(AutoCaptureSequence());
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StopAllCoroutines();
            isCapturing = false;
            ResetCamera();
            Debug.Log("Auto capture stopped");
        }
    }
    
    IEnumerator AutoCaptureSequence()
    {
        isCapturing = true;
        Debug.Log("Starting automatic capture sequence...");
        
        if (targetsToCapture.Length == 0)
        {
            Debug.LogError("No targets set! Add person/fire objects to 'Targets To Capture'");
            isCapturing = false;
            yield break;
        }
        
        int totalCaptures = 0;
        
        foreach (Transform target in targetsToCapture)
        {
            if (target == null) continue;
            
            Debug.Log($"Capturing target: {target.name}");
            
            for (int i = 0; i < capturesPerTarget; i++)
            {
                // Random position around target
                float angle = Random.Range(0f, 360f);
                float height = Random.Range(minHeight, maxHeight);
                float radius = Random.Range(orbitRadius * 0.7f, orbitRadius * 1.3f);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    height,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                
                transform.position = target.position + offset;
                transform.LookAt(target.position + Vector3.up * 1.5f);
                
                // Small random rotation offset
                transform.Rotate(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 0);
                
                yield return new WaitForEndOfFrame();
                
                // Trigger capture
                if (dataCollector != null)
                {
                    dataCollector.SendMessage("CaptureTrainingImage");
                }
                
                totalCaptures++;
                
                yield return new WaitForSeconds(captureInterval);
            }
        }
        
        Debug.Log($"Auto capture complete! Captured {totalCaptures} images");
        ResetCamera();
        isCapturing = false;
    }
    
    void ResetCamera()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        Debug.Log("Camera reset to original position");
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 180, 400, 60),
            "Auto Capture:\n" +
            "F1 = Start Auto Capture\n" +
            "F2 = Stop and Reset Camera");
        
        if (isCapturing)
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 250, 300, 20), "AUTO CAPTURE IN PROGRESS...");
            GUI.color = Color.white;
        }
    }
}