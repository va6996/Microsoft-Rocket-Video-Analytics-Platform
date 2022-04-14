using System;

namespace VideoPipelineCore
{
    public class Simulator
    {
        private readonly int slo;
        private int currentTimestamp;
        private readonly int frameInterval;
        private int frameCount;

        private int drops;

        private int cumulativeLatency;

        public Simulator(int slo, int frameInterval)
        {
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
            // Drop current processing frame
            if (this.currentTimestamp > ((this.frameCount - 1) * this.frameInterval) + this.slo)
            {
                this.DropFrames();
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
                this.frameCount++;
            }
        }

        public void printStatistics()
        {
            Console.WriteLine("Processed {0} frames.", this.frameCount);
            Console.WriteLine("Dropped {0} frames.", this.drops);
            Console.WriteLine("Average serving latency was {0} (of non-dropped frames).",
                this.cumulativeLatency / (this.frameCount - this.drops));
        }
    }
}