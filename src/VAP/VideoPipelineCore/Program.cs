// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AML.Client;
using BGSObjectDetector;
using DarknetDetector;
using DNNDetector;
using DNNDetector.Config;
using DNNDetector.Model;
using LineDetector;
using OpenCvSharp;
using PostProcessor;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using TFDetector;

namespace VideoPipelineCore
{
    public class Result
    {
        public List<double> latency { get; set; } 
        public List<List<string>> object_detection { get; set; }

        
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //parse arguments
            if (args.Length < 4)
            {
                Console.WriteLine(args.Length);
                Console.WriteLine("Usage: <exe> <video url> <cfg file> <samplingFactor> <resolutionFactor> <category1> <category2> ...");
                return;
            }

            string videoUrl = args[0];
            bool isVideoStream;
            if (videoUrl.Substring(0, 4) == "rtmp" || videoUrl.Substring(0, 4) == "http" || videoUrl.Substring(0, 3) == "mms" || videoUrl.Substring(0, 4) == "rtsp")
            {
                isVideoStream = true;
            }
            else
            {
                isVideoStream = false;
                videoUrl = @"media/" + args[0];
            }
            string lineFile = @"cfg/" + args[1];
            Console.WriteLine(args[3]);
            int SAMPLING_FACTOR = int.Parse(args[2]);
            double RESOLUTION_FACTOR = double.Parse(args[3]);

            HashSet<string> category = new HashSet<string>();
            for (int i = 5; i < args.Length; i++)
            {
                category.Add(args[i]);
            }

            //initialize pipeline settings
            string[] stringPplConfigs = args[4].Split(',');
            int[] pplConfigs = new int[stringPplConfigs.Length];
            for (int i=0;i<stringPplConfigs.Length;i++)
            {
                pplConfigs[i] = Convert.ToInt16(stringPplConfigs[i]);
            }
            // int pplConfig = Convert.ToInt16(ConfigurationManager.AppSettings["PplConfig"]);
            bool loop = false;
            bool displayRawVideo = false;
            bool displayBGSVideo = false;
            Utils.Utils.cleanFolderAll();

            //create pipeline components (initialization based on pplConfig)

            //-----Decoder-----
            Decoder.Decoder decoder = new Decoder.Decoder(videoUrl, loop);

            //-----Background Subtraction-based Detector-----
            BGSObjectDetector.BGSObjectDetector bgs = new BGSObjectDetector.BGSObjectDetector();

            //-----Line Detector-----
            Detector lineDetector = new Detector(SAMPLING_FACTOR, RESOLUTION_FACTOR, lineFile, displayBGSVideo);
            Dictionary<string, int> counts = null;
            Dictionary<string, bool> occupancy = null;
            // List<(string key, (System.Drawing.Point p1, System.Drawing.Point p2) coordinates)> lines = lineDetector.multiLaneDetector.getAllLines();
            List<(string key, (System.Drawing.Point p1, System.Drawing.Point p2) coordinates)> lines = null;
            List<Tuple<string, int[]>> convLines = lines == null ? null : Utils.Utils.ConvertLines(lines);
            
            //-----LineTriggeredDNN (Darknet)-----
            LineTriggeredDNNDarknet ltDNNDarknet = null;
            List<Item> ltDNNItemListDarknet = null;
            if (new int[] { 3, 4 }.Intersect(pplConfigs).Any())
            {
                ltDNNDarknet = new LineTriggeredDNNDarknet(lines);
                ltDNNItemListDarknet = new List<Item>();
            }

            //-----LineTriggeredDNN (TensorFlow)-----
            LineTriggeredDNNTF ltDNNTF = null;
            List<Item> ltDNNItemListTF = null;
            if (new int[] { 5,6 }.Intersect(pplConfigs).Any())
            {
                ltDNNTF = new LineTriggeredDNNTF(lines);
                ltDNNItemListTF = new List<Item>();
            }

