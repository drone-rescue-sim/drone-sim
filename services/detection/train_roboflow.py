import argparse
import shutil
from pathlib import Path
from ultralytics import YOLO


def train(
    data_path: Path,
    model_name: str = "yolov8n.pt",
    epochs: int = 120,
    imgsz: int = 960,
    batch: int = 16,
    run_name: str = "person_detectors",
):
    print("=== YOLOv8 Training (Roboflow dataset) ===")
    print(f"Data:     {data_path}")
    print(f"Model:    {model_name}")
    print(f"Epochs:   {epochs}")
    print(f"ImageSz:  {imgsz}")
    print(f"Batch:    {batch}")
    print(f"Run name: {run_name}")

    if not data_path.exists():
        raise FileNotFoundError(f"data.yaml not found at: {data_path}")

    model = YOLO(model_name)

    results = model.train(
        data=str(data_path),
        epochs=epochs,
        imgsz=imgsz,
        batch=batch,
        name=run_name,
        project="runs/detect",
        patience=30,
        save=True,
        plots=True,
        exist_ok=True,
    )

    best_path = Path("runs/detect") / run_name / "weights" / "best.pt"
    if best_path.exists():
        dst = Path(__file__).parent / "best.pt"
        shutil.copy2(best_path, dst)
        print(f"\n Best weights copied to: {dst}")
    else:
        print("\nCould not find best.pt under runs; skipping copy.")

    print("\nTraining complete.")
    return results


def main():
    parser = argparse.ArgumentParser(description="Train YOLOv8 on Roboflow dataset")
    default_data = Path(__file__).parent / "roboflow" / "data.yaml"
    parser.add_argument("--data", type=str, default=str(default_data), help="Path to data.yaml")
    parser.add_argument("--model", type=str, default="yolov8n.pt", help="Base model (e.g., yolov8n.pt)")
    parser.add_argument("--epochs", type=int, default=120, help="Epochs")
    parser.add_argument("--imgsz", type=int, default=960, help="Image size")
    parser.add_argument("--batch", type=int, default=16, help="Batch size")
    parser.add_argument("--name", type=str, default="person_detectors", help="Run name")
    args = parser.parse_args()

    train(
        data_path=Path(args.data),
        model_name=args.model,
        epochs=args.epochs,
        imgsz=args.imgsz,
        batch=args.batch,
        run_name=args.name,
    )


if __name__ == "__main__":
    main()


