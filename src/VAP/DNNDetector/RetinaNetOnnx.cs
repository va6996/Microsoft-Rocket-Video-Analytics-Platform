// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NumSharp;
using Utils.Config;
using Wrapper.ORT;

namespace DNNDetector
{
    public class RetinaNetOnnx : OnnxWrapper
    {
        private static int _imageWidth, _imageHeight, _index;
        public static string modelName = "RetinaNet";

        byte[] imageByteArray;

        public RetinaNetOnnx(List<Tuple<string, int[]>> lines, string modelName, DNNMode mode) : base(modelName, mode)
        {
            _lines = lines;
        }

        protected override List<NamedOnnxValue> getContainer(float[] imgData)
        {
            var container = new List<NamedOnnxValue>();
            var tensor1 = new DenseTensor<float>(imgData, new int[] { 1, 3, 480, 640 });
            // Console.WriteLine("tensor value is {0}", string.Join(",", tensor1));
            container.Add(NamedOnnxValue.CreateFromTensor<float>("input", tensor1));
            return container;
        }
        
        public List<Item> Run(Mat frameOnnx, int frameIndex, Dictionary<string, int> category, Brush bboxColor, int lineID, double min_score_for_linebbox_overlap, bool savePictures = false)
        {
            _imageWidth = frameOnnx.Width;
            _imageHeight = frameOnnx.Height;
            _category = category;
            imageByteArray = Utils.Utils.ImageToByteJpeg(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameOnnx)); // Todo: feed in bmp

            List<ORTItem> boundingBoxes = UseApi(
                    OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameOnnx),
                    _imageHeight,
                    _imageWidth);

            List<string> resString = new List<string>();

            List<Item> preValidItems = new List<Item>();
            foreach (ORTItem bbox in boundingBoxes)
            {
                // Console.WriteLine("dims are {0}, {1}, {2}, {3}", bbox.X, bbox.Y, bbox.Width, bbox.Height);
                preValidItems.Add(new Item(bbox));
            }
            Console.Write("# of prevalid items is {0}\n", preValidItems.Count);
            
            List<Item> validObjects = new List<Item>();

            DateTime startTimePP = DateTime.Now;
            //run _category and overlap ratio-based validation
            if (_lines != null)
            {
                var overlapItems = preValidItems.Select(o => new { Overlap = Utils.Utils.checkLineBboxOverlapRatio(_lines[lineID].Item2, o.X, o.Y, o.Width, o.Height), Bbox_x = o.X + o.Width, Bbox_y = o.Y + o.Height, Distance = this.Distance(_lines[lineID].Item2, o.Center()), Item = o })
                    .Where(o => o.Bbox_x <= _imageWidth && o.Bbox_y <= _imageHeight && o.Overlap >= min_score_for_linebbox_overlap && _category.ContainsKey(o.Item.ObjName)).OrderBy(o => o.Distance);
                Console.Write("# of overlap items is {0}\n", overlapItems.Count());

                foreach (var item in overlapItems)
                {
                    item.Item.TaggedImageData = Utils.Utils.DrawImage(imageByteArray, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height, bboxColor);
                    item.Item.CroppedImageData = Utils.Utils.CropImage(imageByteArray, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);
                    item.Item.Index = _index;
                    item.Item.TriggerLine = _lines[lineID].Item1;
                    item.Item.TriggerLineID = lineID;
                    item.Item.Model = modelName;
                    validObjects.Add(item.Item);
                    resString.Add(item.Item.ObjName);
                    _index++;
                }
            }
            else 
            {
                var overlapItems = preValidItems.Select(o => new { Bbox_x = o.X + o.Width, Bbox_y = o.Y + o.Height, Item = o })
                    .Where(o => o.Bbox_x <= _imageWidth && o.Bbox_y <= _imageHeight && _category.ContainsKey(o.Item.ObjName));
                Console.Write("# of overlap items is {0}\n", overlapItems.Count());

                foreach (var item in overlapItems)
                {
                    item.Item.TaggedImageData = Utils.Utils.DrawImage(imageByteArray, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height, bboxColor);
                    item.Item.CroppedImageData = Utils.Utils.CropImage(imageByteArray, item.Item.X, item.Item.Y, item.Item.Width, item.Item.Height);
                    item.Item.Index = _index;
                    item.Item.TriggerLine = "";
                    item.Item.TriggerLineID = -1;
                    item.Item.Model = modelName;
                    validObjects.Add(item.Item);
                    resString.Add(item.Item.ObjName);

                    _index++;
                }
            }

            //output onnxyolo results
            if (savePictures)
            {
                 // foreach (Item it in validObjects)
                 // {
                 //     using (Image image = Image.FromStream(new MemoryStream(it.TaggedImageData)))
                 //     {
                 //
                 //         image.Save(@OutputFolder.OutputFolderMaskRCNNONNX + $"frame-{frameIndex}-ONNX-{it.Confidence}.jpg", ImageFormat.Jpeg);
                 //         image.Save(@OutputFolder.OutputFolderAll + $"frame-{frameIndex}-ONNX-{it.Confidence}.jpg", ImageFormat.Jpeg);
                 //     }
                 // }
                // byte[] imgBboxes = DrawAllBb(frameIndex, Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameOnnx)),
                //         validObjects, Brushes.Pink);
            }
            finalResults.Add(resString);
            DateTime endTimePP = DateTime.Now;
            latencies["post_process"].Add((endTimePP-startTimePP).TotalMilliseconds);
            
