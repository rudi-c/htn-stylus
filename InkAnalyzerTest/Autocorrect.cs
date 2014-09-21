using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NHunspell;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace InkAnalyzerTest
{
    // In our app, we define the "font size" of a letter to be 
    // the baseline minus the median (or midline).
    // See http://en.wikipedia.org/wiki/Ascender_(typography)

    // Class extensions which contains autocorrection logic.
    public partial class MainWindow : Window
    {
        Hunspell spellchecker = new Hunspell("en_us.aff", "en_us.dic");
        HashSet<InkWordNode> uncheckedNewWordNodes = new HashSet<InkWordNode>();
        SuggestionsBox suggestionsBox;

        Dictionary<char, StylusToken> fontData = new Dictionary<char, StylusToken>();

        int strokesAddedSinceSuggestionsShown = 0;

        void InkAnalyzer_ContextNodeCreated(object sender, ContextNodeCreatedEventArgs e)
        {
            // The GetRecognizedString returns null when the ContextNodeCreated event
            // is called, so we need to deal with it later.
            InkWordNode inkWordNode = e.NodeCreated as InkWordNode;
            if (inkWordNode != null)
                uncheckedNewWordNodes.Add(inkWordNode);
        }

        void AutocorrectInit()
        {
            using (Stream stream = new FileStream("rudi_hand_font.txt", FileMode.Open))
            {
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<char, DataStylusToken>));
                var data = (Dictionary<char, DataStylusToken>)serializer.ReadObject(stream);

                foreach (var token in data)
                {
                    StrokeCollection strokes = token.Value.Representation();

                    // We made our characters in 100x100 boxes.
                    StylusToken stylusToken = new StylusToken(strokes);
                    stylusToken.Normalize();
                    fontData.Add(token.Key, stylusToken);
                }
            }

            // To display autocorrect stuff.
            suggestionsBox = new SuggestionsBox(this);

            OverlayCanvas.Children.Add(suggestionsBox);
            Grid.SetRow(suggestionsBox, 0);
            Grid.SetColumn(suggestionsBox, 0);
            suggestionsBox.Background = new SolidColorBrush(Colors.LightGray);
            suggestionsBox.Visibility = Visibility.Collapsed;
        }

        public void AutocorrectNewWordNodes()
        {
            foreach (InkWordNode inkWordNode in uncheckedNewWordNodes)
            {
                string recognizedString = inkWordNode.GetRecognizedString();
                bool correct = !Regex.IsMatch(recognizedString, @"^[a-zA-Z]+$") || 
                    spellchecker.Spell(recognizedString);

                if (!correct)
                {
                    List<string> suggestions = spellchecker.Suggest(inkWordNode.GetRecognizedString());
                    List<string> alphabeticalSuggestions = new List<string>();

                    foreach (string suggestion in suggestions)
                        if (Regex.IsMatch(suggestion, @"^[a-zA-Z]+$"))
                            alphabeticalSuggestions.Add(suggestion);

                    if (alphabeticalSuggestions.Count > 0)
                    {
                        suggestionsBox.SetSuggestions(inkWordNode, alphabeticalSuggestions, fontData);
                        suggestionsBox.Visibility = Visibility.Visible;
                        strokesAddedSinceSuggestionsShown = 0;

                        // For now, only autocorrect one word at once.
                        break;
                    }
                }
            }

            uncheckedNewWordNodes.Clear();
        }

        void AutocorrectHandleAddStroke(Stroke stroke)
        {
            strokesAddedSinceSuggestionsShown++;

            // Don't leave the suggestions box open for too long.
            if (strokesAddedSinceSuggestionsShown > 7)
                suggestionsBox.Visibility = Visibility.Collapsed;
        }
    }
}
