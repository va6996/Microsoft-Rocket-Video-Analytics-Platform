using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VideoPipelineCore
{
    public class Simulator
    {
        public int slo { get; set; }
        public int currentTimestamp { get; set; }
        public int frameInterval { get; set; }
        public int frameCount { get; set; }
        public List<int> nonDroppedLatencies { get; set; }
        public List<int> allLatencies { get; set; }
        public List<int> queueSize { get; set; }
        public string results { get; set; }
        public bool isFrameSkippingEnabled { get; set; }

        public int drops { get; set; }
        public int skips { get; set; }
        
        public string modelName { get; set; }
        public string experimentName { get; set; }
        
        public Simulator(int slo, int frameInterval, bool isFrameSkippingEnabled)
        {
            this.nonDroppedLatencies = new List<int>();
            this.allLatencies = new List<int>();
            this.queueSize = new List<int>();
            this.isFrameSkippingEnabled = isFrameSkippingEnabled;
            results = "";
            this.currentTimestamp = 0;
            this.frameCount = 1;
            this.drops = 0;
            this.skips = 0;
            this.frameInterval = frameInterval;
            this.slo = slo;
        }

        void DropFrames()
        {
            drops++;
        }
        
        void SkipFrames()
        {
            skips++;
        }

        public void simulateProcessing(int latency)
        {
            this.currentTimestamp += latency;
            allLatencies.Add(latency);
            queueSize.Add((currentTimestamp/frameInterval - frameCount + 1));

            // Drop current processing frame
            if (this.currentTimestamp > ((this.frameCount - 1) * this.frameInterval) + this.slo)
            {
                this.DropFrames();
                Console.WriteLine("Dropping frame since current timestamp is {0} and frame slo was {1}",
                    this.currentTimestamp, (this.frameCount - 1) * this.frameInterval + this.slo);
            }
            else 
            {
                this.currentTimestamp = this.frameCount * this.frameInterval;
                nonDroppedLatencies.Add(latency);
            }

            this.frameCount++;

            // Drop if there is a significant spillover
            while (isFrameSkippingEnabled && this.currentTimestamp > ((this.frameCount) * this.frameInterval))
            {
                this.SkipFrames();
                Console.WriteLine("Skipping frame since current timestamp is {0} and next frame arrival was {1}",
                    this.currentTimestamp, (this.frameCount) * this.frameInterval);
                this.frameCount++;
            }
        }
        
        public int getCurrentTimestamp()
        {
            return this.currentTimestamp;
        }

        public void calculateStatistics()
        {
            int min = nonDroppedLatencies.Min(), max = nonDroppedLatencies.Max();

            results += $"Processed {this.frameCount} frames.\n";
            results += $"Dropped {this.drops} frames.\n";
            results += $"Skipped {this.skips} frames.\n";
            results += $"Average serving latency was {nonDroppedLatencies.Sum() / (frameCount - drops - skips)} " +
                       $"of non-dropped/skipped frame.\n";
            results += $"Average latency across all frames is {allLatencies.Sum() / allLatencies.Count}.\n";
            results += $"Min latency was {min}.\n";
            results += $"Max latency was {max}.\n";
            results += $"Queue lengths at end of processing each frame were as follows:\n" +
                       $"[{string.Join(", ", queueSize)}].\n";
        }
        
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}