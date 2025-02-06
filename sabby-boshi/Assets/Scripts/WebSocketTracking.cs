using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Unity.Netcode;
using System.Drawing;
using Unity.Networking.Transport;
using Unity.VisualScripting;

public class WebSocketTracking : MonoBehaviour
{
    private WebSocket websocket;

    // (Not used in this version) The prefab for a single landmark point.
    public GameObject pointPrefab;

    // Parent GameObject that already contains the landmark children (named "Landmark 0", "Landmark 1", etc.).
    // public GameObject player;

    // Expected number of landmarks (e.g., MediaPipe returns 33 landmarks).
    public int numberOfLandmarks = 33;

    // Smoothing speed for position interpolation.
    public float smoothingSpeed = 2.0f;

    // Dictionary to hold the landmark objects (keyed by landmark id).
    private Dictionary<int, GameObject> landmarkPoints = new Dictionary<int, GameObject>();

    // Dictionary to hold the target local positions for each landmark.
    private Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();

    // A queue to store incoming pose data messages.
    private Queue<PoseData> poseQueue = new Queue<PoseData>();

    public float offset;

    void Start()
    {
        // Ensure that the player GameObject has been assigned.
        if (gameObject == null)
        {
            Debug.LogError("Player GameObject is not assigned in the Inspector!");
            return;
        }

        ConnectToWebSocket();

        // Instead of instantiating new objects, find existing child objects under 'player'
        /*for (int i = 0; i < numberOfLandmarks; i++)
        {
            Transform landmarkTransform = gameObject.transform.Find("Landmark " + i);
            if (landmarkTransform != null)
            {
                GameObject landmark = landmarkTransform.gameObject;
                landmarkPoints.Add(i, landmark);
                targetPositions.Add(i, landmark.transform.localPosition);
            }
            else
            {
                Debug.LogError("Could not find child object named 'Landmark " + i + "' under player.");
            }
        }*/
        if (gameObject.GetComponent<NetworkObject>().IsOwner == true) 
        {
            for (int i = 0; i < numberOfLandmarks; i++)
            {
                GameObject point = Instantiate(pointPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
                point.name = "Landmark " + i;

                Transform landmarkTransform = gameObject.transform.GetChild(i);
                if (landmarkTransform != null)
                {
                    GameObject landmark = landmarkTransform.gameObject;
                    NetworkObject netObj = landmark.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        netObj.Spawn();
                        netObj.TrySetParent(gameObject, false);

                        landmarkPoints.Add(i, point);
                        targetPositions.Add(i, point.transform.localPosition);
                    }
                }
            }
        }
        // If using Unity.Netcode, optionally spawn network objects.

    }

    async void ConnectToWebSocket()
    {
        // Connect to the Python WebSocket server at ws://localhost:8765.
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
            // For each landmark in the received data, compute its world position,
            // convert it to local space relative to 'gameObject', and update the target.
            foreach (var landmark in nextPose.landmarks)
            {
                // Flip the y coordinate to match Unity's screen coordinate system (0 at bottom).
                float flippedY = 1.0f - landmark.y;
                float flippedX = 1.0f - landmark.x;
                float screenX = flippedX * Screen.width;
                float screenY = flippedY * Screen.height;

                // Use a z value close to the camera's near clip plane; adjust as needed.
                float zPos = Camera.main.nearClipPlane + 1f;
                // The original code had a conditional offset based on pose id.
                // You can adjust or remove this offset as needed.
                
                Vector3 screenPos = new Vector3((nextPose.id == 1) ? screenX - offset : screenX + offset, screenY, zPos);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

                Debug.Log(nextPose.id);

                // Convert the world position to local space relative to the player.
                Vector3 localPos = gameObject.transform.InverseTransformPoint(worldPos);

                // Update the target local position for this landmark.
                targetPositions[landmark.id] = localPos;
            }
        }

        // Smoothly interpolate each landmark object's local position toward its target local position.
        foreach (var pair in landmarkPoints)
        {
            int id = pair.Key;
            float scale = 0.0f;
            GameObject point = pair.Value;
            if (targetPositions.ContainsKey(id))
            {
                if (point.GetComponent<NetworkObject>().OwnerClientId == 1)
                { 
                    scale = (point.transform.position.x > 0) ? 0f : 0.3f;
                }
                else if (point.GetComponent<NetworkObject>().OwnerClientId == 2)
                {
                    scale = (point.transform.position.x < 0) ? 0f : 0.3f;
                }

                point.transform.localScale = new Vector3(scale, scale, scale);

                Vector3 currentLocalPos = point.transform.localPosition;
                Vector3 targetLocalPos = targetPositions[id];
                Vector3 newLocalPos = Vector3.Lerp(currentLocalPos, targetLocalPos, Time.deltaTime * smoothingSpeed);
                point.transform.localPosition = newLocalPos;
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
