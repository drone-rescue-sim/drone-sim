from ultralytics import YOLO
import cv2
import math 

#video import
cap = cv2.VideoCapture('240p1.mp4')

# loading pretrained weights on specific dataset (COCO)
model = YOLO("yolo-Weights/yolov8n.pt")

DEVICE = "cpu"

# object classes
classNames = ["person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat",
              "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
              "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella",
              "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat",
              "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup",
              "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli",
              "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed",
              "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone",
              "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors",
              "teddy bear", "hair drier", "toothbrush"
              ]

while True:
    success, img = cap.read()
    
    if not success:
        break

    results = model(img, stream=True)

    # coordinates
    for r in results:
        boxes = r.boxes

        for box in boxes:
            # bounding box
            x1, y1, x2, y2 = box.xyxy[0]
            x1, y1, x2, y2 = int(x1), int(y1), int(x2), int(y2) # convert to int values

            # confidence
            confidence = float(box.conf[0])
            print("Confidence --->",confidence)

            if confidence > 0.7:
                color = (0, 255, 0)   # green for high confidence
            elif confidence > 0.4:
                color = (0, 255, 255) # yellow for medium
            else:
                color = (0, 0, 255)   # red for low

            # draw rectangle
            cv2.rectangle(img, (x1, y1), (x2, y2), color, 3)

            # class name
            cls = int(box.cls[0])
            print("Class name -->", classNames[cls])

            # put label text
            label = f"{classNames[cls]} {confidence:.2f}"

            #output text: img frame, className, org, font, fontScale, color, thickness
            cv2.putText(img, label, (x1, y1 - 10),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, color, 2)

    cv2.imshow('Output video', img)
    if cv2.waitKey(1) == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()