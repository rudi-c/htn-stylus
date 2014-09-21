using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace InkAnalyzerTest.Processors
{
    public class PipelineAnalyzer
    {
        public event EventHandler PipelineComplete;
        InkAnalyzer inkAnalyzer = new InkAnalyzer();

        List<InkProcessor> inkProcessors = new List<InkProcessor>();
        int processor = 0;
        bool processing = false;

        public PipelineAnalyzer(InkAnalyzer inkAnalyzer)
        {
            this.inkAnalyzer = inkAnalyzer;
            inkAnalyzer.ResultsUpdated += InkAnalyzer_ResultsUpdated;
        }

        public void AddProcessor(InkProcessor processor)
        {
            inkProcessors.Add(processor);
        }

        public void QueueAnalysis()
        {
            lock (this)
            {
                if (!processing)
                {
                    processor = 0;
                    processing = true;

                    inkAnalyzer.BackgroundAnalyze();
                }
            }
        }

        private void InkAnalyzer_ResultsUpdated(object sender, ResultsUpdatedEventArgs e)
        {
            lock (this)
            {
                if (processor < inkProcessors.Count)
                {
                    InkProcessor proc = inkProcessors[processor];
                    proc.process(inkAnalyzer);
                    processor++;
                    inkAnalyzer.DirtyRegion.MakeInfinite();
                    inkAnalyzer.BackgroundAnalyze();
                    return;
                }

                if (adds.Count > 0 || removes.Count > 0)
                {
                    //Flush adds and removes
                    while (adds.Count > 0)
                    {
                        Stroke stroke = adds.Dequeue();
                        inkAnalyzer.AddStroke(stroke);
                    }
                    while (removes.Count > 0)
                    {
                        Stroke stroke = removes.Dequeue();
                        inkAnalyzer.RemoveStroke(stroke);
                    }
                    inkAnalyzer.DirtyRegion.MakeInfinite();
                    inkAnalyzer.BackgroundAnalyze();
                    return;
                }

                if (PipelineComplete != null)
                {
                    PipelineComplete(this, new EventArgs());
                }

                processor = 0;
                processing = false;
            }
        }

        Queue<Stroke> adds = new Queue<Stroke>();
        Queue<Stroke> removes = new Queue<Stroke>();

        public void AddStroke(Stroke stroke)
        {
            lock (this)
            {
                if (processing)
                {
                    adds.Enqueue(stroke);
                }
                else
                {
                    inkAnalyzer.AddStroke(stroke);
                }
            }
        }

        public void RemoveStroke(Stroke stroke)
        {
            lock (this)
            {
                if (processing)
                {
                    removes.Enqueue(stroke);
                }
                else
                {
                    inkAnalyzer.RemoveStroke(stroke);
                }
            }
        }
    }
}
