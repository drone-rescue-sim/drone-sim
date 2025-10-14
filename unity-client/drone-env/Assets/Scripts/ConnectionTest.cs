using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DetectionResponse
{
    public string timestamp;
    public List<SimpleDetection> detections;
    public FrameSize frame_size;
    public string error;
}

[System.Serializable]
public class SimpleDetection
{
    [System.Serializable]
    public class SimpleBbox
    {
        public float x1, y1, x2, y2;
        public float x_center, y_center;
        public float width, height;
    }

    [JsonPropertyAttribute("class")]
    public string className;
    public float confidence;
    public SimpleBbox bbox;
}

[System.Serializable]
public class FrameSize
{
    public int width, height;
}

public class ConnectionTest : MonoBehaviour
{
    [Header("Connection")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 9999;

    [Header("Test")]
    public Camera testCamera;
    public bool autoTest = false;
    public float testInterval = 2f;

    [Header("Visualization")]
    public bool showBoundingBoxes = true;
    public Color personBoxColor = Color.yellow;
    public Color fireBoxColor = Color.red;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private bool isConnected = false;
    private float lastTestTime = 0f;
    // OnGUI overlay state (no lingering boxes)
    private List<SimpleDetection> lastDetections = null;
    private FrameSize lastFrameSize = null;
    private Texture2D _lineTex;
    private int lastDetectionsFrame = -1;

    void Start()
    {
        if (testCamera == null)
            testCamera = Camera.main;

        // 1x1 texture used to draw thin outlines in OnGUI
        _lineTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _lineTex.SetPixel(0, 0, Color.white);
        _lineTex.Apply();

        Debug.Log("Connection Test ready (C=connect, T=test, D=disconnect)");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            ConnectToServer();
        else if (Input.GetKeyDown(KeyCode.T) && isConnected)
            TestDetection();
        else if (Input.GetKeyDown(KeyCode.D))
            Disconnect();

        if (autoTest && isConnected && Time.time - lastTestTime > testInterval)
        {
            TestDetection();
            lastTestTime = Time.time;
        }
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;
            Debug.Log($"Connected to server {serverIP}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
            isConnected = false;
        }
    }

    void TestDetection()
    {
        if (!isConnected)
        {
            Debug.LogWarning("Not connected!");
            return;
        }

        try
        {
            string base64Frame = CaptureFrame();
            if (string.IsNullOrEmpty(base64Frame))
                return;

            string jsonRequest = "{\"type\":\"frame\",\"data\":\"" + base64Frame + "\"}";
            byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
            stream.Write(data, 0, data.Length);
            stream.Flush();

            byte[] buffer = new byte[65536];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Debug.LogError("Empty response from server");
                return;
            }

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            DetectionResponse result = JsonUtility.FromJson<DetectionResponse>(response);

            if (!string.IsNullOrEmpty(result.error))
            {
                Debug.LogError($"Server error: {result.error}");
                return;
            }

            if (result.detections != null && result.detections.Count > 0)
            {
                Debug.Log($"Detected {result.detections.Count} objects");
                // Store detections for overlay drawing this frame only
                lastDetections = result.detections;
                lastFrameSize = result.frame_size ?? new FrameSize { width = 640, height = 480 };
                lastDetectionsFrame = Time.frameCount;
            }
            else
            {
                Debug.Log("No detections");
                lastDetections = null;
                lastDetectionsFrame = -1;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Detection error: {e.Message}");
            isConnected = false;
        }
    }

    string CaptureFrame()
    {
        RenderTexture renderTexture = new RenderTexture(640, 480, 24);
        testCamera.targetTexture = renderTexture;
        testCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D screenShot = new Texture2D(640, 480, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
        screenShot.Apply();

        testCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        byte[] imageData = screenShot.EncodeToJPG(75);
        Destroy(screenShot);
        return Convert.ToBase64String(imageData);
    }

    // Draw thin rectangle outline in OnGUI
    void DrawRectOutline(Rect rect, Color color, float thickness = 2f)
    {
        Color old = GUI.color;
        GUI.color = color;
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), _lineTex);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), _lineTex);
        // Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), _lineTex);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), _lineTex);
        GUI.color = old;
    }

    void Disconnect()
    {
        if (stream != null) stream.Close();
        if (tcpClient != null) tcpClient.Close();
        isConnected = false;
        Debug.Log("Disconnected");
    }

    void OnDestroy()
    {
        Disconnect();
        if (_lineTex != null) Destroy(_lineTex);
    }

    void OnGUI()
    {
        GUI.color = isConnected ? Color.green : Color.red;
        GUI.Label(new Rect(10, 10, 200, 20), $"Status: {(isConnected ? "CONNECTED" : "DISCONNECTED")}");
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 30, 400, 20), "C=Connect | T=Test | D=Disconnect");

        // Draw YOLO xyxy-style boxes for the frame we received results
        if (showBoundingBoxes && lastDetections != null && lastFrameSize != null && Time.frameCount == lastDetectionsFrame)
        {
            float sx = Screen.width / (float)Mathf.Max(1, lastFrameSize.width);
            float sy = Screen.height / (float)Mathf.Max(1, lastFrameSize.height);
            foreach (var det in lastDetections)
            {
                // Use xyxy directly
                float x1 = det.bbox.x1 * sx;
                float y1 = det.bbox.y1 * sy;
                float x2 = det.bbox.x2 * sx;
                float y2 = det.bbox.y2 * sy;
                // Convert to screen rect (Unity GUI origin top-left)
                float w = Mathf.Max(1f, x2 - x1);
                float h = Mathf.Max(1f, y2 - y1);
                Rect r = new Rect(x1, y1, w, h);
                DrawRectOutline(r, det.className == "fire" ? fireBoxColor : personBoxColor, 2f);
            }
        }
    }
}

public class JsonPropertyAttribute : System.Attribute
{
    public string PropertyName { get; }
    public JsonPropertyAttribute(string propertyName) { PropertyName = propertyName; }
}
