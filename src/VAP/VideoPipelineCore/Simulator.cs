using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoPipelineCore
{
    public class Simulator
    {
        private readonly int slo;
        private int currentTimestamp;
        private readonly int frameInterval;
        private int frameCount;
        private List<int> latencies;

        private int drops;

        private int cumulativeLatency;

        public Simulator(int slo, int frameInterval)
        {
            this.latencies = new List<int>();
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
            while (this.currentTimestamp > ((this.frameCount - 1) * this.frameInterval) + this.slo)
            {
                this.DropFrames();
                Console.WriteLine("Dropping frame since current timestamp is {0} and frame arrival was {1}",
                    this.currentTimestamp, (this.frameCount - 1) * this.frameInterval);
                this.frameCount++;
            }
        }
        
        public int getCurrentTimestamp()
        {
            return this.currentTimestamp;
        }

        public void printStatistics()
        {
            int min = latencies.Min(), max = latencies.Max();
            Console.WriteLine("Processed {0} frames.", this.frameCount);
            Console.WriteLine("Dropped {0} frames.", this.drops);
            Console.WriteLine("Average serving latency was {0} (of non-dropped frames).",
                this.cumulativeLatency / (this.frameCount - this.drops));
            Console.WriteLine("Min latency was {0}.", min);
            Console.WriteLine("Max latency was {0}.", max);
        }
    }
}