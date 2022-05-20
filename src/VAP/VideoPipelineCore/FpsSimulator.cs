using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VideoPipelineCore
{
    public class FpsSimulator
    {
        public int fps { get; set; }
        public int slo { get; set; }
        public int currentTimestamp { get; set; }
        public int processedTime { get; set; }
        public int frameInterval { get; set; }
        public int frameCount { get; set; }
        private Queue<int> queue { get; set; }
        public List<int> nonDroppedLatencies { get; set; }
        public List<int> allLatencies { get; set; }
        public List<int> queueSize { get; set; }
        public List<int> fpsLog { get; set; }
        public string results { get; set; }
        public bool isFrameSkippingEnabled { get; set; }

        public int dropped { get; set; }
        public int processed { get; set; }
        public int skipped { get; set; }
        
        public string modelName { get; set; }
        public string experimentName { get; set; }
        
        public FpsSimulator(int slo, int fps, bool isFrameSkippingEnabled)
        {
            nonDroppedLatencies = new List<int>();
            allLatencies = new List<int>();
            queueSize = new List<int>();
            fpsLog = new List<int>(new int[100]);
            this.isFrameSkippingEnabled = isFrameSkippingEnabled;
            results = "";
            currentTimestamp = 0;
            frameCount = 1;
            dropped = 0;
            skipped = 0;
            processedTime = 0;
            frameInterval = 1000/fps;
            this.slo = slo;
            this.fps = fps;
            queue = new Queue<int>();
        }

        public int GetMovingAvgLatency()
        {
            if (allLatencies.Count < 5)
            {
                return 0;
            }

            return allLatencies.Skip(allLatencies.Count() - 5).Sum() / 5;
        }
        
        public void CalculateFpsAchieved()
        {
            fpsLog[currentTimestamp / 1000] += 1;
        }

        public int GetCurrentTimestamp()
        {
            return currentTimestamp;
        }

        private void UpdateQueueLengths(int latency)
        {
            // Should t=0 queue length be present?
            int tmp = processedTime + frameInterval;
            while (tmp < currentTimestamp + latency)
            {
                queue.Enqueue(tmp);
                
                // Keep dequeuing and skipping/dropping
                while (queue.Peek() + slo < tmp)
                {
                    queue.Dequeue();
                    dropped++;
                }

                while (isFrameSkippingEnabled && queue.Peek() + GetMovingAvgLatency() < tmp)
                {
                    queue.Dequeue();
                    skipped++;
                }

                // At each second update queue lengths
                if (tmp % 1000 == 0)
                {
                    queueSize.Add(queue.Count);
                }
                
                tmp += frameInterval;
            }

            processedTime = tmp - frameInterval;
        }
        
        public void simulateProcessing(int latency)
        {
            // Update queue lengths until new timestamp
            UpdateQueueLengths(latency);

            currentTimestamp += latency;
            allLatencies.Add(latency);

            // Drop current processing frame
            if (currentTimestamp > ((frameCount - 1) * frameInterval) + slo)
            {
                dropped++;
                Console.WriteLine("Dropping frame since current timestamp is {0} and frame slo was {1}",
                    this.currentTimestamp, (this.frameCount - 1) * this.frameInterval + this.slo);
            }
            else
            {
                processed++;
                nonDroppedLatencies.Add(latency);
                CalculateFpsAchieved();
            }

            int tmp = queue.Dequeue();
            frameCount = tmp / frameInterval;
        }
        
        public int getCurrentTimestamp()
        {
            return this.currentTimestamp;
        }

        public void calculateStatistics()
        {
            int min = allLatencies.Min(), max = allLatencies.Max();

            results += $"Processed {this.frameCount} frames.\n";
            results += $"Dropped {this.dropped} frames.\n";
            results += $"Skipped {this.skipped} frames.\n";
            results += $"Average serving latency was {nonDroppedLatencies.Sum() / (frameCount - dropped - skipped)} " +
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