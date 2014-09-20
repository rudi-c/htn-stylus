using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Shapes;

namespace InkAnalyzerTest
{
    public class Headings
    {
        public List<HeadingItem> headings;
        public InkCanvas sidebar;

        public void invalidate()
        {
            sidebar.Strokes.Clear();
            double y = 20;
            double x = 20;
            foreach(HeadingItem heading in headings)
            {
                Rect firstText = heading.text[0].Strokes.GetBounds();
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
                }
                y += 20;
            }
            sidebar.InvalidateVisual();
        }
    }

    public class HeadingItem
    {
        public List<ContextNode> text = new List<ContextNode>();
        public List<ContextNode> lines = new List<ContextNode>();

        public bool intersects(Rect bounds)
        {
            foreach(ContextNode line in lines)
            {
                if(line.Strokes.GetBounds().IntersectsWith(bounds))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
