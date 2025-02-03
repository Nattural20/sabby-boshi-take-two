import cv2
import mediapipe as mp
import asyncio
import websockets
import json
import time

class BodyTracking:
    def __init__(self, id, camera, ws_port: int) -> None:
        """Initialize body tracking with OpenCV and WebSocket settings."""
        self.id = id
        self.mp_drawing = mp.solutions.drawing_utils
        self.mp_pose = mp.solutions.pose
        self.cap = cv2.VideoCapture(camera)

        if not isinstance(ws_port, int):
            raise ValueError(f"ws_port must be an integer, got {type(ws_port)} instead: {ws_port}")
        self.ws_port = ws_port

    async def send_data(self, websocket, pose_landmarks):
        """Send pose landmarks as JSON to Unity."""
        try:
            data = {"id": self.id, "landmarks": pose_landmarks}
            await websocket.send(json.dumps(data))
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket client disconnected.")

    async def track_and_stream(self, websocket, path=None):  # Made path optional
        """Continuously capture video frames, process landmarks, and send them via WebSocket."""
        print(f"Client connected from {websocket.remote_address}")

        with self.mp_pose.Pose(
            static_image_mode=False,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5
        ) as pose:

            while True:
                start_time = time.time()  # import time at the top of your file
                success, frame = self.cap.read()
                if not success:
                    print("Failed to read frame, stopping...")
                    break

                # Convert the image from BGR (OpenCV) to RGB (MediaPipe)
                image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                results = pose.process(image_rgb)

                # Inside your track_and_stream method:
                if results.pose_landmarks:
                    pose_landmarks = [
                        {"id": i, "x": lm.x, "y": lm.y} 
                        for i, lm in enumerate(results.pose_landmarks.landmark)
                    ]
                    await self.send_data(websocket, pose_landmarks)


                self.mp_drawing.draw_landmarks(
                    frame, results.pose_landmarks, self.mp_pose.POSE_CONNECTIONS
                )

                cv2.imshow('Pose Tracking', frame)
                if cv2.waitKey(5) & 0xFF == 27:  # ESC to quit
                    print("ESC pressed, closing...")
                    break
                
                # # print out the frame processing time
                # end_time = time.time()
                # fps = 1.0 / (end_time - start_time)
                # print(f"Processing at {fps:.1f} FPS")

        print("Releasing resources...")
        self.cap.release()
        cv2.destroyAllWindows()

    async def start_server(self):
        """Start WebSocket server to send pose data."""
        try:
            print(f"Starting WebSocket server on ws://localhost:{self.ws_port}")
            print(f"Type of ws_port: {type(self.ws_port)}")
            print(f"Is track_and_stream callable? {callable(self.track_and_stream)}")
            print(f"Serving WebSocket with args: host='localhost', port={self.ws_port}")

            # Use explicit keyword arguments for host and port
            server = await websockets.serve(
                self.track_and_stream,
                host="localhost",
                port=self.ws_port,
                compression=None
            )

            print("WebSocket server started successfully.")
            await server.wait_closed()
        except Exception as e:
            print(f"WebSocket server error: {e}")

# Explicitly ensure the port is correct
ws_port = 8765
print(f"Starting BodyTracking instance with port {ws_port} of type {type(ws_port)}")
bt = BodyTracking(1, 0, ws_port)

# Run the WebSocket server in an event loop
asyncio.run(bt.start_server())
