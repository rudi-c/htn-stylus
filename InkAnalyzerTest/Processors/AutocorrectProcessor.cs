using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace InkAnalyzerTest.Processors
{
    // This needs refactoring, Autocorrect.cs still has more logic
    // than desirable.
    class AutocorrectProcessor : InkProcessor
    {
        private MainWindow window;

        public AutocorrectProcessor(MainWindow window)
        {
            this.window = window;
        }

        public void process(InkAnalyzer inkAnalyzer)
        {
            window.AutocorrectNewWordNodes();
        } 
    }
}
