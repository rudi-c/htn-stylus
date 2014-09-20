using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Shapes;

namespace InkAnalyzerTest
{
    public class Headings
    {
        List<Stroke> strokes = new List<Stroke>();
        public void checkAllBlocks(ContextNode node)
        {/*
            foreach(ContextNode cNode in node.SubNodes)
            {
                // Create new tree node corresponding to context node.
                TreeViewItem tNode = new TreeViewItem();
                tNode.Tag = cNode;
                tNode.Header = cNode.ToString();

                if(cNode is InkWordNode)
                {
                    tNode.Header += ": " + (cNode as InkWordNode).GetRecognizedString();
                }
                else if(cNode is InkDrawingNode)
                {
                    tNode.Header += ": " + (cNode as InkDrawingNode).GetShapeName();
                }

                if(cNode.IsConfirmed(ConfirmationType.NodeTypeAndProperties))
                {
                    tNode.Header += "Confirmed.";
                }

                parentTNode.Items.Add(tNode);
                BuildTree(cNode, tNode);
            }
            bool horizontalLine = isStrokeHorizontalLine(stroke);

            bool skip = !horizontalLine;
            if(horizontalLine)
            {
                if(strokes.Count > 0)
                {
                    //Distance check
                    Rect bounds1 = stroke.GetBounds();
                    Rect bounds2 = strokes[0].GetBounds();
                    skip = bounds1.Y - bounds2.Y > 30;
                }

                if(!skip)
                {
                    strokes.Add(stroke);
                }
            }

            if(skip)
            {
                if(strokes.Count > 0)
                {
                    HeadingItem heading = new HeadingItem();
                    heading.strokes = strokes;
                    headings.Add(heading);
                    strokes = new List<Stroke>();
                    Debug.WriteLine(heading.strokes.Count + " underline");
                }
            }*/
        }

        private bool isStrokeHorizontalLine(Stroke stroke)
        {
            Rect bounds = stroke.GetBounds();
            return (bounds.Height / bounds.Width) < 0.1;
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
