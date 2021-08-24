sudo mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/"
sudo mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/"
sudo mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/"

sudo wget --output-document="src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow.so" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-libtensorflow.so
sudo wget --output-document="src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow_framework.so" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-libtensorflow_framework.so

sudo wget --output-document="src/VAP/YoloWrapper/Dependencies/opencv_world340.dll" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-opencv_world340.dll
sudo wget --output-document="src/VAP/YoloWrapper/Dependencies/opencv_world340d.dll" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-opencv_world340d.dll
sudo wget --output-document="src/VAP/YoloWrapper/Yolo.Config/YoloV3Coco/yolov3.weights" https://pjreddie.com/media/files/yolov3.weights
sudo wget --output-document="src/VAP/YoloWrapper/Yolo.Config/YoloV3TinyCoco/yolov3-tiny.weights" https://pjreddie.com/media/files/yolov3-tiny.weights
sudo wget --output-document="modelOnnx/yolov3ort.onnx" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-yolov3ort.onnx
sudo wget --output-document="modelOnnx/yolov3tinyort.onnx" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-yolov3tinyort.onnx
