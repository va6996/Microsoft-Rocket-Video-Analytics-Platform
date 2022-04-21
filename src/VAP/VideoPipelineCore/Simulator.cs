using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VideoPipelineCore
{
    public class Simulator
    {
        private int slo { get; set; }
        private int currentTimestamp { get; set; }
        private int frameInterval { get; set; }
        private int frameCount { get; set; }
        private List<int> nonDroppedLatencies { get; set; }
        private List<int> allLatencies { get; set; }
        private List<int> queueSize { get; set; }
        public string results { get; set; }
        private bool isFrameSkippingEnabled { get; set; }

        private int drops { get; set; }
        private int skips { get; set; }
        
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
            queueSize.Add((currentTimestamp/frameInterval - frameCount));

            // Drop current processing frame
            if (this.currentTimestamp > ((this.frameCount - 1) * this.frameInterval) + this.slo)
            {
                this.DropFrames();
                Console.WriteLine("Dropping frame since current timestamp is {0} and frame arrival was {1}",
                    this.currentTimestamp, (this.frameCount - 1) * this.frameInterval);
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
                       $"of non-dropped/skipped frame.\n ";
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