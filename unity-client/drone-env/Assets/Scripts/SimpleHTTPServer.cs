using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SimpleHTTPServer : MonoBehaviour
{
    private Thread serverThread;
    private TcpListener listener;
    private bool isRunning = false;

    [Header("Server Settings")]
    public int port = 5005;
    public DroneCommandUI commandUI;

    private void Start()
    {
        StartServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void StartServer()
    {
        if (isRunning) return;

        isRunning = true;
        serverThread = new Thread(new ThreadStart(ListenForRequests));
        serverThread.IsBackground = true;
        serverThread.Start();

        Debug.Log($"HTTP Server started on port {port}");
    }

    private void StopServer()
    {
        isRunning = false;
        if (listener != null)
        {
            listener.Stop();
        }
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Abort();
        }
        Debug.Log("HTTP Server stopped");
    }

    private void ListenForRequests()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (isRunning)
            {
                if (listener.Pending())
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.IsBackground = true;
                    clientThread.Start(client);
                }
                Thread.Sleep(100);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Server error: {e.Message}");
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = null;

        try
        {
            stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string response = ProcessRequest(request);
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Client handling error: {e.Message}");
        }
        finally
        {
            if (stream != null) stream.Close();
            client.Close();
        }
    }

    private string ProcessRequest(string request)
    {
        Debug.Log($"Received request: {request}");

        try
        {
            // Simple HTTP parsing
            string[] lines = request.Split('\n');
            if (lines.Length > 0)
            {
                string requestLine = lines[0].Trim();
                if (requestLine.Contains("POST /receive_command"))
                {
                    // Extract JSON body
                    string body = ExtractBody(request);
                    if (!string.IsNullOrEmpty(body))
                    {
                        // Parse JSON and execute command
                        CommandData commandData = JsonUtility.FromJson<CommandData>(body);
                        if (commandData != null && !string.IsNullOrEmpty(commandData.command))
                        {
                            // Execute command on main thread
                            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                            {
                                ExecuteDroneCommand(commandData);
                            });

                            return CreateHttpResponse(200, "OK", "Command received and executed");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing request: {e.Message}");
            return CreateHttpResponse(500, "Internal Server Error", $"Error: {e.Message}");
        }

        return CreateHttpResponse(400, "Bad Request", "Invalid request");
    }

    private string ExtractBody(string request)
    {
        string[] parts = request.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);
        if (parts.Length > 1)
        {
            return parts[1].Trim();
        }
        return "";
    }

    private string CreateHttpResponse(int statusCode, string statusText, string body)
    {
        return $"HTTP/1.1 {statusCode} {statusText}\r\n" +
               "Content-Type: text/plain\r\n" +
               $"Content-Length: {body.Length}\r\n" +
               "Access-Control-Allow-Origin: *\r\n" +
               "Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n" +
               "Access-Control-Allow-Headers: Content-Type\r\n" +
               "\r\n" +
               body;
    }

    private void ExecuteDroneCommand(CommandData commandData)
    {
        if (commandUI != null)
        {
            commandUI.ExecuteDroneCommand(commandData.command);
        }
        else
        {
            Debug.Log($"Executing command: {commandData.command} - {commandData.details}");
        }
    }

    [System.Serializable]
    private class CommandData
    {
        public string command;
        public string details;
    }
}

// Singleton for dispatching actions to main thread
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly object _lock = new object();
    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("UnityMainThreadDispatcher");
                    _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
