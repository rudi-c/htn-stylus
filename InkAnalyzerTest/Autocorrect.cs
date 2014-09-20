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

namespace InkAnalyzerTest
{
    // In our app, we define the "font size" of a letter to be 
    // the median (or midline) minus the baseline.
    // See http://en.wikipedia.org/wiki/Ascender_(typography)

    // Class extensions which contains autocorrection logic.
    public partial class MainWindow : Window
    {
        Hunspell spellchecker = new Hunspell("en_us.aff", "en_us.dic");
        HashSet<InkWordNode> uncheckedNewWordNodes = new HashSet<InkWordNode>();

        Dictionary<char, StylusToken> fontData = new Dictionary<char, StylusToken>();

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
        }

        void AutocorrectNewWordNodes()
        {
            foreach (InkWordNode inkWordNode in uncheckedNewWordNodes)
            {
                string recognizedString = inkWordNode.GetRecognizedString();
                bool correct = spellchecker.Spell(recognizedString);
                if (!correct)
                {
                    List<string> suggestions = spellchecker.Suggest(inkWordNode.GetRecognizedString());
                    if (suggestions.Count > 0)
                    {
                        SuggestionsBox suggestionsBox = new SuggestionsBox(inkWordNode, suggestions, fontData);

                        // For now, only autocorrect one word at once.
                        break;
                    }
                }
            }

            uncheckedNewWordNodes.Clear();
        }
    }
}
