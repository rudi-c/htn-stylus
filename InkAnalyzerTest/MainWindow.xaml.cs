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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        InkAnalyzer inkAnalyzer = new InkAnalyzer();
        CanvasEditor canvasEditor = new CanvasEditor();

        public MainWindow()
        {
            InitializeComponent();

            AutocorrectInit();
        }

        private void InkWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MainInkCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
            inkAnalyzer = new InkAnalyzer();

            inkAnalyzer.ContextNodeCreated += InkAnalyzer_ContextNodeCreated;
        }

        void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            foreach (Stroke stroke in e.Added) {
                inkAnalyzer.AddStroke(stroke);
            }

            foreach (Stroke stroke in e.Removed) {
                inkAnalyzer.RemoveStroke(stroke);
            }
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            inkAnalyzer.Analyze();

            AutocorrectNewWordNodes();

            // We're completely rebuilding the tree view.
            AnalysisView.Items.Clear();

            TreeViewItem rootTreeItem = new TreeViewItem();
            rootTreeItem.Tag = inkAnalyzer.RootNode;
            rootTreeItem.Header = inkAnalyzer.RootNode.ToString();

            AnalysisView.Items.Add(rootTreeItem);
            BuildTree(inkAnalyzer.RootNode, rootTreeItem);

            canvasEditor.analyzeStrokeEvent(inkAnalyzer, MainInkCanvas);
        }

        // http://msdn.microsoft.com/en-us/library/system.windows.ink.contextnode(v=vs.90).aspx
        private void BuildTree(ContextNode parentCNode, TreeViewItem parentTNode)
        {
            parentTNode.IsExpanded = true;

            foreach (ContextNode cNode in parentCNode.SubNodes)
            {
                // Create new tree node corresponding to context node.
                TreeViewItem tNode = new TreeViewItem();
                tNode.Tag = cNode;
                tNode.Header = cNode.ToString();

                if (cNode is InkWordNode)
                {
                    tNode.Header += ": " + (cNode as InkWordNode).GetRecognizedString();
                }
                else if (cNode is InkDrawingNode)
                {
                    tNode.Header += ": " + (cNode as InkDrawingNode).GetShapeName();
                }

                if (cNode.IsConfirmed(ConfirmationType.NodeTypeAndProperties))
                {
                    tNode.Header += "Confirmed.";
                }

                parentTNode.Items.Add(tNode);
                BuildTree(cNode, tNode);
            }
        }

        private void AnalysisView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = e.NewValue as TreeViewItem;
            if (selectedItem == null)
                return;

            ContextNode cNode = (ContextNode)selectedItem.Tag;

            MarkNodeAsRed(cNode);
        }

        private void MarkNodeAsRed(ContextNode cNode)
        {
            foreach (Stroke stroke in MainInkCanvas.Strokes)
                stroke.DrawingAttributes.Color = Colors.Black;

            foreach (Stroke stroke in cNode.Strokes)
                stroke.DrawingAttributes.Color = Colors.Red;
        }
    }
}
