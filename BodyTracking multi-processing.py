import cv2
import mediapipe as mp
from multiprocessing import Process

class BodyTracking:
    def __init__(self, id, camera) -> None:
        self.id = id
        self.mp_drawing = mp.solutions.drawing_utils
        self.mp_pose = mp.solutions.pose
        self.cap = cv2.VideoCapture(camera) 

    def start_tracking(self):
        with self.mp_pose.Pose(
            static_image_mode=False,
            min_detection_confidence=0.5,
            min_tracking_confidence=0.5) as pose:
    
            while True:
                success, frame = self.cap.read()
                if not success:
                    break
                
                # Convert the image from BGR (OpenCV) to RGB (MediaPipe)
                image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                results = pose.process(image_rgb)
                
                # Draw pose annotations on the frame
                if results.pose_landmarks:
                    self.mp_drawing.draw_landmarks(
                        frame,
                        results.pose_landmarks,
                        self.mp_pose.POSE_CONNECTIONS)
                
                cv2.imshow('Pose Tracking', frame)
                if cv2.waitKey(5) & 0xFF == 27:  # ESC to quit
                    break
        self.cap.release()
        cv2.destroyAllWindows()

def run_body_tracking(id, camera):
    bt = BodyTracking(id, camera)
    bt.start_tracking()

# Run two instances in separate processes
if __name__ == "__main__":
    process1 = Process(target=run_body_tracking, args=(1, 0))  # Camera index 0
    process2 = Process(target=run_body_tracking, args=(2, 1))  # Camera index 1

    process1.start()
    process2.start()

    process1.join()
    process2.join()




