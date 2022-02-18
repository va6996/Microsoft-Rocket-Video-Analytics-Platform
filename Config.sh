mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/"
mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/"
mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/"
mkdir "src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/win7-x64/native/"

wget --output-document="src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow.so" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-libtensorflow.so
cp src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow.so src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/liblibtensorflow.so

wget --output-document="src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow_framework.so" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-libtensorflow_framework.so
cp src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow_framework.so src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/linux/native/libtensorflow_framework.so.1

wget --output-document="src/VAP/TFWrapper/packages/TensorFlowSharp.1.12.0/runtimes/win7-x64/native/libtensorflow.dll" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-libtensorflow.dll

wget --output-document="src/VAP/YoloWrapper/Dependencies/opencv_world340.dll" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-opencv_world340.dll
wget --output-document="src/VAP/YoloWrapper/Dependencies/opencv_world340d.dll" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-opencv_world340d.dll
wget --output-document="src/VAP/YoloWrapper/Yolo.Config/YoloV3Coco/yolov3.weights" https://pjreddie.com/media/files/yolov3.weights
wget --output-document="src/VAP/YoloWrapper/Yolo.Config/YoloV3TinyCoco/yolov3-tiny.weights" https://pjreddie.com/media/files/yolov3-tiny.weights
wget --output-document="modelOnnx/yolov3ort.onnx" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-yolov3ort.onnx
wget --output-document="modelOnnx/yolov3tinyort.onnx" https://aka.ms/Microsoft-Rocket-Video-Analytics-Platform-yolov3tinyort.onnx
wget --output-document="modelOnnx/maskrcnnort.onnx" https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/mask-rcnn/model/MaskRCNN-10.onnx
wget --output-document="modelOnnx/fasterrcnnort.onnx" https://github.com/onnx/models/raw/main/vision/object_detection_segmentation/faster-rcnn/model/FasterRCNN-10.onnx
