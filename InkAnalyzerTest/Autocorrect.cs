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

namespace InkAnalyzerTest
{
    // Class extensions which contains autocorrection logic.
    public partial class MainWindow : Window
    {
        Hunspell spellchecker = new Hunspell("en_us.aff", "en_us.dic");
        HashSet<InkWordNode> uncheckedNewWordNodes = new HashSet<InkWordNode>();

        void InkAnalyzer_ContextNodeCreated(object sender, ContextNodeCreatedEventArgs e)
        {
            // The GetRecognizedString returns null when the ContextNodeCreated event
            // is called, so we need to deal with it later.
            InkWordNode inkWordNode = e.NodeCreated as InkWordNode;
            if (inkWordNode != null)
                uncheckedNewWordNodes.Add(inkWordNode);
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

                    }
                }
            }

            uncheckedNewWordNodes.Clear();
        }
    }
}
