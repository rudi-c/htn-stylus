using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace InkAnalyzerTest.Processors
{
    public interface InkProcessor
    {
        void process(InkAnalyzer inkAnalyzer);
    }
}
