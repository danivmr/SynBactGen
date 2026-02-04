
![Logo](Resources/Logo/logo_text.png)


# Bacteria Dataset Generator


Simulating realistic microbiology data is a challenge due to the need for specialized equipment such as microscopes, cameras, and bacterial cultures—as well as domain expertise in bacteriology. This project proposes a solution by generating synthetic datasets using a video game engine that simulates bacterial colonies.

The system creates synthetic images alongside automated annotations. Each bacterium's position is marked with a 2D bounding box, generating data compatible with modern object detection frameworks. The dataset's effectiveness is evaluated using the YOLO (You Only Look Once) object detection algorithm.


## Tutorials

## 1. Unity Setup

### Running the Project and Generating Datasets

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


## 2. Dataset Format and Conversion

**Prerequisites:** Download a pre-generated dataset (link) or generate a new dataset following the Unity Setup section above.

### Dataset Structure

The generated dataset has the following structure:

- **Sequence folders**
  - `Step0.camera.png`
  - `step0.frame_data.json`
- `annotation_definition.json`
- `metadata.json`
- `metric_definitions.json`
- `sensor_definitions.json`

The files within the sequence folders and `annotation_definition.json` are used to convert the dataset to YOLO format.

### Converting to YOLO Format

Use the provided Jupyter notebook to convert the dataset format. Follow the instructions in `JupyterNotebooks/ConvertFormatToYOLO.ipynb`.

## 3. Model Training

Refer to the Jupyter notebook in the `JupyterNotebooks/` folder for training instructions...