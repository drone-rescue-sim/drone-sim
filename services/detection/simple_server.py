# detection/simple_server.py
import socket
import json
import base64
import cv2
import numpy as np
from ultralytics import YOLO
from pathlib import Path
import threading

class RobustDetectionServer:
    def __init__(self, port=9999, model_path='best.pt'):
        self.port = port

        # Resolve model path: default to local services/detection/best.pt
        base_dir = Path(__file__).parent
        if not model_path or model_path == 'best.pt':
            resolved = (base_dir / 'best.pt').resolve()
        else:
            p = Path(model_path)
            resolved = p if p.is_absolute() else (base_dir / p).resolve()

        print(f"Loading model: {resolved}")
        if not resolved.exists():
            raise FileNotFoundError(f"Model not found at {resolved}")

        self.model = YOLO(str(resolved))
        print(f"Model loaded. Classes: {self.model.names}")
        
    def start(self):
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        try:
            server_socket.bind(('127.0.0.1', self.port))
        except OSError:
            print(f"ERROR: Port {self.port} already in use!")
            return
        
        server_socket.listen(5)
        print(f"\nServer listening on port {self.port}")
        print("Press Play in Unity, then press C to connect\n")
        
        while True:
            try:
                client_socket, addr = server_socket.accept()
                print(f"=== CONNECTION from {addr} ===\n")
                
                # Handle in same thread for simplicity
                self.handle_client(client_socket)
                
            except KeyboardInterrupt:
                print("\nShutting down server...")
                break
            except Exception as e:
                print(f"Accept error: {e}")
        
        server_socket.close()
    
    def handle_client(self, client_socket):
        client_socket.settimeout(30.0) 
        
        try:
            request_count = 0
            
            while True:
                # Read all available data
                chunks = []
                client_socket.settimeout(5.0)
                
                try:
                    while True:
                        chunk = client_socket.recv(65536)
                        if not chunk:
                            break
                        chunks.append(chunk)
                        
                        # Check if we have complete JSON
                        data = b''.join(chunks)
                        try:
                            # Try to decode as JSON
                            json.loads(data.decode('utf-8'))
                            # If successful, we have complete message
                            break
                        except (json.JSONDecodeError, UnicodeDecodeError):
                            # Need more data
                            client_socket.settimeout(0.1)
                            continue
                        
                except socket.timeout:
                    if not chunks:
                        continue
                
                if not chunks:
                    print("Client disconnected (no data)")
                    break
                
                data = b''.join(chunks)
                request_count += 1
                
                print(f"\n--- Request #{request_count} ---")
                print(f"Received: {len(data)} bytes")
                
                try:
                    request = json.loads(data.decode('utf-8'))
                    
                    if request.get("type") == "frame":
                        print("Processing frame...")
                        result = self.process_frame(request["data"])
                        
                        response = json.dumps(result)
                        client_socket.sendall(response.encode('utf-8'))
                        
                        if result.get("detections"):
                            print(f"SUCCESS: Found {len(result['detections'])} objects")
                            for det in result["detections"]:
                                print(f"  - {det['class']}: {det['confidence']:.2f}")
                        else:
                            print("No detections")
                    
                except json.JSONDecodeError as e:
                    print(f"JSON error: {e}")
                    print(f"Data preview: {data[:200]}")
                    error = json.dumps({"error": "Invalid JSON"})
                    client_socket.sendall(error.encode('utf-8'))
                except Exception as e:
                    print(f"Processing error: {e}")
                    import traceback
                    traceback.print_exc()
                    error = json.dumps({"error": str(e)})
                    client_socket.sendall(error.encode('utf-8'))
                    
        except socket.timeout:
            print("Connection timeout")
        except ConnectionResetError:
            print("Connection reset by Unity")
        except Exception as e:
            print(f"Client error: {e}")
            import traceback
            traceback.print_exc()
        finally:
            client_socket.close()
            print("\n=== Connection closed ===\n")
    
    def process_frame(self, base64_frame):
        try:
            # Decode image
            img_data = base64.b64decode(base64_frame)
            nparr = np.frombuffer(img_data, np.uint8)
            frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            
            if frame is None:
                return {"error": "Could not decode image"}
            
            print(f"Image size: {frame.shape[1]}x{frame.shape[0]}")
            
            # Save image for debugging
            cv2.imwrite('debug_latest_frame.jpg', frame)
            print("Saved debug image to debug_latest_frame.jpg")
            
            # Run detection with LOWER confidence
            results = self.model(frame, conf=0.4, verbose=False) 
            
            print(f"Raw detections before filtering: {len(results[0].boxes) if results[0].boxes is not None else 0}")
            
            detections = []
            for r in results:
                boxes = r.boxes
                if boxes is not None:
                    for box in boxes:
                        cls = int(box.cls[0].item())
                        class_name = self.model.names[cls]
                        conf = box.conf[0].item()
                        x1, y1, x2, y2 = box.xyxy[0].tolist()
                        
                        print(f"  Found: {class_name} (conf: {conf:.3f}, class_id: {cls})")
                        
                        detection = {
                            "class": class_name,
                            "confidence": float(conf),
                            "bbox": {
                                "x1": float(x1),
                                "y1": float(y1),
                                "x2": float(x2),
                                "y2": float(y2),
                                "x_center": float((x1 + x2) / 2),
                                "y_center": float((y1 + y2) / 2),
                                "width": float(x2 - x1),
                                "height": float(y2 - y1)
                            }
                        }
                        detections.append(detection)
            
            return {
                "timestamp": "test",
                "detections": detections,
                "frame_size": {
                    "width": int(frame.shape[1]),
                    "height": int(frame.shape[0])
                }
            }
            
        except Exception as e:
            print(f"Frame processing error: {e}")
            import traceback
            traceback.print_exc()
            return {"error": str(e)}

if __name__ == "__main__":
    print("="*50)
    print("DETECTION SERVER")
    print("="*50)
    
    # Default to local best.pt next to this script
    server = RobustDetectionServer()
    server.start()