using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TrainingDataCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    public Camera captureCamera;
    public int imageWidth = 640;
    public int imageHeight = 640;
    public string outputFolder = "TrainingData";
    
    [Header("Objects to Detect - PERSON ONLY")]
    public GameObject[] personObjects;
    
    [Header("Capture Control")]
    public KeyCode captureKey = KeyCode.Return;
    public float autoInterval = 0f;
    
    private int imageCount = 0;
    private float lastCaptureTime = 0f;
    private string imagesPath;
    private string labelsPath;
    
    void Start()
    {
        if (captureCamera == null)
            captureCamera = Camera.main;
        
        imagesPath = Path.Combine(Application.dataPath, "..", outputFolder, "images");
        labelsPath = Path.Combine(Application.dataPath, "..", outputFolder, "labels");
        
        Directory.CreateDirectory(imagesPath);
        Directory.CreateDirectory(labelsPath);
        
        Debug.Log($"Training data will be saved to: {outputFolder}");
        Debug.Log($"Press {captureKey} to capture image + annotations");
        Debug.Log($"TRAINING MODE: PERSON ONLY");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            CaptureTrainingImage();
        }
        
        if (autoInterval > 0 && Time.time - lastCaptureTime > autoInterval)
        {
            CaptureTrainingImage();
            lastCaptureTime = Time.time;
        }
    }
    
    void CaptureTrainingImage()
    {
        Texture2D screenshot = CaptureScreenshot();
        if (screenshot == null)
        {
            Debug.LogError("Failed to capture screenshot");
            return;
        }
        
        string imageName = $"img_{imageCount:D5}";
        string imagePath = Path.Combine(imagesPath, imageName + ".jpg");
        byte[] imageData = screenshot.EncodeToJPG(90);
        File.WriteAllBytes(imagePath, imageData);
        
        List<YOLOAnnotation> annotations = GenerateAnnotations();
        SaveAnnotations(imageName, annotations);
        
        DestroyImmediate(screenshot);
        
        imageCount++;
        Debug.Log($"Captured training image {imageCount}: {annotations.Count} people annotated");
    }
    
    Texture2D CaptureScreenshot()
    {
        RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();
        
        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenshot.Apply();
        
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
        
        return screenshot;
    }
    
    List<YOLOAnnotation> GenerateAnnotations()
    {
        List<YOLOAnnotation> annotations = new List<YOLOAnnotation>();
        
        // ONLY annotate person objects - class 0
        foreach (var personObj in personObjects)
        {
            if (personObj != null && personObj.activeInHierarchy)
            {
                YOLOAnnotation annotation = CreateAnnotation(personObj, 0);
                if (annotation != null)
                    annotations.Add(annotation);
            }
        }
        
        return annotations;
    }
    
    YOLOAnnotation CreateAnnotation(GameObject obj, int classId)
{
    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
    if (renderers.Length == 0) return null;

    // Slå sammen alle renderers' bounds i world space
    Bounds bounds = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

    // Hent 8 hjørner i world space
    Vector3[] corners = GetBoundsCorners(bounds);

    // Bruk viewport (0..1) -- dette er uavhengig av skjermoppløsning
    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
    Vector2 max = new Vector2(float.MinValue, float.MinValue);

    bool anyVisible = false;
    foreach (var corner in corners)
    {
        Vector3 vp = captureCamera.WorldToViewportPoint(corner);
        // vp.z > 0 betyr foran kameraet
        if (vp.z > 0f)
        {
            anyVisible = true;
            // Konverter viewport (0..1) til pixel i RenderTexture
            float px = vp.x * imageWidth;
            float py = vp.y * imageHeight;

            min.x = Mathf.Min(min.x, px);
            min.y = Mathf.Min(min.y, py);
            max.x = Mathf.Max(max.x, px);
            max.y = Mathf.Max(max.y, py);
        }
    }

    if (!anyVisible) return null;

    // Klamp innenfor RenderTexture dim
    min.x = Mathf.Clamp(min.x, 0f, imageWidth);
    min.y = Mathf.Clamp(min.y, 0f, imageHeight);
    max.x = Mathf.Clamp(max.x, 0f, imageWidth);
    max.y = Mathf.Clamp(max.y, 0f, imageHeight);

    float pixelWidth = max.x - min.x;
    float pixelHeight = max.y - min.y;

    if (pixelWidth < 5f || pixelHeight < 5f)
    {
        Debug.LogWarning($"Skipping {obj.name}: bbox too small ({pixelWidth}x{pixelHeight} px)");
        return null;
    }

    // Beregn YOLO-normaliserte verdier
    float xCenter = (min.x + max.x) / 2f / imageWidth;
    // NOTE: Unity viewport y=0 er bunn; YOLO typisk bruker y fra topp -> behold deres tidligere invertering:
    float yCenter = 1f - (min.y + max.y) / 2f / imageHeight;
    float width = pixelWidth / imageWidth;
    float height = pixelHeight / imageHeight;

    // Enkel sanity-check
    if (width <= 0f || height <= 0f || width > 0.99f || height > 0.99f)
    {
        Debug.LogWarning($"Skipping {obj.name}: invalid bbox size ({width:F3}, {height:F3})");
        return null;
    }

    return new YOLOAnnotation
    {
        classId = classId,
        xCenter = xCenter,
        yCenter = yCenter,
        width = width,
        height = height
    };
}

    Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents;

        return new Vector3[]
        {
            center + new Vector3(-ext.x, -ext.y, -ext.z),
            center + new Vector3(ext.x, -ext.y, -ext.z),
            center + new Vector3(-ext.x, -ext.y, ext.z),
            center + new Vector3(ext.x, -ext.y, ext.z),
            center + new Vector3(-ext.x, ext.y, -ext.z),
            center + new Vector3(ext.x, ext.y, -ext.z),
            center + new Vector3(-ext.x, ext.y, ext.z),
            center + new Vector3(ext.x, ext.y, ext.z)
        };
    }

    
    void SaveAnnotations(string imageName, List<YOLOAnnotation> annotations)
    {
        string labelPath = Path.Combine(labelsPath, imageName + ".txt");
        
        using (StreamWriter writer = new StreamWriter(labelPath))
        {
            foreach (var annotation in annotations)
            {
                string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0} {1:F6} {2:F6} {3:F6} {4:F6}",
                    annotation.classId,
                    annotation.xCenter,
                    annotation.yCenter,
                    annotation.width,
                    annotation.height);
                writer.WriteLine(line);
            }
        }
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 60, 400, 20), $"Press {captureKey} to capture training image");
        GUI.Label(new Rect(10, 80, 400, 20), $"Images captured: {imageCount}");
        GUI.Label(new Rect(10, 100, 400, 20), $"Mode: PERSON ONLY");
    }
    
    [System.Serializable]
    public class YOLOAnnotation
    {
        public int classId;
        public float xCenter;
        public float yCenter;
        public float width;
        public float height;
    }
}