# detection/train_custom_model.py
import os
import shutil
from pathlib import Path
from ultralytics import YOLO
import yaml

class CustomFirePersonTrainer:
    def __init__(self, training_data_path):
        """
        Complete training pipeline for Unity person detection
        """
        self.training_data_path = Path(training_data_path)
        self.dataset_path = Path("dataset")
        
    def prepare_dataset(self):
        """
        Organize collected Unity images into train/val split
        """
        print("Preparing dataset...")
        
        # Reset dataset dir to avoid stale labels
        if self.dataset_path.exists():
            shutil.rmtree(self.dataset_path)

        # Create dataset structure
        for split in ['train', 'val']:
            (self.dataset_path / split / 'images').mkdir(parents=True, exist_ok=True)
            (self.dataset_path / split / 'labels').mkdir(parents=True, exist_ok=True)
        
        # Get all images
        images_dir = self.training_data_path / 'images'
        labels_dir = self.training_data_path / 'labels'
        
        image_files = list(images_dir.glob('*.jpg'))
        print(f"Found {len(image_files)} training images")
        
        if len(image_files) == 0:
            print("ERROR: No training images found!")
            print(f"Expected images in: {images_dir}")
            return False
        
        # Split 80/20 train/val
        split_idx = int(len(image_files) * 0.8)
        train_images = image_files[:split_idx]
        val_images = image_files[split_idx:]
        
        print(f"Train: {len(train_images)}, Val: {len(val_images)}")
        
        # Copy images and rewrite labels to single-class person (class 0)
        def rewrite_label(src_label: Path, dst_label: Path):
            """Keep only person entries and remap class id to 0.
            Accept common mappings: 1 (from ['fire','person']) or 0 (already single-class).
            """
            try:
                text = src_label.read_text().strip()
                if not text:
                    return False
                lines = text.splitlines()
            except Exception:
                return False

            kept = []
            for line in lines:
                parts = line.strip().split()
                if len(parts) < 5:
                    continue
                try:
                    cls_id = int(float(parts[0]))
                except Exception:
                    continue
                if cls_id in (1, 0):
                    parts[0] = '0'  # remap to single-class person
                    kept.append(' '.join(parts))

            if not kept:
                # No person labels; write empty file so YOLO can handle images without objects
                dst_label.write_text("")
                return False

            dst_label.write_text('\n'.join(kept) + '\n')
            return True

        for img_path in train_images:
            (self.dataset_path / 'train' / 'images' / img_path.name).write_bytes(img_path.read_bytes())
            src_label = labels_dir / (img_path.stem + '.txt')
            dst_label = self.dataset_path / 'train' / 'labels' / (img_path.stem + '.txt')
            if src_label.exists():
                rewrite_label(src_label, dst_label)
            else:
                dst_label.write_text("")
        
        for img_path in val_images:
            (self.dataset_path / 'val' / 'images' / img_path.name).write_bytes(img_path.read_bytes())
            src_label = labels_dir / (img_path.stem + '.txt')
            dst_label = self.dataset_path / 'val' / 'labels' / (img_path.stem + '.txt')
            if src_label.exists():
                rewrite_label(src_label, dst_label)
            else:
                dst_label.write_text("")
        
        # Create data.yaml
        data_yaml = {
            'path': str(self.dataset_path.absolute()),
            'train': 'train/images',
            'val': 'val/images',
            'nc': 1,
            'names': ['person']
        }
        
        yaml_path = self.dataset_path / 'data.yaml'
        with open(yaml_path, 'w') as f:
            yaml.dump(data_yaml, f)
        
        print(f"Dataset prepared: {yaml_path}")
        return True
    
    def train_model(self, epochs=30, img_size=640):
        """
        Train custom YOLO model
        """
        print("\nStarting training...")
        
        # Load pretrained model
        model = YOLO('yolov8n.pt')
        
        # Train
        results = model.train(
            data=str(self.dataset_path / 'data.yaml'),
            epochs=epochs,
            imgsz=img_size,
            batch=8,
            name='person_detector',
            patience=10,  # Early stopping
            save=True,
            plots=True
        )
        
        print("\nTraining complete!")
        print(f"Best model saved to: runs/detect/person_detector/weights/best.pt")
        
        return results
    
    def test_model(self, model_path=None):
        """
        Test trained model on validation set
        """
        if model_path is None:
            model_path = 'runs/detect/person_detector/weights/best.pt'
        
        print(f"\nTesting model: {model_path}")
        
        model = YOLO(model_path)
        
        # Validate
        results = model.val()
        
        print(f"mAP50: {results.box.map50:.3f}")
        print(f"mAP50-95: {results.box.map:.3f}")
        
        return results

def main():
    print("=== CUSTOM PERSON-ONLY DETECTION TRAINING ===\n")
    
    # Path to Unity collected data
    training_data_path = "../../unity-client/drone-env/TrainingData"
    
    if not Path(training_data_path).exists():
        print(f"ERROR: Training data not found at {training_data_path}")
        print("Please collect training data first using TrainingDataCollector in Unity")
        return
    
    trainer = CustomFirePersonTrainer(training_data_path)
    
    # Step 1: Prepare dataset
    if not trainer.prepare_dataset():
        return
    
    # Step 2: Train model
    print("\nStarting training (this may take a while)...")
    trainer.train_model(epochs=50)
    
    # Step 3: Test model
    trainer.test_model()
    
    print("\n=== TRAINING COMPLETE ===")
    print("Next steps:")
    print("1. Copy best.pt to detection/ folder")
    print("2. Ensure simple_server.py uses your new best.pt and person-only filtering")
    print("3. Run server and test in Unity")

if __name__ == "__main__":
    main()