using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;

namespace InkAnalyzerTest.Processors
{
    public class PipelineAnalyzer
    {
        public static PipelineAnalyzer analyzer;

        public event EventHandler PipelineComplete;
        InkAnalyzer inkAnalyzer = new InkAnalyzer();

        List<InkProcessor> inkProcessors = new List<InkProcessor>();
        int processor = 0;
        bool processing = false;
        bool running = false;
        bool queued = false;

        public PipelineAnalyzer(InkAnalyzer inkAnalyzer)
        {
            analyzer = this;
            this.inkAnalyzer = inkAnalyzer;
            inkAnalyzer.ResultsUpdated += InkAnalyzer_ResultsUpdated;
        }

        public void AddProcessor(InkProcessor processor)
        {
            inkProcessors.Add(processor);
        }

        public void QueueAnalysis()
        {
            if (!processing)
            {
                processor = 0;
                processing = true;
                fireoff();
            }
            else
            {
                queued = true;
            }
        }

        private void InkAnalyzer_ResultsUpdated(object sender, ResultsUpdatedEventArgs e)
        {
            running = false;

            PostAnalyze();

            if (processor < inkProcessors.Count)
            {
                InkProcessor proc = inkProcessors[processor];
                proc.process(inkAnalyzer);
                processor++;
                fireoff();
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
                fireoff();
                return;
            }

            if (PipelineComplete != null)
            {
                PipelineComplete(this, new EventArgs());
            }

            processor = 0;
            processing = false;

            if (queued)
            {
                queued = false;
                processor = 0;
                processing = true;
                fireoff();
            }
        }

        private void fireoff()
        {
            running = true;
            if (!inkAnalyzer.BackgroundAnalyze())
            {
                InkAnalyzer_ResultsUpdated(null, null);
            }
        }

        Queue<Stroke> adds = new Queue<Stroke>();
        Queue<Stroke> removes = new Queue<Stroke>();

        public void AddStroke(Stroke stroke)
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

        public void RemoveStroke(Stroke stroke)
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

        private void PostAnalyze()
        {
            foreach (ContextNode writingRegion in inkAnalyzer.RootNode.SubNodes)
            {
                List<ParagraphAnalysisEntry> paragraphs = new List<ParagraphAnalysisEntry>();
                foreach (ContextNode node in writingRegion.SubNodes)
                {
                    if (node is ParagraphNode)
                    {
                        ParagraphNode paragraph = node as ParagraphNode;
                        ContextNode firstLine = paragraph.SubNodes[0];
                        Point paragraphReference = firstLine.Strokes.GetBounds().TopLeft;
                        ParagraphAnalysisEntry entry = new ParagraphAnalysisEntry();
                        entry.paragraph = paragraph;
                        entry.point = paragraphReference;
                        paragraphs.Add(entry);
                    }
                }

                paragraphs.Sort(delegate(ParagraphAnalysisEntry a, ParagraphAnalysisEntry b)
                {
                    return a.point.Y.CompareTo(b.point.Y);
                });

                for (int i = 0; i < paragraphs.Count - 1; i++)
                {
                    ParagraphAnalysisEntry entry = paragraphs[i];
                    ParagraphAnalysisEntry next = paragraphs[i + 1];
                    bool closeto = entry.closeTo(next);
                    if (closeto)
                    {
                        foreach (ContextNode node in next.paragraph.SubNodes)
                        {
                            node.Reparent(entry.paragraph);
                        }
                        writingRegion.DeleteSubNode(next.paragraph);
                        paragraphs[i + 1] = entry;
                    }
                }
            }
        }
    }

    public class ParagraphAnalysisEntry
    {
        public Point point;
        public ParagraphNode paragraph;

        public bool closeTo(ParagraphAnalysisEntry other)
        {
            double dy = other.point.Y - point.Y - paragraph.Strokes.GetBounds().Height;
            double dx = Math.Abs(other.point.X - point.X);
            return dx < 30 && dy < 40;
        }
    }
}
