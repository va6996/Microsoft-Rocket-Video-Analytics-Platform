// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Wrapper.ORT
{
    public abstract class OnnxWrapper
    {
        protected IYoloConfiguration cfg;
        InferenceSession session1, session2;
        DNNMode mode = DNNMode.Unknown;
        protected static List<Tuple<string, int[]>> _lines;
        protected static Dictionary<string, int> _category;
        public static List<List<string>> finalResults = new List<List<string>>();
        public static Dictionary<string, List<double>> latencies = new Dictionary<string, List<double>>();

        public OnnxWrapper(string modelPath, DNNMode mode)
        {
            latencies["model"] = new List<double>();
            
            string actualPath = $@"modelOnnx/{modelPath}ort.onnx";
            // Optional : Create session options and set the graph optimization level for the session
            SessionOptions options = new SessionOptions();
            //options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED;
            cfg = new Yolov3BaseConfig();

            this.mode = mode;
            switch (mode)
            {
                case DNNMode.LT:
                case DNNMode.Frame:
                    session1 = new InferenceSession(actualPath, SessionOptions.MakeSessionOptionWithCudaProvider(0));
                    break;
                case DNNMode.CC:
                    session2 = new InferenceSession(actualPath, SessionOptions.MakeSessionOptionWithCudaProvider(0));
                    break;
            }
        }

        public List<ORTItem> UseApi(Bitmap bitmap, int h, int w)
        {
            float[] imgData = LoadTensorFromImageFile(bitmap);
            var container = getContainer(imgData);
            List<ORTItem> itemList;

            // Run the inference
            DateTime startTime = DateTime.Now;
            switch (mode)
            {
                case DNNMode.LT:
                case DNNMode.Frame:
                    using (var results = session1.Run(container))  // results is an IDisposableReadOnlyCollection<DisposableNamedOnnxValue> container
                    {
                        itemList =  PostProcessing(results);
                        break;
                    }
                case DNNMode.CC:
                    using (var results = session2.Run(container))  // results is an IDisposableReadOnlyCollection<DisposableNamedOnnxValue> container
                    {
                        itemList = PostProcessing(results);
                        break;
                    }
                default:
                    {
                        itemList = null;
                        break;
                    }
            }
            DateTime endTime = DateTime.Now;
            latencies["model"].Add((endTime-startTime).TotalMilliseconds);

            return itemList;
        }

        protected abstract List<NamedOnnxValue> getContainer(float[] imgData);
        protected abstract List<ORTItem> PostProcessing(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results);
        protected abstract float[] LoadTensorFromImageFile(Bitmap bitmap);
        
        public float[] LoadTensorFromPreProcessedFile(string filename)
        {
            var tensorData = new List<float>();

            // read data from file
            using (var inputFile = new StreamReader(filename))
            {
                List<string> dataStrList = new List<string>();
                string line;
                while ((line = inputFile.ReadLine()) != null)
                {
                    dataStrList.AddRange(line.Split(new char[] { ',', '[', ']' }, StringSplitOptions.RemoveEmptyEntries));
                }

                string[] dataStr = dataStrList.ToArray();
                for (int i = 0; i < dataStr.Length; i++)
                {
                    tensorData.Add(Single.Parse(dataStr[i]));
                }
            }

            return tensorData.ToArray();
        }

        

        protected void RGBtoBGR(Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                           ImageLockMode.ReadWrite, bmp.PixelFormat);

            int length = Math.Abs(data.Stride) * bmp.Height;

            byte[] imageBytes = new byte[length];
            IntPtr scan0 = data.Scan0;
            Marshal.Copy(scan0, imageBytes, 0, imageBytes.Length);

            byte[] rgbValues = new byte[length];
            for (int i = 0; i < length; i += 3)
            {
                rgbValues[i] = imageBytes[i + 2];
                rgbValues[i + 1] = imageBytes[i + 1];
                rgbValues[i + 2] = imageBytes[i];
            }
            Marshal.Copy(rgbValues, 0, scan0, length);

            bmp.UnlockBits(data);
        }

        public void DrawBoundingBox(Image imageOri,
            string outputImageLocation,
            string imageName,
            List<ORTItem> itemList)
        {
            Image image = (Image)imageOri.Clone();

            foreach (var item in itemList)
            {
                var x = Math.Max(item.X, 0);
                var y = Math.Max(item.Y, 0);
                var width = item.Width;
                var height = item.Height;
                string text = $"{item.ObjName} ({(item.Confidence * 100).ToString("0")}%)";
                using (Graphics thumbnailGraphic = Graphics.FromImage(image))
                {
                    thumbnailGraphic.CompositingQuality = CompositingQuality.HighQuality;
                    thumbnailGraphic.SmoothingMode = SmoothingMode.HighQuality;
                    thumbnailGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    Font drawFont = new Font("Arial", 12, FontStyle.Bold);
                    SizeF size = thumbnailGraphic.MeasureString(text, drawFont);
                    SolidBrush fontBrush = new SolidBrush(Color.Black);
                    Point atPoint = new Point((int)(x + width / 2), (int)(y + height / 2) - (int)size.Height - 1);

                    // Define BoundingBox options
                    Pen pen = new Pen(Color.Pink, 3.2f);
                    SolidBrush colorBrush = new SolidBrush(Color.Pink);

                    thumbnailGraphic.FillRectangle(colorBrush, (int)(x + width / 2), (int)(y + height / 2 - size.Height - 1), size.Width, (int)size.Height);
                    thumbnailGraphic.DrawString(text, drawFont, fontBrush, atPoint);

                    // Draw bounding box on image
                    thumbnailGraphic.DrawRectangle(pen, x, y, width, height);
                    if (!Directory.Exists(outputImageLocation))
                    {
                        Directory.CreateDirectory(outputImageLocation);
                    }

                    image.Save(Path.Combine(outputImageLocation, imageName));
                }
            }
        }

        protected Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(96, 96); //@todo: image.HorizontalResolution throw exceptions during docker build on linux
            //destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        
    }
}
