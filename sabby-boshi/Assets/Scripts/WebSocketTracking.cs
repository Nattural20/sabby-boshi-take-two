using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketTracking : MonoBehaviour
{
    private WebSocket websocket;

    // The prefab for a single landmark point. Assign this in the Inspector.
    public GameObject pointPrefab;

    // Expected number of landmarks (e.g., MediaPipe returns 33 landmarks).
    public int numberOfLandmarks = 33;

    // Smoothing speed for position interpolation.
    public float smoothingSpeed = 10.0f;

    // Dictionary to hold the instantiated landmark objects (keyed by landmark id).
    private Dictionary<int, GameObject> landmarkPoints = new Dictionary<int, GameObject>();

    // Dictionary to hold the target positions for each landmark.
    private Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();

    // A queue to store incoming pose data messages.
    private Queue<PoseData> poseQueue = new Queue<PoseData>();

    void Start()
    {
        // Instantiate a GameObject for each expected landmark and initialize positions.
        for (int i = 0; i < numberOfLandmarks; i++)
        {
            GameObject point = Instantiate(pointPrefab, Vector3.zero, Quaternion.identity);
            point.name = "Landmark " + i;
            landmarkPoints.Add(i, point);
            targetPositions.Add(i, point.transform.position);
        }
        ConnectToWebSocket();
    }

    async void ConnectToWebSocket()
    {
        // Connect to the Python WebSocket server at ws://localhost:8765
        websocket = new WebSocket("ws://localhost:8765");

        websocket.OnMessage += (bytes) =>
        {
            // Convert the received bytes into a string.
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            // Log a short snippet of the message with a timestamp.
            Debug.Log($"Received at {DateTime.Now:HH:mm:ss.fff}: {message.Substring(0, Math.Min(message.Length, 60))}...");

            // Parse the JSON into our PoseData object.
            PoseData poseData = JsonUtility.FromJson<PoseData>(message);

            // Enqueue the parsed data for processing in Update.
            lock (poseQueue)
            {
                poseQueue.Enqueue(poseData);
            }
        };

        await websocket.Connect();
    }

    void Update()
    {
        // Process at most one pose update per frame from the queue.
        PoseData nextPose = null;
        lock (poseQueue)
        {
            if (poseQueue.Count > 0)
            {
                nextPose = poseQueue.Dequeue();
            }
        }
        if (nextPose != null)
        {
            // For each landmark in the received data, compute its world position.
            foreach (var landmark in nextPose.landmarks)
            {
                // Flip the y coordinate to match Unity's coordinate system (0 at bottom).
                float flippedY = 1.0f - landmark.y;
                float screenX = landmark.x * Screen.width;
                float screenY = flippedY * Screen.height;

                // Use a z value close to the camera's near clip plane; adjust as needed.
                float zPos = Camera.main.nearClipPlane + 1f;
                Vector3 screenPos = new Vector3(screenX, screenY, zPos);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

                // Update the target position for this landmark.
                targetPositions[landmark.id] = worldPos;
            }
        }

        // Smoothly interpolate each landmark object's position toward its target position.
        foreach (var pair in landmarkPoints)
        {
            int id = pair.Key;
            GameObject point = pair.Value;
            if (targetPositions.ContainsKey(id))
            {
                Vector3 currentPos = point.transform.position;
                Vector3 targetPos = targetPositions[id];
                Vector3 newPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothingSpeed);
                point.transform.position = newPos;
            }
        }

        // Ensure incoming WebSocket messages are processed.
        websocket?.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}

// Data classes for JSON parsing.
[Serializable]
public class PoseData
{
    public int id;  // This could represent the id of the pose (e.g., for multiple people)
    public List<Landmark> landmarks;
}

[Serializable]
public class Landmark
{
    public int id;  // The landmark id (e.g., 0 for nose, 1 for left eye, etc.)
    public float x; // Normalized x coordinate (0 to 1, possibly outside if scaled)
    public float y; // Normalized y coordinate (0 to 1, possibly outside if scaled)
}
