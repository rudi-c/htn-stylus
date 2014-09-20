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
        private MainWindow mainWindow;

        public SuggestionsBox(MainWindow window)
        {
            mainWindow = window;

            InitializeComponent();
        }

        public void SetSuggestions(InkWordNode _incorrectWord, List<String> suggestions,
            Dictionary<char, StylusToken> fontData)
        {
            SuggestionsStack.Children.Clear();

            incorrectWord = _incorrectWord;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (Point point in incorrectWord.GetRotatedBoundingBox())
            {
                minX = Math.Min(minX, (int)point.X);
                maxX = Math.Max(maxX, (int)point.X);
                minY = Math.Min(minY, (int)point.Y);
                maxY = Math.Max(maxY, (int)point.Y);
            }
            this.Width = 0;
            this.Height = 0;

            var midline = incorrectWord.GetMidline();
            var baseline = incorrectWord.GetBaseline();

            // Assume that the midline and baseline are horizontal lines
            // i.e. two points, same y coordinate.
            double wordSize = baseline[0].Y - midline[0].Y;
            
            // Keep the top 3 suggestions for now.
            int displayCount = Math.Min(3, suggestions.Count);
            for (int i = 0; i < displayCount; i++)
            {
                StrokeCollection strokeRepresentation = 
                    GetStrokesForString(suggestions[i], fontData);

                // In the font maker, the font size is currently 30.0
                InkUtils.Scale(strokeRepresentation, wordSize / 30.0);
                InkUtils.MatchThickness(incorrectWord.Strokes, strokeRepresentation);

                InkCanvas suggestionCanvas = new InkCanvas();
                suggestionCanvas.Strokes.Add(strokeRepresentation);
                suggestionCanvas.Height = InkUtils.StrokeYRange(strokeRepresentation) * 1.4;
                suggestionCanvas.Width = InkUtils.StrokeXRange(strokeRepresentation) + 10;
                suggestionCanvas.TouchDown += SuggestionCanvas_TouchDown;
                suggestionCanvas.StylusDown += SuggestionCanvas_StylusDown;

                this.Width = Math.Max(this.Width, suggestionCanvas.Width);
                this.Height += suggestionCanvas.Height;

                // We shouldn't be writing on this canvas, it's only for
                // display purposes.
                suggestionCanvas.EditingMode = InkCanvasEditingMode.None;

                SuggestionsStack.Children.Add(suggestionCanvas); 
            }

            // Positioning.
            if (minY < this.Height)
            {
                // Show suggestions under incorrect word.
                Canvas.SetTop(this, maxY);
            }
            else
            {
                // Show suggestions above incorrect word.
                Canvas.SetTop(this, minY - this.Height);
            }
            Canvas.SetLeft(this, minX);
        }

        void SuggestionCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            ChooseSuggestion(sender as InkCanvas);
        }

        void SuggestionCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            ChooseSuggestion(sender as InkCanvas);
        }

        void ChooseSuggestion(InkCanvas canvas)
        {
            // Incase we get a double event.
            if (this.Visibility == Visibility.Collapsed)
                return;

            StrokeCollection newStrokes = canvas.Strokes;

            double baseline = incorrectWord.GetBaseline()[0].Y;
            InkUtils.Shift(newStrokes, InkUtils.StrokeXMin(incorrectWord.Strokes),
                baseline - 35);

            mainWindow.MainInkCanvas.Strokes.Remove(incorrectWord.Strokes);
            mainWindow.MainInkCanvas.Strokes.Add(newStrokes);

            this.Visibility = Visibility.Collapsed;
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
