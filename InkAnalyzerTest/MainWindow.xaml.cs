using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using InkAnalyzerTest.Processors;
using MahApps.Metro.Controls;

namespace InkAnalyzerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        InkAnalyzer inkAnalyzer;
        public PipelineAnalyzer pipeline;
        Headings headings;
        GraphAnalyzer graphAnalyzer;
        InsertionProcessor inserter;
        InsertionBox insertionBox;

        bool continuousAnalyze = true;
        bool didFirstInvalidate = false;

        public MainWindow()
        {
            graphAnalyzer = new GraphAnalyzer(this);
            InitializeComponent();

            AutocorrectInit();
        }

        void DisableDictionary()
        {
            AnalysisHintNode hint = inkAnalyzer.CreateAnalysisHint();
            hint.Factoid = "NONE";
            hint.Location.MakeInfinite();
        }

        private void InkWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Initialize analyzer and pipeline
            inkAnalyzer = new InkAnalyzer(this.Dispatcher);
            pipeline = new PipelineAnalyzer(inkAnalyzer);

            //Initialize headings
            headings = new Headings();
            headings.sidebar = SideInkCanvas;
            headings.scrollViewerContainer = MainScrollView;

            MainInkCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
            inkAnalyzer.ContextNodeCreated += InkAnalyzer_ContextNodeCreated;
            pipeline.PipelineComplete += pipeline_PipelineComplete;

            //DisableDictionary();

            insertionBox = new InsertionBox(inserter);
            insertionBox.TheInsertButton.Click += InsertButton_Click;
            insertionBox.TheCancelButton.Click += CancelButton_Click;
            OverlayCanvas.Children.Add(insertionBox);
            insertionBox.Visibility = Visibility.Collapsed;

            inserter = new InsertionProcessor(MainInkCanvas, insertionBox);
            pipeline.AddProcessor(inserter);
            pipeline.AddProcessor(new StrikethroughProcessor(MainInkCanvas));
            pipeline.AddProcessor(new ReflowProcessor(MainInkCanvas));
            pipeline.AddProcessor(new NavigationProcessor(headings));
            pipeline.AddProcessor(new AutocorrectProcessor(this));
        }

        void pipeline_PipelineComplete(object sender, EventArgs e)
        {
            // We're completely rebuilding the tree view.
            AnalysisView.Items.Clear();

            TreeViewItem rootTreeItem = new TreeViewItem();
            rootTreeItem.Tag = inkAnalyzer.RootNode;
            rootTreeItem.Header = inkAnalyzer.RootNode.ToString();

            AnalysisView.Items.Add(rootTreeItem);
            BuildTree(inkAnalyzer.RootNode, rootTreeItem);

            GenerateBoundingBoxes();
        }

        void SideInkCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            headings.click(e.GetPosition(SideInkCanvas));
        }

        void MainInkCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (e.GetPosition(MainInkCanvas).X <= 10)
            {
                openSidebar();
            }
            else
            {
                hideSidebar();
            }
        }

        void MainInkCanvas_StylusEnter(object sender, StylusEventArgs e)
        {
            // This is needed to fix a bug where the first rending of the first
            // stroke is off horizontally for some reason.
            if (!didFirstInvalidate)
            {
                MainInkCanvas.InvalidateVisual();
                didFirstInvalidate = true;
            }
        }

        void MainInkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.GetPosition(MainInkCanvas).X <= 15)
            {
                openSidebar();
            }
            else
            {
                hideSidebar();
            }
        }

        private void openSidebar()
        {
            SidePanel.Visibility = Visibility.Visible;
        }

        private void hideSidebar()
        {
            SidePanel.Visibility = Visibility.Collapsed;
        }

        void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            foreach (Stroke stroke in e.Added)
            {
                if (!graphAnalyzer.newStroke(stroke))
                {
                    inkAnalyzer.AddStroke(stroke);
                }

                AutocorrectHandleAddStroke(stroke);
            }
        
            double ymax = InkUtils.StrokeYMax(e.Added);
            if (ymax > MainInkCanvas.ActualHeight - 100.0)
                MainInkCanvas.Height = ymax + 200.0; 

            foreach (Stroke stroke in e.Removed)
            {
                graphAnalyzer.removeStroke(stroke);
                // If we erase a word and try to replace it with autocorrect
                // suggestions, there's no good way to define the behavior
                // so just hide the suggestions.
                suggestionsBox.Visibility = Visibility.Collapsed;

                inkAnalyzer.RemoveStroke(stroke);
            }
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            pipeline.QueueAnalysis();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            insertionBox.Visibility = Visibility.Collapsed;
            inserter.insertStrokes(inkAnalyzer, MainInkCanvas, insertionBox.InkCanvas);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            insertionBox.Visibility = Visibility.Collapsed;
            inserter.cancel(MainInkCanvas, insertionBox.InkCanvas);
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

            ContextNode cNode = (ContextNode) selectedItem.Tag;

            MarkNodeAsRed(cNode);
        }

        private void MarkNodeAsRed(ContextNode cNode)
        {
            foreach (Stroke stroke in MainInkCanvas.Strokes)
                stroke.DrawingAttributes.Color = Colors.Black;

            foreach (Stroke stroke in cNode.Strokes)
                stroke.DrawingAttributes.Color = Colors.Red;
        }

        private void BoundingBoxCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            OverlayInkCanvas.Visibility = Visibility.Collapsed;
        }

        private void BoundingBoxCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OverlayInkCanvas.Visibility = Visibility.Visible;
            GenerateBoundingBoxes();
        }

        private void ContinuousCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            continuousAnalyze = false;
        }

        private void ContinuousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            continuousAnalyze = true;
        }

        private void GenerateBoundingBoxes(ContextNode node = null)
        {
            if (node == null)
            {
                OverlayInkCanvas.Strokes.Clear();
                node = inkAnalyzer.RootNode;
            }

            PointCollection boundingBox = null;
            Color strokeColor = Colors.Black;
            if (node is InkWordNode)
            {
                boundingBox = (node as InkWordNode).GetRotatedBoundingBox();
                strokeColor = Colors.Azure;
            }
            else if (node is LineNode)
            {
                boundingBox = (node as LineNode).GetRotatedBoundingBox();
                strokeColor = Colors.Lime;
            }
            else if (node is ParagraphNode)
            {
                boundingBox = (node as ParagraphNode).GetRotatedBoundingBox();
                strokeColor = Colors.Magenta;
            }
            else if (node is InkDrawingNode)
            {
                boundingBox = (node as InkDrawingNode).GetRotatedBoundingBox();
                strokeColor = Colors.Gold;
            }

            if (boundingBox != null)
            {
                StrokeCollection collection = new StrokeCollection();

                // Copy of the points for wrapping.
                boundingBox.Add(boundingBox[0]);
                for (int i = 0; i < boundingBox.Count - 1; i++)
                {
                    StylusPointCollection points = new StylusPointCollection();
                    StylusPoint point1 = new StylusPoint(boundingBox[i].X, boundingBox[i].Y, 1.0f);
                    StylusPoint point2 = new StylusPoint(boundingBox[i + 1].X, boundingBox[i + 1].Y, 1.0f);
                    points.Add(point1);
                    points.Add(point2);

                    Stroke stroke = new Stroke(points);
                    stroke.DrawingAttributes.IsHighlighter = true; // for transparency
                    stroke.DrawingAttributes.Color = strokeColor;
                    collection.Add(stroke);
                }

                OverlayInkCanvas.Strokes.Add(collection);
            }

            // Recurse
            foreach (ContextNode child in node.SubNodes)
                GenerateBoundingBoxes(child);
        }

        private void MainInkCanvas_StylusLeave(object sender, StylusEventArgs e)
        {
            if (continuousAnalyze)
                inkAnalyzer.BackgroundAnalyze();
        }

        private void MainInkCanvas_StylusOutOfRange(object sender, StylusEventArgs e)
        {
            if (continuousAnalyze)
                inkAnalyzer.BackgroundAnalyze();
        }

        private void DebugToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            MainGrid.ColumnDefinitions[3].Width = new GridLength(0);
        }

        private void DebugToggle_Checked(object sender, RoutedEventArgs e)
        {
            MainGrid.ColumnDefinitions[3].Width = new GridLength(20, GridUnitType.Star);
        }
    }
}
