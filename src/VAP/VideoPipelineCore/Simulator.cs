using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VideoPipelineCore
{
    public class Simulator
    {
        private readonly int slo;
        private int currentTimestamp;
        private readonly int frameInterval;
        private int frameCount;
        private List<int> latencies;
        public string results;

        private int drops;

        private int cumulativeLatency;

        public Simulator(int slo, int frameInterval)
        {
            this.latencies = new List<int>();
            results = "";
            this.currentTimestamp = 0;
            this.cumulativeLatency = 0;
            this.frameCount = 1;
            this.drops = 0;
            this.frameInterval = frameInterval;
            this.slo = slo;
        }

        void DropFrames()
        {
            drops++;
        }

        public void simulateProcessing(int latency)
        {
            this.currentTimestamp += latency;
            latencies.Add(latency);
            
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
                this.cumulativeLatency += latency;
            }

            this.frameCount++;

            // Drop if there is a significant spillover
            while (this.currentTimestamp > ((this.frameCount) * this.frameInterval))
            {
                this.DropFrames();
                Console.WriteLine("Dropping frame since current timestamp is {0} and next frame arrival was {1}",
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
            int min = latencies.Min(), max = latencies.Max();

            results += $"Processed {this.frameCount} frames.\n";
            results += $"Dropped {this.drops} frames.\n";
            results += $"Average serving latency was {this.cumulativeLatency / (this.frameCount - this.drops)} " +
                       $"(of non-dropped frames).\n";
            results += $"Min latency was {min}.\n";
            results += $"Max latency was {max}.\n";
        }
        
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}