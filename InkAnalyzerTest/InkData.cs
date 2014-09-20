using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkAnalyzerTest
{
    [DataContract]
    public class DataStylusPoint
    {
        [DataMember]
        double x;
        [DataMember]
        double y;
        [DataMember]
        float pressure;

        public StylusPoint Representation()
        {
            return new StylusPoint(x, y, pressure);
        }

        public DataStylusPoint(StylusPoint point)
        {
            x = point.X;
            y = point.Y;
            pressure = point.PressureFactor;
        }
    }

    [DataContract]
    public class DataStylusStroke
    {
        [DataMember]
        DataStylusPoint[] stylusPoints;

        public Stroke Representation()
        {
            StylusPointCollection collection = new StylusPointCollection();
            foreach (var data in stylusPoints)
                collection.Add(data.Representation());
            return new Stroke(collection);
        }

        public DataStylusStroke(Stroke stroke)
        {
            stylusPoints = new DataStylusPoint[stroke.StylusPoints.Count];
            for (int i = 0; i < stylusPoints.Length; i++)
                stylusPoints[i] = new DataStylusPoint(stroke.StylusPoints[i]);
        }
    }

    [DataContract]
    public class DataStylusToken
    {
        [DataMember]
        DataStylusStroke[] stylusStrokes;

        public StrokeCollection Representation()
        {
            StrokeCollection strokes = new StrokeCollection();
            foreach (var data in stylusStrokes)
                strokes.Add(data.Representation());
            return strokes;
        }

        public DataStylusToken(StrokeCollection strokes)
        {
            stylusStrokes = new DataStylusStroke[strokes.Count];
            for (int i = 0; i < stylusStrokes.Length; i++)
                stylusStrokes[i] = new DataStylusStroke(strokes[i]);
        }
    }

    public class StylusToken
    {
        public StrokeCollection strokes;
        public double width;
        public double height;

        public void Normalize()
        {
            // Normalize to the left edge.
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            foreach (Stroke stroke in strokes)
            {
                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    minX = Math.Min(minX, point.X);
                    maxX = Math.Max(maxX, point.X);
                }
            }
            foreach (Stroke stroke in strokes)
                for (int i = 0; i < stroke.StylusPoints.Count; i++)
                    // Struct type can't be used as foreach.
                    stroke.StylusPoints[i] = new StylusPoint(
                        stroke.StylusPoints[i].X - minX,
                        stroke.StylusPoints[i].Y);

            width = maxX - minX;
        }

        public StylusToken(StrokeCollection _strokes)
        {
            width = 0.0;
            strokes = _strokes;
        }
    }
}
