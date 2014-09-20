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
}
