using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using pdftron.PDF;

using UIRect = System.Windows.Rect;
using UIPoint = System.Windows.Point;

using PDFRect = pdftron.PDF.Rect;

namespace pdftron.PDF.Tools
{
    /// <summary>
    /// Interaction logic for FormSelectionView.xaml
    /// </summary>
    public partial class FormSelectionView : UserControl
    {
        public delegate void OnSelectionChanged(IList<string> selection);

        public event OnSelectionChanged SelectionChanged;


        private Popup _ContentPopup;
        
        private PDFViewWPF _PDFViewWPF;
        private Annot _Widget;
        private PDFRect _WidgetRect;
        List<string> _Options;
        private double _FontSize;
        private bool _IsMultiChoice;
        private bool _IsCombo;

        private Grid _TouchDownGrid;
        private List<Grid> _SelectedItems;

        /// <summary>
        /// Gets or sets whether the menu is open
        /// </summary>
        public bool IsOpen
        {
            get { return _ContentPopup.IsOpen; }
            set { _ContentPopup.IsOpen = value; }
        }

        public FormSelectionView()
        {
            InitializeComponent();
        }

        public FormSelectionView(PDFViewWPF pdfView, Annot widget, PDFRect rect, List<string> options, double fontSize, double borderWidth, bool isMultiChoice, bool isCombo)
        {
            InitializeComponent();
            _PDFViewWPF = pdfView;
            _Widget = widget;
            _WidgetRect = rect;
            _Options = options;
            _FontSize = Math.Max(18, fontSize);
            BackgroundBorder.BorderThickness = new System.Windows.Thickness(Math.Max(1, borderWidth));
            _IsMultiChoice = isMultiChoice;
            _IsCombo = isCombo;

            _SelectedItems = new List<Grid>();

            CreatePopup();
            PositionPopup();
            CreateOptions();
            SetUpManipulation();
        }


        public void SetSelectedItem(int index)
        {
            try
            {
                Object optionItem = OptionStackPanel.Children[index];
                Grid grid = optionItem as Grid;
                if (grid != null)
                {
                    SelectItem(optionItem as Grid);
                }
            }
            catch (System.Exception ex)
            {
            	System.Diagnostics.Debug.WriteLine("could not select the item: " + ex.ToString());
            }
        }

        public void SetSelectedItems(IList<int> indexes)
        {
            foreach (int index in indexes)
            {
                SetSelectedItem(index);
            }
        }

        private void CreatePopup()
        {
            _ContentPopup = new Popup();
            _ContentPopup.PlacementTarget = _PDFViewWPF;
            _ContentPopup.Child = this;
            _ContentPopup.StaysOpen = false;
            _ContentPopup.IsOpen = true;

            this.MinWidth = _WidgetRect.Width();
            this.MinHeight = _WidgetRect.Height();
            this.MaxWidth = _PDFViewWPF.ActualWidth;
            this.MaxHeight = _PDFViewWPF.ActualHeight;

            this.SizeChanged += FormSelectionView_SizeChanged;
        }

        void FormSelectionView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionPopup();
        }

        private void PositionPopup()
        {
            _ContentPopup.Placement = PlacementMode.Relative;

            _ContentPopup.HorizontalOffset = this.ActualWidth + _WidgetRect.x1;
            if (_IsCombo)
            {
                _ContentPopup.VerticalOffset = _WidgetRect.y2;
            }
            else
            {
                _ContentPopup.VerticalOffset = _WidgetRect.y1;
            }
        }


        private void CreateOptions()
        {
            foreach (string option in _Options)
            {
                Grid g = new Grid();
                TextBlock tb = new TextBlock();
                g.Children.Add(tb);
                OptionStackPanel.Children.Add(g);

                tb.Text = option;
                g.Style = OptionStackPanel.FindResource("SelectionGridBaseStyle") as Style;
                g.TouchDown += Option_TouchDown;
            }
        }

        void Option_TouchDown(object sender, TouchEventArgs e)
        {
            _TouchDownGrid = sender as Grid;
            if (_TouchDownGrid != null)
            {
                _TouchDownGrid.CaptureTouch(e.TouchDevice);
            }
        }


        #region Manipulation

        private void SetUpManipulation()
        {
            BackgroundBorder.ManipulationStarting += BackgroundBorder_ManipulationStarting;
            BackgroundBorder.ManipulationStarted += BackgroundBorder_ManipulationStarted;
            BackgroundBorder.ManipulationInertiaStarting += BackgroundBorder_ManipulationInertiaStarting;
            BackgroundBorder.ManipulationDelta += BackgroundBorder_ManipulationDelta;
            BackgroundBorder.ManipulationCompleted += BackgroundBorder_ManipulationCompleted;
            BackgroundBorder.ManipulationBoundaryFeedback += (s, e) => { e.Handled = true; };
        }

        void BackgroundBorder_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = BackgroundBorder;
            e.Handled = true;
        }

        void BackgroundBorder_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            e.Handled = true;
        }

        void BackgroundBorder_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.005;
            e.TranslationBehavior.InitialVelocity = e.InitialVelocities.LinearVelocity;
        }

        void BackgroundBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            OptionsScroller.ScrollToVerticalOffset(OptionsScroller.VerticalOffset - e.DeltaManipulation.Translation.Y);
        }

        void BackgroundBorder_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.TotalManipulation.Translation.Length < 10 && _TouchDownGrid != null)
            {
                if (_IsMultiChoice)
                {
                    if (_SelectedItems.Contains(_TouchDownGrid))
                    {
                        DeselectItem(_TouchDownGrid);
                    }
                    else
                    {
                        SelectItem(_TouchDownGrid);
                    }
                    ReturnSelection();
                }
                else
                {
                    if (!_SelectedItems.Contains(_TouchDownGrid))
                    {
                        DeselectItem(_SelectedItems[0]);
                        SelectItem(_TouchDownGrid);
                        ReturnSelection();
                    }
                }
            }
            _TouchDownGrid = null;
        }

        #endregion Manipulation

        private void SelectItem(Grid grid)
        {            
            Style style = OptionStackPanel.FindResource("SelectionGridSelectedStyle") as Style;
            grid.Style = style;
            _SelectedItems.Add(grid);
        }

        private void DeselectItem(Grid grid)
        {
            Style style = OptionStackPanel.FindResource("SelectionGridBaseStyle") as Style;
            grid.Style = style;
            _SelectedItems.Remove(grid);
        }

        private void ReturnSelection()
        {
            List<string> selectionList = new List<string>();

            // We want to make sure we return the items in the order they appear in the list
            // otherwise, the appearance will be incorrect.
            for (int i = 0; i < OptionStackPanel.Children.Count; i++)
            {
                Grid grid = OptionStackPanel.Children[i] as Grid;
                if (grid != null && _SelectedItems.Contains(grid))
                {
                    TextBlock tb = grid.Children[0] as TextBlock;
                    selectionList.Add(tb.Text);
                }
            }

            if (SelectionChanged != null)
            {
                SelectionChanged(selectionList);
            }
        }

    }
}
