using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;

namespace InkAnalyzerTest
{
    public class Headings
    {
        public List<HeadingItem> headings = new List<HeadingItem>();
        public InkCanvas sidebar;

        // The scroll viewer that contains the stuff the header points to.
        public ScrollViewer scrollViewerContainer;

        public void invalidate()
        {
            sidebar.Strokes.Clear();
            double y = 20;
            double x = 20;
            foreach(HeadingItem heading in headings)
            {
                Rect firstText = heading.text[0].Strokes.GetBounds();
                heading.finalBounds = firstText;
                double currentY = y;
                foreach(ContextNode word in heading.text)
                {
                    StrokeCollection strokes = word.Strokes.Clone();
                    InkUtils.transposeStrokes(null, strokes, x-firstText.X, currentY - firstText.Y);
                    sidebar.Strokes.Add(strokes);
                    Rect finalBounds = strokes.GetBounds();
                    if(y < finalBounds.Y + finalBounds.Height)
                    {
                        y = finalBounds.Y + finalBounds.Height;
                    }
                    heading.finalBounds.Union(finalBounds);
                }
                y += 20;
            }
            sidebar.InvalidateVisual();
        }

        public void click(Point location)
        {
            foreach (HeadingItem heading in headings)
            {
                if (heading.finalBounds.Contains(location))
                {
                    foreach (Stroke line in heading.lines)
                    {
                        line.DrawingAttributes.Color = Colors.CornflowerBlue;
                    }

                    scrollViewerContainer.ScrollToVerticalOffset(heading.lines[0].StylusPoints[0].Y);
                } 
            }
        }
    }

    public class HeadingItem
    {
        public List<ContextNode> text = new List<ContextNode>();
        public List<Stroke> lines = new List<Stroke>();
        public Rect finalBounds;

        public bool intersects(Rect bounds)
        {
            foreach (Stroke line in lines)
            {
                if(line.GetBounds().IntersectsWith(bounds))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