            //-----LineTriggeredDNN (ONNX)-----
            LineTriggeredDNNORTYolo ltDNNOnnx = null;
            List<Item> ltDNNItemListOnnx = null;
            if (new int[] { 7 }.Intersect(pplConfigs).Any())
            {
                ltDNNOnnx = new LineTriggeredDNNORTYolo(convLines, "yolov3tiny");
                ltDNNItemListOnnx = new List<Item>();
            }

            //-----CascadedDNN (Darknet)-----
            CascadedDNNDarknet ccDNNDarknet = null;
            List<Item> ccDNNItemListDarknet = null;
            if (new int[] { 3 }.Intersect(pplConfigs).Any())
            {
                ccDNNDarknet = new CascadedDNNDarknet(lines);
                ccDNNItemListDarknet = new List<Item>();
            }

            //-----CascadedDNN (ONNX)-----
            CascadedDNNORTYolo ccDNNOnnx = null;
            List<Item> ccDNNItemListOnnx = null;
            if (new int[] { 7 }.Intersect(pplConfigs).Any())
            {
                
                ccDNNOnnx = new CascadedDNNORTYolo(convLines, "yolov3");
                ccDNNItemListOnnx = new List<Item>();
            }

            //-----DNN on every frame (Darknet)-----
            FrameDNNDarknet frameDNNDarknet = null;
            List<Item> frameDNNDarknetItemList = null;
            if (new int[] { 1 }.Intersect(pplConfigs).Any())
            {
                frameDNNDarknet = new FrameDNNDarknet("YoloV3TinyCoco", Wrapper.Yolo.DNNMode.Frame, null);
                frameDNNDarknetItemList = new List<Item>();
            }

            //-----DNN on every frame (TensorFlow)-----
            FrameDNNTF frameDNNTF = null;
            List<Item> frameDNNTFItemList = null;
            if (new int[] { 2 }.Intersect(pplConfigs).Any())
            {
                frameDNNTF = new FrameDNNTF(null);
                frameDNNTFItemList = new List<Item>();
            }

            //-----DNN on every frame (ONNX)-----
            FrameDNNOnnxYolo frameDNNOnnxYolo = null;
            List<Item> frameDNNONNXItemList = null;
            if (new int[] { 8 }.Intersect(pplConfigs).Any())
            {
                frameDNNOnnxYolo = new FrameDNNOnnxYolo(convLines, "yolov3", Wrapper.ORT.DNNMode.Frame);
                frameDNNONNXItemList = new List<Item>();
            }
            
            MaskRCNNOnnx rcnnOnnx = null;
            List<Item> maskRCNNONNXItemList = null;
            if (new int[] { 9 }.Intersect(pplConfigs).Any())
            {
                rcnnOnnx = new MaskRCNNOnnx(convLines, "maskrcnn", Wrapper.ORT.DNNMode.Frame);
            }
            
            FasterRCNNOnnx fasterRcnnOnnx = null;
            List<Item> fasterRCNNONNXItemList = null;
            if (new int[] { 10 }.Intersect(pplConfigs).Any())
            {
                fasterRcnnOnnx = new FasterRCNNOnnx(convLines, "fasterrcnn", Wrapper.ORT.DNNMode.Frame);
            }

            //-----Call ML models deployed on Azure Machine Learning Workspace-----
            AMLCaller amlCaller = null;
            List<bool> amlConfirmed;
            if (new int[] { 6 }.Intersect(pplConfigs).Any())
            {
                amlCaller = new AMLCaller(ConfigurationManager.AppSettings["AMLHost"],
                Convert.ToBoolean(ConfigurationManager.AppSettings["AMLSSL"]),
                ConfigurationManager.AppSettings["AMLAuthKey"],
                ConfigurationManager.AppSettings["AMLServiceID"]);
            }

            //-----Write to DB-----
            List<Item> ItemList = null;

