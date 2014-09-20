using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Ink;

namespace InkAnalyzerTest
{
    /// <summary>
    /// Interaction logic for SuggestionsBox.xaml
    /// </summary>
    public partial class SuggestionsBox : UserControl
    {
        private InkWordNode incorrectWord;

        public SuggestionsBox(InkWordNode _incorrectWord, List<string> suggestions,
            Dictionary<char, StylusToken> fontData)
        {
            InitializeComponent();

            incorrectWord = _incorrectWord;

            var midline = incorrectWord.GetMidline();
            var baseline = incorrectWord.GetBaseline();

            // Assume that the midline and baseline are horizontal lines
            // i.e. two points, same y coordinate.
            double wordSize = midline[0].Y - baseline[0].Y;
            
            // Keep the top 3 suggestions for now.
            for (int i = 0; i < Math.Min(3, suggestions.Count); i++)
            {

            }
        }

        // Generates a collection of strokes representing an entire word.
        StrokeCollection GetStrokesForString(string text,
            Dictionary<char, StylusToken> fontData)
        {
            double currentX = 0.0;

            StrokeCollection stringStrokes = new StrokeCollection();

            foreach (char c in text)
            {
                if (fontData.Keys.Contains(c))
                {
                    StylusToken token = fontData[c];

                    foreach (Stroke stroke in token.strokes)
                    {
                        StylusPointCollection newPoints = new StylusPointCollection();
                        foreach (StylusPoint point in stroke.StylusPoints)
                        {
                            newPoints.Add(new StylusPoint(
                                point.X + currentX, point.Y));
                        }
                        stringStrokes.Add(new Stroke(newPoints));
                    }

                    currentX += token.width;
                }
            }

            return stringStrokes;
        }
    }
}
