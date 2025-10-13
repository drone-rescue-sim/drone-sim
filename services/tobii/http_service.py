from flask import Flask, request, jsonify
import threading
import time
import json
import sys

try:
    import tobii_research as tr
except Exception as e:
    tr = None
    _import_error = str(e)

app = Flask(__name__)

# Global state (simple for now)
connected_tracker = None
gaze_listener_active = False
latest_gaze = None


def _gaze_callback(gaze_data):
    global latest_gaze
    # Convert to a minimal serializable structure
    try:
        latest_gaze = {
            "system_time_stamp": gaze_data.get("system_time_stamp"),
            "device_time_stamp": gaze_data.get("device_time_stamp"),
            "left_gaze_point_on_display_area": gaze_data.get("left_gaze_point_on_display_area"),
            "right_gaze_point_on_display_area": gaze_data.get("right_gaze_point_on_display_area"),
            "left_gaze_point_validity": gaze_data.get("left_gaze_point_validity"),
            "right_gaze_point_validity": gaze_data.get("right_gaze_point_validity"),
        }
    except Exception:
        # Fallback if not dict-like
        try:
            latest_gaze = json.loads(json.dumps(gaze_data, default=str))
        except Exception:
            latest_gaze = None


@app.route("/health", methods=["GET"])
def health():
    return jsonify({
        "status": "healthy",
        "tobii_imported": tr is not None,
        "import_error": None if tr else _import_error,
        "connected": connected_tracker is not None,
    }), 200


@app.route("/discover", methods=["GET"]) 
def discover():
    if tr is None:
        return jsonify({"error": "tobii_research not available", "hint": "pip install tobii_research"}), 500
    trackers = tr.find_all_eyetrackers()
    result = []
    for t in trackers:
        result.append({
            "address": t.address,
            "model": t.model,
            "name": t.device_name,
            "serial_number": t.serial_number,
        })
    return jsonify(result), 200


@app.route("/connect", methods=["POST"]) 
def connect():
    global connected_tracker
    if tr is None:
        return jsonify({"error": "tobii_research not available"}), 500
    data = request.get_json(silent=True) or {}
    address = data.get("address")
    if not address:
        return jsonify({"error": "Missing 'address'"}), 400
    try:
        connected_tracker = tr.EyeTracker(address)
        return jsonify({"status": "connected", "address": connected_tracker.address}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/start", methods=["POST"]) 
def start_stream():
    global gaze_listener_active
    if tr is None:
        return jsonify({"error": "tobii_research not available"}), 500
    if connected_tracker is None:
        return jsonify({"error": "Not connected"}), 400
    if gaze_listener_active:
        return jsonify({"status": "already_running"}), 200
    try:
        connected_tracker.subscribe_to(tr.EYETRACKER_GAZE_DATA, _gaze_callback, as_dictionary=True)
        gaze_listener_active = True
        return jsonify({"status": "started"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/stop", methods=["POST"]) 
def stop_stream():
    global gaze_listener_active
    if tr is None:
        return jsonify({"error": "tobii_research not available"}), 500
    if connected_tracker is None:
        return jsonify({"error": "Not connected"}), 400
    try:
        if gaze_listener_active:
            connected_tracker.unsubscribe_from(tr.EYETRACKER_GAZE_DATA, _gaze_callback)
            gaze_listener_active = False
        return jsonify({"status": "stopped"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/gaze", methods=["GET"]) 
def get_latest_gaze():
    return jsonify({"gaze": latest_gaze}), 200


if __name__ == "__main__":
    print("🚀 Starting Tobii HTTP Service...")
    print("📡 Listening on http://127.0.0.1:5007")
    print("🧭 Discover trackers: GET /discover")
    print("🔗 Connect: POST /connect {address}")
    print("▶️ Start stream: POST /start | ⏹ Stop: POST /stop")
    print("👁️ Latest gaze: GET /gaze | 💚 Health: GET /health")
    print("-" * 50)
    app.run(host="127.0.0.1", port=5007, debug=True)


