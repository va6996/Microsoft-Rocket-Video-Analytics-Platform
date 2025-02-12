// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DNNDetector.Config;
using DNNDetector.Model;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BGSObjectDetector;
using Utils.Config;

namespace TFDetector
{
    public class LineTriggeredDNNTF
    {
        //static string TFCONFIG = "";
        FrameDNNTF frameDNNTF;
        FrameBuffer frameBufferLtDNNTF;
        Dictionary<string, int> counts_prev = new Dictionary<string, int>();
        public static List<List<string>> finalResults = new List<List<string>>();
        public static string modelName = "LineTriggeredDNNTF";

        public LineTriggeredDNNTF(List<(string key, (System.Drawing.Point p1, System.Drawing.Point p2) coordinates)> lines)
        {
            frameBufferLtDNNTF = new FrameBuffer(DNNConfig.FRAME_SEARCH_RANGE);

            frameDNNTF = new FrameDNNTF(lines);
        }

        public List<Item> Run(Mat frame, int frameIndex, Dictionary<string, int> counts, List<(string key, 
            (System.Drawing.Point p1, System.Drawing.Point p2) coordinates)> lines, HashSet<string> category, 
            List<Box> foregroundBoxes)
        {
            // buffer frame
            frameBufferLtDNNTF.Buffer(frame);

            // if (counts_prev.Count != 0)
            // {
                // foreach (string lane in counts.Keys)
                // {
                //     int diff = Math.Abs(counts[lane] - counts_prev[lane]);
                //     if (diff > 0) //object detected by BGS-based counter
                //     {
                        if (frameIndex >= DNNConfig.FRAME_SEARCH_RANGE)
                        {
                            // call tf cheap model for crosscheck
                            // int lineID = Array.IndexOf(counts.Keys.ToArray(), lane);
                            Mat[] frameBufferArray = frameBufferLtDNNTF.ToArray();
                            int frameIndexTF = frameIndex - 1;
                            DateTime start = DateTime.Now;
                            List<Item> analyzedTrackingItems = null;
                            
                            List<string> resString = new List<string>();
                            List<Item> ltDNNItem = new List<Item>();

                            while (frameIndex - frameIndexTF < DNNConfig.FRAME_SEARCH_RANGE)
                            {
                                Console.WriteLine("** Calling Cheap on " + (DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexTF)));
                                Mat frameTF = frameBufferArray[DNNConfig.FRAME_SEARCH_RANGE - (frameIndex - frameIndexTF)];

                                analyzedTrackingItems = frameDNNTF.Run(frameTF, frameIndexTF, category, System.Drawing.Brushes.Pink, DNNConfig.MIN_SCORE_FOR_LINEBBOX_OVERLAP_LARGE, false, foregroundBoxes);

                                // object detected by cheap model
                                if (analyzedTrackingItems != null)
                                {
                                    foreach (Item item in analyzedTrackingItems)
                                    {
                                        item.RawImageData = Utils.Utils.ImageToByteBmp(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frameTF));
                                        item.TriggerLine = "notSet";
                                        item.TriggerLineID = 0;
                                        item.Model = "Cheap";
                                        ltDNNItem.Add(item);
                                        resString.Add(item.ObjName);

                                        // output cheap TF results
                                        string blobName_Cheap = $@"frame-{frameIndex}-Cheap-{item.Confidence}.jpg";
                                        string fileName_Cheap = @OutputFolder.OutputFolderLtDNN + blobName_Cheap;
                                        File.WriteAllBytes(fileName_Cheap, item.TaggedImageData);
                                        File.WriteAllBytes(@OutputFolder.OutputFolderAll + blobName_Cheap, item.TaggedImageData);
                                    }
                                    updateCount(counts);
                                    break;
                                }
                                frameIndexTF--;
                            }
                            finalResults.Add(resString);
                            return ltDNNItem;
                        }
                //     }
                // }
            // }
            
            updateCount(counts);

            return null;
        }

        void updateCount(Dictionary<string, int> counts)
        {
            foreach (string dir in counts.Keys)
            {
                counts_prev[dir] = counts[dir];
            }
        }
    }
}