            return (validObjects.Count == 0 ? null : validObjects);
        }

        protected double Distance(int[] line, System.Drawing.Point bboxCenter)
        {
            System.Drawing.Point p1 = new System.Drawing.Point((int)((line[0] + line[2]) / 2), (int)((line[1] + line[3]) / 2));
            return Math.Sqrt(this.Pow2(bboxCenter.X - p1.X) + Pow2(bboxCenter.Y - p1.Y));
        }

        protected double Pow2(double x)
        {
            return x * x;
        }

        protected static byte[] DrawAllBb(int frameIndex, byte[] imgByte, List<Item> items, Brush bboxColor)
        {
            byte[] canvas = new byte[imgByte.Length];
            canvas = imgByte;
            foreach (var item in items)
            {
                canvas = Utils.Utils.DrawImage(canvas, item.X, item.Y, item.Width, item.Height, bboxColor);
            }
            string frameIndexString = frameIndex.ToString("000000.##");
            File.WriteAllBytes(@OutputFolder.OutputFolderMaskRCNNONNX + $@"frame{frameIndexString}-Raw.jpg", canvas);

            return canvas;
        }
        
        public List<int> ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => new List<int>{ v, t, p},
                1 => new List<int>{q, v, p},
                2 => new List<int>{ p, v, t},
                3 => new List<int>{ p, q, v},
                4 => new List<int>{ t, p, v},
                _ => new List<int>{v, p, q}
            };
        }

        public List<int> getPalette()
        {
            List<int> palette = new List<int>();
            palette.Add(0);
            palette.Add(0);
            palette.Add(0);

            var ybc = new Yolov3BaseConfig();
            for (int i = 3; i < ybc.Labels.Length; i++)
            {
                List<int> color = ColorFromHSV((i*1.0 / ybc.Labels.Length), 0.75, 0.75);
                for (int j = 0; j < 3; j++)
                {
                    palette.Add(color[j]*255);
                }
            }

            return palette;
        }
        
        protected override List<ORTItem> PostProcessing(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
        {
            List<ORTItem> itemList = new List<ORTItem>();

            var output = results.AsEnumerable().ElementAt(0).AsTensor<float>();

            // Console.WriteLine("dimensions are {0}", string.Join(",", output.Dimensions.ToArray()));

            for (int i = 0; i < 640; i++)
            {
                for (int j = 0; j < 480; j++)
                {
                    float max = 0;
                    int idx = -1;
                    
                    for (int k = 1; k < 21; k++)
                    {
                        if (max < output[0,k, j, i])
                        {
                            max = output[0, k, j, i];
                            idx = k;
                        }
                    }

                    if (idx != 0)
                    {
                        ORTItem item = new ORTItem(i, j, 1,1, idx, cfg.Labels[idx], output[0,idx, j, i]);
                        itemList.Add(item);
                    }
                    
                }
            }

            return itemList;
            
        }

        protected override float[] LoadTensorFromImageFile(Bitmap bitmap)
        {
            // RGBtoBGR(bitmap);
            int iw = bitmap.Width, ih = bitmap.Height, w = 640, h = 480, nw, nh;

            float scale = Math.Min((float)w/iw, (float)h/ih);
            nw = (int)(iw * scale);
            nh = (int)(ih * scale);

            //resize
            Bitmap rsImg = ResizeImage(bitmap, nw, nh);
            Bitmap boxedImg = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (Graphics gr = Graphics.FromImage(boxedImg))
            {
                gr.FillRectangle(new SolidBrush(Color.FromArgb(255, 128, 128, 128)), 0, 0, boxedImg.Width, boxedImg.Height);
                gr.DrawImage(rsImg, new System.Drawing.Point((int)((w - nw) / 2), (int)((h - nh) / 2)));
            }
            var imgData = boxedImg.ToNDArray(flat: false, copy: true);

            var means = new double[] {0.485, 0.456, 0.406};
            var npMeans = np.array(means);

            var std = new double[] {0.229, 0.224, 0.225};
            var npStd = np.array(std);
            
            imgData /= 255.0;
            imgData = np.transpose(imgData, new int[] { 0, 3, 1, 2 });
            // Console.WriteLine("Shape is {0}", string.Join(",", imgData.shape));
            foreach (var i in new int[]{0,1,2})
            {
                imgData[0, i] = (imgData[0, i] - npMeans[i]) / npStd[i];
            }
            // Console.WriteLine("Shape is {0}", string.Join(",", imgData[0]));

            imgData = imgData.reshape(1, 3 * w * h);

            
            double[] doubleArray = imgData[0].ToArray<double>();
            float[] floatArray = new float[doubleArray.Length];
            for (int i = 0; i < doubleArray.Length; i++)
            {
                floatArray[i] = (float)doubleArray[i];
            }

            return floatArray;
        }
    }
}