            int frameIndex = 0;
            int videoTotalFrame = 0;
            if (!isVideoStream)
                videoTotalFrame = decoder.getTotalFrameNum() - 1; //skip the last frame which could be wrongly encoded from vlc capture

            long teleCountsBGS = 0, teleCountsCheapDNN = 0, teleCountsHeavyDNN = 0;

            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;
            List<double> latencies = new List<double>();
            int iter = 0;
            while (true)
            {   
                if (!loop)
                {
                    if (!isVideoStream && frameIndex >= videoTotalFrame)
                    {
                        break;
                    }
                }

                //decoder
                Mat frame = decoder.getNextFrame();

                
                //frame pre-processor
                frame = FramePreProcessor.PreProcessor.returnFrame(frame, frameIndex, SAMPLING_FACTOR, RESOLUTION_FACTOR, displayRawVideo);
                frameIndex++;
                if (frame == null) continue;
                //Console.WriteLine("Frame ID: " + frameIndex);


                //background subtractor
                Mat fgmask = null;
                List<Box> foregroundBoxes = bgs.DetectObjects(DateTime.Now, frame, frameIndex, out fgmask);
                

                //line detector
                if (new int[] { 0, 3, 4, 5, 6, 7 }.Intersect(pplConfigs).Any())
                {
                    (counts, occupancy) = lineDetector.updateLineResults(frame, frameIndex, fgmask, foregroundBoxes);
                }


                //cheap DNN
                if (new int[] { 3, 4 }.Intersect(pplConfigs).Any())
                {
                    ltDNNItemListDarknet = ltDNNDarknet.Run(frame, frameIndex, counts, lines, category);
                    ItemList = ltDNNItemListDarknet;
                }
                else if (new int[] { 5, 6 }.Intersect(pplConfigs).Any())
                {
                    ltDNNItemListTF = ltDNNTF.Run(frame, frameIndex, counts, lines, category, foregroundBoxes);
                    ItemList = ltDNNItemListTF;
                }
                else if (new int[] { 7 }.Intersect(pplConfigs).Any())
                {
                    ltDNNItemListOnnx = ltDNNOnnx.Run(frame, frameIndex, counts, convLines, Utils.Utils.CatHashSet2Dict(category), ref teleCountsCheapDNN, true);
                    ItemList = ltDNNItemListOnnx;
                }


                //heavy DNN
                if (new int[] { 3 }.Intersect(pplConfigs).Any())
                {
                    ccDNNItemListDarknet = ccDNNDarknet.Run(frame, frameIndex, ltDNNItemListDarknet, lines, category);
                    ItemList = ccDNNItemListDarknet;
                }
                else if (new int[] { 7 }.Intersect(pplConfigs).Any())
                {
                    ccDNNItemListOnnx = ccDNNOnnx.Run(frameIndex, ItemList, convLines, Utils.Utils.CatHashSet2Dict(category), ref teleCountsHeavyDNN, true);
                    ItemList = ccDNNItemListOnnx;
                }


                //frameDNN with Darknet Yolo
                if (new int[] { 1 }.Intersect(pplConfigs).Any())
                {
                    frameDNNDarknetItemList = frameDNNDarknet.Run(Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame)), frameIndex, lines, category, System.Drawing.Brushes.Pink);
                    ItemList = frameDNNDarknetItemList;
                }


                //frame DNN TF
                if (new int[] { 2 }.Intersect(pplConfigs).Any())
                {
                    frameDNNTFItemList = frameDNNTF.Run(frame, frameIndex, category, System.Drawing.Brushes.Pink, 0.2);
                    ItemList = frameDNNTFItemList;
                }


                //frame DNN ONNX Yolo
                if (new int[] { 8 }.Intersect(pplConfigs).Any())
                {
                    frameDNNONNXItemList = frameDNNOnnxYolo.Run(frame, frameIndex, Utils.Utils.CatHashSet2Dict(category), System.Drawing.Brushes.Pink, 0, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL, true);
                    ItemList = frameDNNONNXItemList;
                }
                
