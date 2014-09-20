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

namespace InkAnalyzerTest
{
    // Class extensions which contains autocorrection logic.
    public partial class MainWindow : Window
    {
        void InkAnalyzer_ContextNodeCreated(object sender, ContextNodeCreatedEventArgs e)
        {
            InkWordNode inkWordNode = e.NodeCreated as InkWordNode;
            if (inkWordNode != null)
            {

            }
        }
    }
}
