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

using InkAnalyzerTest.Processors;

namespace InkAnalyzerTest
{
    /// <summary>
    /// Interaction logic for InsertionBox.xaml
    /// </summary>
    public partial class InsertionBox : UserControl
    {
        public Button TheInsertButton
        {
            get { return InsertButton;  }
        }

        public Button TheCancelButton
        {
            get { return CancelButton;  }
        }

        public InkCanvas InkCanvas
        {
            get { return InsertCanvas; }
        }

        public InsertionBox(InsertionProcessor inserter)
        {
            InitializeComponent();
        }
    }
}