                if (new int[] { 9 }.Intersect(pplConfigs).Any())
                {
                    maskRCNNONNXItemList = rcnnOnnx.Run(frame, frameIndex, Utils.Utils.CatHashSet2Dict(category), System.Drawing.Brushes.Pink, 0, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL, true);
                    ItemList = maskRCNNONNXItemList;
                }
                
                if (new int[] { 10 }.Intersect(pplConfigs).Any())
                {
                    fasterRCNNONNXItemList = fasterRcnnOnnx.Run(frame, frameIndex, Utils.Utils.CatHashSet2Dict(category), System.Drawing.Brushes.Pink, 0, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_SMALL, true);
                    ItemList = fasterRCNNONNXItemList;
                }

                //Azure Machine Learning
                if (new int[] { 6 }.Intersect(pplConfigs).Any())
                {
                    amlConfirmed = AMLCaller.Run(frameIndex, ItemList, category).Result;
                }


                //DB Write
                if (new int[] { 4 }.Intersect(pplConfigs).Any())
                {
                    Position[] dir = { Position.Unknown, Position.Unknown }; // direction detection is not included
                    DataPersistence.PersistResult("test", videoUrl, 0, frameIndex, ItemList, dir, "Cheap", "Heavy", // ArangoDB database
                                                            "test"); // Azure blob
                }


                //display counts
                if (ItemList != null)
                {
                    Dictionary<string, string> kvpairs = new Dictionary<string, string>();
                    foreach (Item it in ItemList)
                    {
                        if (lines != null && !kvpairs.ContainsKey(it.TriggerLine))
                            kvpairs.Add(it.TriggerLine, "1");
                        Console.WriteLine("Detected: {0}", it.ObjName);
                    }
                    FramePreProcessor.FrameDisplay.updateKVPairs(kvpairs);
                }


                //print out stats
                double fps = 1000 * (double)(1) / (DateTime.Now - prevTime).TotalMilliseconds;
                double latency = (DateTime.Now - prevTime).TotalMilliseconds;
                double avgFps = 1000 * (long)frameIndex / latency;
                latencies.Add(latency);
                Console.WriteLine("FrameID: {0} Latency:{1}", frameIndex, latency);
		        prevTime = DateTime.Now;
            }

            string modelName = "";
            List<List<string>> prediction = new List<List<string>>();
            for (int i = 0; i < frameIndex; i++)
            {
                prediction.Add(new List<string>());
            }
            if (new int[] {2}.Intersect(pplConfigs).Any())
            {
                mergePredictions(prediction, FrameDNNTF.finalResults);
                modelName += "_" + FrameDNNTF.modelName;
            }
            if (new int[] {8}.Intersect(pplConfigs).Any())
            {
                mergePredictions(prediction, FrameDNNOnnxYolo.finalResults);
                modelName += "_" +  FrameDNNOnnxYolo.modelName;
            }
            if (new int[] {9}.Intersect(pplConfigs).Any())
            {
                mergePredictions(prediction, MaskRCNNOnnx.finalResults);
                modelName += "_" + MaskRCNNOnnx.modelName;
            }
            if (new int[] {10}.Intersect(pplConfigs).Any())
            {
                mergePredictions(prediction, FasterRCNNOnnx.finalResults);
                modelName += "_" + FasterRCNNOnnx.modelName;
            }
            Result res = new Result
            {
                latency = latencies,
                object_detection = prediction
            };
            Console.WriteLine(res.Serialize());
            string videoName = videoUrl.Split("/").Last().Split(".").First();
            File.WriteAllText(@"benchmarks/rocket" + modelName  + "_" + videoName +".json", res.Serialize());
        }

        static void mergePredictions(List<List<string>> dest, List<List<string>> src)
        {
            for (int i = 0; i < src.Count; i++)
            {
                dest[i].AddRange(src[i]);
            }
        }
    }
}
