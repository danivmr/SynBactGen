
![Logo](Resources/Logo/logo_text.png)

# Synthetic Data for Detecting Bacterial Morphology Across Multiple Shapes

## Overview

Simulating realistic microbiology data is a challenge due to the need for specialized equipment such as microscopes, cameras, and bacterial cultures—as well as domain expertise in bacteriology. This project proposes a solution by generating synthetic datasets using a video game engine that simulates bacterial colonies.

The system creates synthetic images alongside automated annotations. Each bacterium's position is marked with a 2D bounding box, generating data compatible with modern object detection frameworks. The dataset's effectiveness is evaluated using the YOLO (You Only Look Once) object detection algorithm.

## Table of Contents

1. [Proof of Concept: Multi-Class Bacteria Detection Model](#proof-of-concept-multi-class-bacteria-detection-model)
2. [Complete Experimental Flow](#complete-experimental-flow)
   - [Unity Dataset Generation](#1-unity-dataset-generation)
   - [Dataset Format and Conversion](#2-dataset-format-and-conversion)
   - [Model Training and Inference](#3-model-training-and-inference)

---

## Proof of Concept: Multi-Class Bacteria Detection Model

This section shows you how to run a pre-trained model on bacterial images using Google Colab. No setup required—just load the notebook and start making predictions on example images or your own data.

### Running Inference

1. Open the notebook using the "Open in Colab" button at the top of [YOLOV26_Proof_Of_Concept.ipynb](JupyterNotebooks/YOLOV26_Proof_Of_Concept.ipynb)
2. Skip to the **Inference and Results Visualization** section (Section 3)
3. Load the pre-trained model from `Models/yolo26-bacteria-det-synbactgen-v1.pt`
4. Use the images provided in this repository or provide your real bacterial images in the `source` parameter.
5. The notebook will generate annotated predictions with detected bacterial objects and confidence scores in the runs/detect/predict directory folder

This approach is ideal for:
- Testing the model on the provided example images or predict position and shape on new, unseen bacterial images
- Quick evaluation without retraining
- Integration into analysis pipelines

## Complete Experimental Flow

This section provides the complete pipeline for generating synthetic bacterial datasets, converting them to YOLO format, training your own detection model, and evaluating its performance. Follow all three subsections in order for a complete end-to-end workflow.

### 1. Unity Dataset Generation

Generate synthetic bacterial images with automated annotations using the Unity Perception framework. This creates randomized bacterial colonies with precise 2D bounding box labels ready for model training.

**System Requirements:**
- **OS**: Windows 10/11, macOS, or Linux
- **Unity Editor**: Version 2021.3.18f1
- **RAM**: 8GB minimum (16GB+ recommended)
- **CPU**: Multi-core processor (e.g., AMD Ryzen 7 5800X or equivalent)
- **Storage**: 5-10GB for Unity project and generated datasets
  - Example: 600 images (520x520 resolution) with annotations = ~185 MB

**Installation Steps:**
1. **Clone the Repository**
   ```bash
   git clone https://github.com/danivmr/SynBactGen.git
   cd Bacteria-dataset-generator
   ```

2. **Download Unity**
   - Download Unity Editor version 2021.3.18f1 from [Unity Download Archive](https://unity3d.com/download/download_unity)

#### Tutorial: Running the Project and Generating Datasets

1. Clone the repository using Git
2. Open Unity Hub and select "Add project from disk"

![alt text](Resources/TutorialImages/image.png)

3. Navigate to the `BacteriaGeneratorUnity` folder from the cloned repository and select it. Use Unity Editor version 2021.3.18f1.

4. Open the project with the selected Editor version.

5. Once the project opens, go to File > Open Scene and select `TechnicalNoteV1.unity`. This will load the configuration required for dataset generation.

6. Navigate to the simulation scenario to view the configured randomizers. Refer to the Randomizers and Configurations section below for customization details. 

![alt text](Resources/TutorialImages/image-1.png)

7. Click Pause, then Play to observe the image generation step-by-step. Click the Step button to advance to the next generated image. To generate all images continuously, click Play without pausing.

![alt text](Resources/TutorialImages/image-2.png)

8. Navigate to Project Settings > Perception to specify the base path where the dataset will be saved.

![alt text](Resources/TutorialImages/image-3.png)

**Note:** If you encounter issues during generation, refer to the [Unity Perception Tutorial](https://docs.unity3d.com/Packages/com.unity.perception@1.0/manual/Tutorial/Phase1.html) for guidance on configuring HDRP, camera, and lighting.

### 2. Dataset Format and Conversion

Convert your generated dataset from the Unity Perception format to YOLO format, the standard input format for YOLO algorithms. This includes splitting data into training and validation sets, normalizing bounding box coordinates, and organizing files.

#### Prerequisites

- **Python**: 3.8 or higher
- **RAM**: 4GB minimum for typical datasets
- **Generated Dataset**: A dataset from Section 1 (Unity Dataset Generation) or use the provided example dataset

#### Installation Steps
1. **Set Up Python Environment**
   ```bash
   python -m venv venv
   # On Windows:
   venv\Scripts\activate
   # On macOS/Linux:
   source venv/bin/activate
   ```

2. **Install Python Dependencies**
   ```bash
   pip install notebook pillow numpy pandas
   ```

#### Tutorial: Converting to YOLO Format

#### Input Dataset Structure (Generated from Unity)

The generated dataset from Unity has the following structure:

```
dataset/
├── sequence_0/
│   ├── Step0.camera.png
│   ├── step0.frame_data.json
│   └── ...
├── sequence_1/
│   └── ...
├── annotation_definitions.json
├── metadata.json
├── metric_definitions.json
└── sensor_definitions.json
```

Each sequence contains:
- **Step*.camera.png** - RGB image of bacterial colonies
- **step*.frame_data.json** - Frame metadata and bounding box annotations for detected bacteria

#### Conversion Steps

Follow the instructions in [ConvertFormatToYOLO.ipynb](JupyterNotebooks/ConvertFormatToYOLO.ipynb) to:

1. **Extract class labels** from annotation definitions
2. **Split data** into training and validation sets
3. **Convert bounding box coordinates** from custom format to YOLO normalized format (center coordinates in 0-1 range)
4. **Generate output structure** with organized train/valid splits containing images and labels
5. **Archive the dataset** as a compressed file

#### Output Dataset Structure (YOLO Format)

The notebook will produce:
```
converted_dataset/
├── train/
│   ├── images/
│   └── labels/
├── valid/
│   ├── images/
│   └── labels/
├── classes.txt
└── data.yaml
```

### 3. Model Training and Inference

Use the pretrained model yolo26n to train a detection system on your bacterial dataset and run inference on test images.

#### Prerequisites

- **GPU**: NVIDIA GPU recommended
- **Python**: 3.8 or higher
- **YOLO Dataset**: A YOLO-formatted dataset created using Section 2

#### Installation Steps
1. **Set Up Python Environment**
   ```bash
   python -m venv venv
   # On Windows:
   venv\Scripts\activate
   # On macOS/Linux:
   source venv/bin/activate
   ```

2. **Install Dependencies**
   ```bash
   pip install notebook ultralytics torch
   ```

#### Tutorial: Model Training and Inference

1. **Setup Instructions** - Open the notebook, you can use the "Open in Colab" button at the top of [YOLOV26_Proof_Of_Concept.ipynb](JupyterNotebooks/YOLOV26_Proof_Of_Concept.ipynb)

2. **Data Preparation** - Extract the training dataset and install the ultralytics library for YOLO26 training

3. **Model Training** - Use the pretrained model yolo26n to train a detection system on your bacteria dataset for 100 epochs with optimized image size (520px)

4. **Model Validation** - Evaluate the trained model on the test dataset to assess detection accuracy and performance metrics

5. **Inference and Visualization** - Execute inference on test images to generate annotated predictions with detected bacterial objects and confidence scores

6. **Results Archival** - Compress and backup experiment outputs for reproducibility