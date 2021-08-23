// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using AML.Client;
using BGSObjectDetector;
using DNNDetector.Model;
using LineDetector;
using OpenCvSharp;
using PostProcessor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using FramePreProcessor;
using TFDetector;
using Utils.Config;

namespace VideoPipelineCore
{
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
                videoUrl = @"./media/" + args[0];
            }
            string lineFile = @"./cfg/" + args[1];
            int SAMPLING_FACTOR = int.Parse(args[2]);
            double RESOLUTION_FACTOR = double.Parse(args[3]);

            HashSet<string> category = new HashSet<string>();
            for (int i = 4; i < args.Length; i++)
            {
                category.Add(args[i]);
            }
            //initialize pipeline settings
            int pplConfig = Convert.ToInt16(ConfigurationManager.AppSettings["PplConfig"]);
            bool loop = false;
            bool displayRawVideo = false;
            bool displayBGSVideo = false;
            Utils.Utils.cleanFolder(@OutputFolder.OutputFolderAll);
            //create pipeline components (initialization based on pplConfig)

            //-----Decoder-----
            Decoder.Decoder decoder = new Decoder.Decoder(videoUrl, loop);

            //-----Background Subtraction-based Detector-----
            BGSObjectDetector.BGSObjectDetector bgs = new BGSObjectDetector.BGSObjectDetector();

            //-----Line Detector-----
            Detector lineDetector = new Detector(SAMPLING_FACTOR, RESOLUTION_FACTOR, lineFile, displayBGSVideo);
            Dictionary<string, bool> occupancy = null;
            List<Tuple<string, int[]>> lines = lineDetector.multiLaneDetector.getAllLines();

            //-----LineTriggeredDNN (TensorFlow)-----
            LineTriggeredDNNTF ltDNNTF = null;
            List<Item> ltDNNItemListTF = null;
            if (new int[] { 5, 6 }.Contains(pplConfig))
            {
                ltDNNTF = new LineTriggeredDNNTF(lines);
                ltDNNItemListTF = new List<Item>();
            }

            //-----DNN on every frame (TensorFlow)-----
            FrameDNNTF frameDNNTF = null;
            List<Item> frameDNNTFItemList = null;
            if (new int[] { 2 }.Contains(pplConfig))
            {
                frameDNNTF = new FrameDNNTF(lines);
                frameDNNTFItemList = new List<Item>();
                Utils.Utils.cleanFolder(@OutputFolder.OutputFolderFrameDNNTF);
            }

            //-----Call ML models deployed on Azure Machine Learning Workspace-----
            AMLCaller amlCaller = null;
            List<bool> amlConfirmed;
            if (new int[] { 5 }.Contains(pplConfig))
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


            //RUN PIPELINE 
            DateTime startTime = DateTime.Now;
            DateTime prevTime = DateTime.Now;
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
                if (foregroundBoxes != null && foregroundBoxes.Count > 0)
                {
                    Console.WriteLine("boxes deteceted: {0}", foregroundBoxes.Count);
                }
                

                //line detector
                if (new int[] { 0, 4, 5, 6 }.Contains(pplConfig))
                {
                    occupancy = lineDetector.updateLineOccupancy(frame, frameIndex, fgmask, foregroundBoxes);
                    foreach (var kv in occupancy)
                    {
                        // Console.WriteLine("Line Detector: Key {0} Value {1}", kv.Key, kv.Value);
                    }
                }


                //cheap DNN
                if (new int[] { 4, 5, 6 }.Contains(pplConfig))
                {
                    ltDNNItemListTF = ltDNNTF.Run(frame, frameIndex, occupancy, lines, category);
                    ItemList = ltDNNItemListTF;
                    if (ltDNNItemListTF != null)
                    {
                        // foreach (var kv in ltDNNItemListTF)
                        // {
                        //     if (kv != null)
                        //     {
                                Console.WriteLine("cheap DNN: Key {0}", ltDNNItemListTF.Count);
                        //     }
                        //
                        // }
                    }
                    
                }


                //frame DNN TF
                if (new int[] { 2 }.Contains(pplConfig))
                {
                    frameDNNTFItemList = frameDNNTF.Run(frame, frameIndex, category, System.Drawing.Brushes.Pink, 0.2);
                    ItemList = frameDNNTFItemList;
                    if (frameDNNTFItemList != null)
                    {
                        Console.WriteLine("frame DNN: Key {0}", frameDNNTFItemList.Count);
                    }
                }


                //Azure Machine Learning
                if (new int[] { 5 }.Contains(pplConfig))
                {
                    amlConfirmed = AMLCaller.Run(frameIndex, ItemList, category).Result;
                }


                //DB Write
                if (new int[] { 4 }.Contains(pplConfig))
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
                        Console.WriteLine("Found {0}", it.TriggerLine);
                        if (!kvpairs.ContainsKey(it.TriggerLine))
                            kvpairs.Add(it.TriggerLine, "1");
                    }
                    FramePreProcessor.FrameDisplay.updateKVPairs(kvpairs);
                }


                //print out stats
                double fps = 1000 * (double)(1) / (DateTime.Now - prevTime).TotalMilliseconds;
                double avgFps = 1000 * (long)frameIndex / (DateTime.Now - startTime).TotalMilliseconds;
                Console.WriteLine("FrameID: {0} Latency:{1}", frameIndex, (DateTime.Now - prevTime).TotalMilliseconds);
		        prevTime = DateTime.Now;
            }

            foreach (var pair in FramePreProcessor.FrameDisplay.displayKVpairs)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            // Console.WriteLine("{0}", FramePreProcessor.FrameDisplay.displayKVpairs.ToString());
            Console.WriteLine("Done!");
        }
    }
}
