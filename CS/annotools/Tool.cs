using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pdftron.PDF;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Input;



using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;

using pdftron.PDF.Annots;
using System.Windows.Media;

namespace pdftron.PDF.Tools
{

    public class Tool : Canvas
    {
        /// <summary>
        /// A convenience structure that represents all the text selection on a particular page.
        /// </summary>
        public struct SelectionDrawing
        {
            public bool IsAttached;
            public double PageHeight;
            public List<PDFRect> Quads;
            public Canvas Canvas;
        }

        protected PDFViewWPF mPDFView;
        protected ToolManager mToolManager;
        internal ToolManager ToolManager
        {
            get { return mToolManager; }
        }

        protected bool mUseSameToolWhenDone = false;
        protected bool mJustSwitchedFromAnotherTool = true;

        protected Canvas mViewerCanvas;


        // Current Annotation
        protected Annot mAnnot = null;
        protected int mAnnotPageNum = 0;
        protected PDFRect mAnnotBBox = null;

        //////////////////////////////////////////////////////////////////////////
        // Mouse States
        protected bool mIsDragging = false;
        protected bool mLeftButtonDown = false;
        protected bool mRightButtonDown = false;


        //////////////////////////////////////////////////////////////////////////
        // Context menu
        protected ContextMenu mContextMenu;
        protected bool mIsContextMenuOpen = false;
        protected bool mDidPressCloseContextMenu = false;
        protected bool mIsRightTapOnText = false;
        protected bool mAllowTextSelectionOptions = true;

        //////////////////////////////////////////////////////////////////////////
        // Keyboard Handling
        protected bool mIsCtrlDown = false;
        protected bool mIsShiftDown = false;
        protected bool mIsAltDown = false;
        protected bool mIsModifierKeyDown = false; // aggregate to simplify
        protected bool mIsContinuousMode = false; // Some keys will work different depending on this.
        protected bool mShouldHandleKeyEvents = true;
        protected const int ARROW_KEY_SCROLL_DISATANCE = 30;

        // Text Selection
        protected int mSelectionStartPage;
        protected int mSelectionEndPage;
        protected List<PDFRect> mSelectedAreasForHitTest;

        protected List<int> mPagesOnScreen;
        protected Dictionary<int, SelectionDrawing> mSelectionCanvases;
        protected Dictionary<int, PDFRect> mSelectionRectangles;
        protected double CumulativeRotation = 0;

        protected SolidColorBrush mTextSelectionBrush = new SolidColorBrush(Color.FromArgb(100, 80, 110, 200));

        /// <summary>
        /// Set this to false if you don't want the tools to handle key events, e.g. when a textbox is open
        /// </summary>
        internal bool ShouldHandleKeyEvents
        {
            set { mShouldHandleKeyEvents = value; }
        }

        /// <summary>
        /// Determines if we should only use the tool once, and then return to the default, or if we should keep on using it.
        /// e.g. We can draw multiple ovals without having to re-select the tool.
        /// </summary>
        internal bool UseSameToolWhenDone
        {
            set { mUseSameToolWhenDone = value; }
        }

        protected ToolManager.ToolType mToolMode;
        /// <summary>
        /// Gets the current tool type.
        /// </summary>
        public ToolManager.ToolType ToolMode
        {
            get { return mToolMode; }
        }

        protected ToolManager.ToolType mNextToolMode;
        internal ToolManager.ToolType NextToolMode
        {
            get { return mNextToolMode; }
        }

        protected bool IsShapeCreationTool
        {
            get
            {
                return this is SimpleShapeCreate || this is TextMarkupCreate || this is StickyNoteCreate;
            }
        }

        internal Tool(PDFViewWPF view, ToolManager manager) : base()
        {
            mPDFView = view;
            mToolManager = manager;
            mPDFView.Cursor = Cursors.Arrow;

            mSelectionCanvases = new Dictionary<int, SelectionDrawing>();
            mPagesOnScreen = new List<int>();
        }

        /// <summary>
        /// Will transfer some data from the old tool to the new. It's used by the
        /// ToolManager when we're switching to a new tool.
        /// </summary>
        /// <param name="oldTool"></param>
        internal virtual void Transfer(Tool oldTool)
        {
            mAnnot = oldTool.mAnnot;
            mAnnotBBox = oldTool.mAnnotBBox;
            mAnnotPageNum = oldTool.mAnnotPageNum;
            if (this.ToolMode != oldTool.ToolMode)
            {
                mJustSwitchedFromAnotherTool = true;
            }
        }

        /// <summary>
        /// This will be called after transfer, so that we can initialize with the values transferred to us.
        /// </summary>
        internal virtual void OnCreate()
        {

        }

        /// <summary>
        /// Cleans up after we're done with the tool
        /// </summary>
        internal virtual void OnClose()
        {
            Canvas viewerCanvas = mToolManager.AnnotationCanvas;
            if (viewerCanvas.Children.Contains(this))
            {
                viewerCanvas.ReleaseMouseCapture();
                if (!IsShapeCreationTool)
                {
                    viewerCanvas.Children.Remove(this);
                }
            }
        }

        #region Event Handling

        internal virtual void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            mDidPressCloseContextMenu = false;
            if (mIsContextMenuOpen)
            {
                mDidPressCloseContextMenu = true;
                return;
            }

            mLeftButtonDown = true;
            mIsDragging = true;
        }


        internal virtual void MouseRightButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            //mDidPressCloseContextMenu = false;
            //if (mIsContextMenuOpen)
            //{
            //    mDidPressCloseContextMenu = true;
            //    return;
            //}
            //if (IsPointInSelection(e.GetPosition(mPDFView)))
            //{
            //    mIsRightTapOnText = true;
            //}
            //else
            //{
            //    mIsRightTapOnText = false;
            //    DeselectAllText();
            //}
            //mRightButtonDown = true;
        }

        internal virtual void MouseMovedHandler(Object sender, MouseEventArgs e)
        {

        }

        internal virtual void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            mLeftButtonDown = false;
            mIsDragging = false;
        }


        internal virtual void MouseRightButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            //if (mDidPressCloseContextMenu)
            //{
            //    mDidPressCloseContextMenu = false;
            //    return;
            //}
            //mRightButtonDown = false;
            //if (!mLeftButtonDown)
            //{
            //    mIsDragging = false;
            //}

            //if (!mToolManager.SuppressContextMenu)
            //{
            //    UIPoint downPoint = e.GetPosition(mPDFView);
            //    CreateContextMenu(downPoint.X, downPoint.Y);
            //    mIsContextMenuOpen = true;
            //    mDidPressCloseContextMenu = false;
            //}
        }

        /// <summary>
        /// If ctrl is pressed, this will zoom in or out on the PDFViewCtrl.
        /// 
        /// Note: This only works if the PDFViewCtrl is in focus, otherwise, it won't.
        /// The reason is that the modifier keys (ctrl) are hooked up only to the PDFViewWPF to keep the tool more modular.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void PreviewMouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            GetModifierKeyStates();
            if (mIsCtrlDown)
            {
                if (e.Delta > 0)
                {
                    UIPoint screenPoint = e.GetPosition(mPDFView);
                    mPDFView.SetZoom((int)screenPoint.X - 5, (int)screenPoint.Y - 5, mPDFView.GetZoom() * 1.25, true);
                    e.Handled = true;
                }
                else if (e.Delta < 0)
                {
                    UIPoint screenPoint = e.GetPosition(mPDFView);
                    mPDFView.SetZoom((int)screenPoint.X, (int)screenPoint.Y, mPDFView.GetZoom() / 1.25, true);
                    e.Handled = true;
                }
            }
            else
            {
                if (!mIsContinuousMode)
                {
                    if (e.Delta > 0 && !mPDFView.CanViewerScrollUp())
                    {
                        int page = mPDFView.GetCurrentPage();
                        if (page > 1)
                        {
                            mPDFView.GotoPreviousPage();
                            mPDFView.SetVScrollPos(mPDFView.GetCanvasHeight());
                            e.Handled = true;
                        }
                    }
                    else if (e.Delta < 0 && !mPDFView.CanViewerScrollDown())
                    {
                        int page = mPDFView.GetCurrentPage();
                        if (page < mPDFView.GetPageCount())
                        {
                            mPDFView.GotoNextPage();
                            e.Handled = true;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// This function will return true, so that we can set handler to true. This way, PDFViewCtrl isn't allowed to scroll or zoom.
        /// 
        /// Any tool that wants to allow scrolling will have to overwrite this function and then return false.
        /// </summary>
        internal virtual void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {

        }

        /// <summary>
        /// This is the entry point for the ToolManager to let the tools handle keyboard events.
        /// This function will set up the key handling and then pass on to the KeyDownAction,
        /// which is to be extended by other tools that want to handle key presses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void KeyDownHandler(object sender, KeyEventArgs e)
        {
            GetModifierKeyStates();
            KeyDownAction(sender, e);
        }

        /// <summary>
        /// This function will be called when a key press is detected. Should be extended by other tools that
        /// want to handle keyboard events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void KeyDownAction(object sender, KeyEventArgs e)
        {
            if (!mShouldHandleKeyEvents)
            {
                return;
            }
            if (mIsDragging)
            {
                e.Handled = true;
                return;
            }
            if (mIsCtrlDown)
            {
                switch (e.Key)
                {
                    case Key.PageUp:
                    case Key.Up:
                    case Key.Left:
                        mPDFView.GotoPreviousPage();
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                    case Key.Down:
                    case Key.Right:
                        mPDFView.GotoNextPage();
                        e.Handled = true;
                        break;
                    case Key.Home:
                        mPDFView.GotoFirstPage();
                        e.Handled = true;
                        break;
                    case Key.End:
                        mPDFView.GotoLastPage();
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.PageUp:
                        if (!mPDFView.CanViewerScrollUp() && !mIsContinuousMode)
                        {
                            mPDFView.GotoPreviousPage();
                            mPDFView.SetVScrollPos(mPDFView.GetCanvasHeight());
                            e.Handled = true;
                        }
                        else
                        {
                            mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() - mPDFView.ActualHeight + 30);
                            e.Handled = true;
                        }
                        break;
                    case Key.PageDown:
                        if (!mPDFView.CanViewerScrollDown())
                        {
                            mPDFView.GotoNextPage();
                            e.Handled = true;
                        }
                        else
                        {
                            mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() + mPDFView.ActualHeight - 30);
                            e.Handled = true;
                        }
                        break;
                    case Key.Left:
                        if (mPDFView.ActualWidth >= mPDFView.GetExtendedCanvasWidth())
                        {
                            mPDFView.GotoPreviousPage();
                            e.Handled = true;
                        }
                        else
                        {
                            mPDFView.SetHScrollPos(mPDFView.GetHScrollPos() - ARROW_KEY_SCROLL_DISATANCE);
                            e.Handled = true;
                        }
                        break;
                    case Key.Right:
                        if (mPDFView.ActualWidth >= mPDFView.GetExtendedCanvasWidth())
                        {
                            mPDFView.GotoNextPage();
                            e.Handled = true;
                        }
                        else
                        {
                            mPDFView.SetHScrollPos(mPDFView.GetHScrollPos() + ARROW_KEY_SCROLL_DISATANCE);
                            e.Handled = true;
                        }
                        break;
                    case Key.Up:
                        mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() - ARROW_KEY_SCROLL_DISATANCE);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() + ARROW_KEY_SCROLL_DISATANCE);
                        e.Handled = true;
                        break;
                    case Key.Home:
                        mPDFView.GotoFirstPage();
                        e.Handled = true;
                        break;
                    case Key.End:
                        if (mIsContinuousMode)
                        {
                            mPDFView.SetVScrollPos(mPDFView.GetCanvasHeight());
                            e.Handled = true;
                        }
                        else
                        {
                            mPDFView.GotoLastPage();
                            mPDFView.SetVScrollPos(mPDFView.GetCanvasHeight());
                            e.Handled = true;
                        }
                        break;
                }
            }



            return;
        }

        /// <summary>
        /// This is the entry point for the ToolManager to let the tools handle keyboard events.
        /// This function will set up the key handling and then pass on to the KeyUpAction,
        /// which is to be extended by other tools that want to handle key releases.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void KeyUpHandler(object sender, KeyEventArgs e)
        {
            GetModifierKeyStates();
        }

        /// <summary>
        /// This function will be called when a key release is detected. Should be extended by other tools that
        /// want to handle keyboard events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void KeyUpAction(object sender, KeyEventArgs e)
        {
            return;
        }

        /// <summary>
        /// When the mouse enters the canvas, we want to check which modifier keys are down.
        /// Otherwise, we might accidentally do a ctrl + mouse wheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal virtual void MouseEnterHandler(object sender, MouseEventArgs e)
        {
            GetModifierKeyStates();
            mIsDragging = false;
        }

        internal virtual void MouseLeaveHandler(object sender, MouseEventArgs e)
        {

        }

        internal virtual void MouseClickHandler(object sender, MouseEventArgs e)
        {
        }

        internal virtual void MouseDoubleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }



        internal virtual void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            DrawSelection(true);
        }


        internal virtual void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            DrawSelection(true);
        }


        internal virtual void CurrentPageNumberChangedHandler(PDFViewWPF viewer, int currentPage, int totalPages)
        {
            DrawSelection(true);
        }


        internal virtual void CurrentScrollChangedHandler(PDFViewWPF viewer, ScrollChangedEventArgs e)
        {
            DrawSelection();
        }


        internal virtual void PreviewMouseRightButtonUpHandler(object sender, MouseButtonEventArgs e)
        {

        }


        /// <summary>
        /// Will issue a select all command to the current tool.
        /// The default behavior is to select all text, but this can be overridden by other tools.
        /// </summary>
        public virtual void SelectAll()
        {
            SelectAllText();
        }

        /// <summary>
        /// Will issue a deselect all command to the current tool.
        /// The default behavior is to deselect all text, but this can be overrident by other tools.
        /// </summary>
        public virtual void DeselectAll()
        {
            DeselectAllText();
        }


        //////////////////////////////////////////////////////////////////////////
        // Touch events

        public virtual void TouchDownHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //if (mIsContextMenuOpen)
            //{
            //    mDidPressCloseContextMenu = true;
            //    if (mContextMenu != null)
            //    {
            //        mContextMenu.IsOpen = false;
            //    }
            //    return;
            //}
        }

        public virtual void TouchMoveHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //e.Handled = true;
        }

        public virtual void TouchUpHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //e.Handled = true;
        }

        public virtual void TouchCaptureLostHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //e.Handled = true;
        }

        public virtual void LongPressHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //if (IsPointInSelection(e.GetTouchPoint(mPDFView).Position))
            //{
            //    mIsRightTapOnText = true;
            //}
            //else
            //{
            //    mIsRightTapOnText = false;
            //    DeselectAllText();
            //}
            //if (!mToolManager.SuppressContextMenu)
            //{
            //    UIPoint downPoint = e.GetTouchPoint(mPDFView).Position;
            //    CreateContextMenu(downPoint.X, downPoint.Y);
            //    mIsContextMenuOpen = true;
            //    mDidPressCloseContextMenu = false;
            //}
        }

        public virtual void TapHandler(object sender, TouchEventArgs e)
        {
        }

        //Stylus Event
        public virtual void StylusDownHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {

        }
        public virtual void StylusMoveHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {

        }
        public virtual void StylusUpHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {

        }
        public virtual void StylusCaptureLostHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {

        }
        #endregion Event Handling


        #region Context Menu

        /// <summary>
        /// Adds a collection of Context Menu items to the context menu
        /// </summary>
        /// <param name="menu">The Context Menu</param>
        public virtual void AddContextMenuItems(ContextMenu menu)
        {
            MenuItem m = new MenuItem();
            m.Header = "Zoom In";
            m.Click += ContextMenu_ZoomIn;
            menu.Items.Add(m);

            m = new MenuItem();
            m.Header = "Zoom Out";
            m.Click += ContextMenu_ZoomOut;
            menu.Items.Add(m);

            m = new MenuItem();
            m.Header = "Fit Width";
            m.Click += ContextMenu_FitWidth;
            menu.Items.Add(m);

            m = new MenuItem();
            m.Header = "Fit Page";
            m.Click += ContextMenu_FitPage;
            menu.Items.Add(m);

            if (mAllowTextSelectionOptions)
            {
                Separator sep = new Separator();
                menu.Items.Add(sep);
                if (mIsRightTapOnText)
                {
                    m = new MenuItem();
                    m.Header = "Copy";
                    m.Click += ContextMenu_Copy;
                    menu.Items.Add(m);

                    m = new MenuItem();
                    m.Header = "Select All";
                    m.Click += ContextMenu_SelectAll;
                    menu.Items.Add(m);

                    m = new MenuItem();
                    m.Header = "Deselect All";
                    m.Click += ContextMenu_DeselectAll;
                    menu.Items.Add(m);

                    sep = new Separator();
                    menu.Items.Add(sep);

                    m = new MenuItem();
                    m.Header = "Highlight";
                    m.Click += ContextMenu_Highlight;
                    menu.Items.Add(m);

                    m = new MenuItem();
                    m.Header = "Underline";
                    m.Click += ContextMenu_Underline;
                    menu.Items.Add(m);

                    m = new MenuItem();
                    m.Header = "Strikeout";
                    m.Click += ContextMenu_Strikeout;
                    menu.Items.Add(m);

                    m = new MenuItem();
                    m.Header = "Squiggly";
                    m.Click += ContextMenu_Squiggly;
                    menu.Items.Add(m);
                }
                else
                {
                    m = new MenuItem();
                    m.Header = "Select All";
                    m.Click += ContextMenu_SelectAll;
                    menu.Items.Add(m);
                }
            }
        }


        /// <summary>
        /// Adds a collection of Context Menu items to the context menu
        /// </summary>
        /// <param name="menu">The Context Menu</param>
        /// <param name="x">The x position relative to the PDFViewCtrl</param>
        /// <param name="y">The y position relative to the PDFViewCtrl</param>
        public virtual void AddContextMenuItems(ContextMenu menu, double x, double y)
        {
            AddContextMenuItems(menu);
        }




        void ContextMenu_ZoomIn(object sender, RoutedEventArgs e)
        {
            mPDFView.SetZoom(mPDFView.GetZoom() * 1.25);
        }

        void ContextMenu_ZoomOut(object sender, RoutedEventArgs e)
        {
            mPDFView.SetZoom(mPDFView.GetZoom() / 1.25);
        }

        void ContextMenu_FitWidth(object sender, RoutedEventArgs e)
        {
            mPDFView.SetPageViewMode(pdftron.PDF.PDFViewWPF.PageViewMode.e_fit_width);
        }

        void ContextMenu_FitPage(object sender, RoutedEventArgs e)
        {
            mPDFView.SetPageViewMode(pdftron.PDF.PDFViewWPF.PageViewMode.e_fit_page);
        }

        private void ContextMenu_Copy(object sender, RoutedEventArgs e)
        {
            CopySelectedTextToClipBoard();
        }

        private void ContextMenu_SelectAll(object sender, RoutedEventArgs e)
        {
            SelectAll();
        }

        private void ContextMenu_DeselectAll(object sender, RoutedEventArgs e)
        {
            DeselectAll();
        }


        private void ContextMenu_Highlight(object sender, RoutedEventArgs e)
        {
            CreateTextMarkup(Annot.Type.e_Highlight);
        }

        private void ContextMenu_Underline(object sender, RoutedEventArgs e)
        {
            CreateTextMarkup(Annot.Type.e_Underline);
        }

        private void ContextMenu_Strikeout(object sender, RoutedEventArgs e)
        {
            CreateTextMarkup(Annot.Type.e_StrikeOut);
        }

        private void ContextMenu_Squiggly(object sender, RoutedEventArgs e)
        {
            CreateTextMarkup(Annot.Type.e_Squiggly);
        }

        private void CreateTextMarkup(Annot.Type annotType)
        {
            // Store created markups by page, so that we can request all updates after we have created them all
            Dictionary<int, TextMarkup> textMarkupsToUpdate = new Dictionary<int, TextMarkup>();
            try
            {
                PDFDoc doc = mPDFView.GetDoc();
                mPDFView.DocLock(true);

                mSelectionStartPage = mPDFView.GetSelectionBeginPage();
                mSelectionEndPage = mPDFView.GetSelectionEndPage();

                for (int pgnm = mSelectionStartPage; pgnm <= mSelectionEndPage; pgnm++)
                {
                    if (!mPDFView.HasSelectionOnPage(pgnm))
                    {
                        continue;
                    }

                    double[] quads = mPDFView.GetSelection(pgnm).GetQuads();
                    int sz = quads.Length / 8;
                    if (sz == 0)
                    {
                        continue;
                    }

                    // for translating points
                    PDFPoint p1 = new PDFPoint();
                    PDFPoint p2 = new PDFPoint();
                    PDFPoint p3 = new PDFPoint();
                    PDFPoint p4 = new PDFPoint();


                    QuadPoint qp = new QuadPoint(p1, p2, p3, p4);
                    PDFRect bbox = new PDFRect(quads[0], quads[1], quads[4], quads[5]); //just use the first quad to temporarily populate the bbox
                    TextMarkup tm;

                    // Annotation settings
                    int r = 255; // default color
                    int g = 0;
                    int b = 0;
                    double opacity = 1; // default opacity

                    if (annotType == Annot.Type.e_Highlight)
                    {
                        tm = Highlight.Create(doc.GetSDFDoc(), bbox);
                        pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.HighlightColor;
                        //SolidColorBrush strokeBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

                        r = col.R;
                        g = col.G;
                        b = col.B;

                        opacity = pdftron.PDF.Tools.Properties.Settings.Default.HighlightOpacity;
                    }
                    else // Underline, Strikeout, and Squiggly share color and opacity settings
                    {
                        pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.TextMarkupColor;
                        //SolidColorBrush strokeBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

                        r = col.R;
                        g = col.G;
                        b = col.B;

                        opacity = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupOpacity;

                        // figure out markup type
                        if (annotType == Annot.Type.e_Underline)
                        {
                            tm = Underline.Create(doc.GetSDFDoc(), bbox);
                        }
                        else if (annotType == Annot.Type.e_StrikeOut)
                        {
                            tm = StrikeOut.Create(doc.GetSDFDoc(), bbox);
                        }
                        else // squiggly
                        {
                            tm = Squiggly.Create(doc.GetSDFDoc(), bbox);
                        }
                    }

                    // Add the quads
                    int k = 0;
                    for (int i = 0; i < sz; ++i, k += 8)
                    {
                        p1.x = quads[k];
                        p1.y = quads[k + 1];

                        p2.x = quads[k + 2];
                        p2.y = quads[k + 3];

                        p3.x = quads[k + 4];
                        p3.y = quads[k + 5];

                        p4.x = quads[k + 6];
                        p4.y = quads[k + 7];

                        qp.p1 = p1;
                        qp.p2 = p2;
                        qp.p3 = p3;
                        qp.p4 = p4;

                        tm.SetQuadPoint(i, qp);
                    }

                    // set color and opacity
                    ColorPt color = new ColorPt(r / 255.0, g / 255.0, b / 255.0);
                    tm.SetColor(color, 3);
                    tm.SetOpacity(opacity);
                    pdftron.PDF.Annot.BorderStyle bStyle = tm.GetBorderStyle();
                    bStyle.width = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupThickness;
                    tm.SetBorderStyle(bStyle);
                    tm.RefreshAppearance();

                    PDFPage page = doc.GetPage(pgnm);
                    page.AnnotPushBack(tm);

                    // add markup to dictionary for later update
                    textMarkupsToUpdate[pgnm] = tm;
                }

                // clear selection
                DeselectAllText();
                this.Children.Clear();
                mPagesOnScreen.Clear();
                mSelectionCanvases.Clear();

            }
            catch (System.Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }

            // Now update the PDFViewCtrl to display our new text markup
            foreach (int pgnm in textMarkupsToUpdate.Keys)
            {
                mPDFView.Update(textMarkupsToUpdate[pgnm], pgnm);
                mToolManager.RaiseAnnotationAddedEvent(textMarkupsToUpdate[pgnm]);
            }
        }


        

        
        
        #endregion Context Menu




        #region Utility Functions

        /// <summary>
        /// Creates a context menu with the options supplied by AddContextMenuItems.
        /// Tools can override AddContextMenuItems in order to supply their own options.
        /// </summary>
        protected void CreateContextMenu(double x, double y)
        {
            mContextMenu = new ContextMenu();
            mViewerCanvas = mToolManager.AnnotationCanvas;
            mViewerCanvas.ContextMenu = mContextMenu;

            AddContextMenuItems(mContextMenu, x, y);

            mContextMenu.IsOpen = true;
            mContextMenu.Closed += mContextMenu_Closed;
        }

        void mContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            mIsContextMenuOpen = false;
        }


        /// <summary>
        /// This will either set the next tool mode to nextMode (is mUseSameToolWhenDone is false)
        /// or it will create a new tool of the same type as the tool that called it.
        /// </summary>
        /// <param name="nextMode">The mode of the next tool</param>
        protected void EndCurrentTool(ToolManager.ToolType nextMode)
        {
            if (mUseSameToolWhenDone)
            {
                mToolManager.CreateTool(mToolMode, this, true);
            }
            else
            {
                mNextToolMode = nextMode;
            }
        }

        /// <summary>
        /// Computes and returns a rectangle with the bounding box of the page indicated by page_num
        /// </summary>
        /// <param name="pageNum">The page number of the page whose bounding box is requested</param>
        /// <returns>A Rect containing the page bounding box</returns>
        internal PDFRect BuildPageBoundBoxOnClient(int pageNum)
        {
            PDFRect rect = null;
            if (pageNum >= 1)
            {
                try
                {
                    mPDFView.DocLockRead();
                    PDFPage page = mPDFView.GetDoc().GetPage(pageNum);
                    if (page == null)
                    {
                        page = mPDFView.GetDoc().GetPage(pageNum);
                    }
                    if (page != null)
                    {
                        rect = new PDFRect();
                        PDFRect r = page.GetCropBox();

                        double x1 = r.x1;
                        double y1 = r.y1;
                        double x2 = r.x2;
                        double y2 = r.y2;
                        
                        // Get coordinates of two opposite points in screen space
                        mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, pageNum);
                        mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, pageNum);

                        double sx = mPDFView.GetHScrollPos();
                        double sy = mPDFView.GetVScrollPos();

                        rect.Set(x1 + sx, y1 + sy, x2 + sx, y2 + sy);
                        rect.Normalize();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlockRead();
                }
            }
            return rect;
        }

        /// <summary>
        /// Finds an annotation (if available) that is in screen space (x, y)
        /// And sets mAnnot, mAnnotBBox, and mAnnotPageNum accordingly
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected void SelectAnnot(int x, int y)
        {
            mAnnot = null;
            mAnnotPageNum = 0;

            PDFDoc doc = mPDFView.GetDoc();
            if (doc != null)
            {
                try
                {
                    pdftron.SDF.Obj obj = mPDFView.GetAnnotationAt(x, y);
                    if (obj != null)
                    {
                        Annot a = new Annot(obj);
                        if (a != null && a.IsValid())
                        {
                            mAnnot = a;
                            BuildAnnotBBox();
                            mAnnotPageNum = mPDFView.GetPageNumberFromScreenPt(x, y);
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    //mPDFView.DocUnlock();
                }
            }
        }

        /// <summary>
        /// Build the bounding box for the annotation (mAnnotBBox)
        /// The rectangle is in page space.
        /// </summary>
        protected void BuildAnnotBBox()
        {
            if (mAnnot != null)
            {
                mAnnotBBox = new PDFRect(0, 0, 0, 0);
                try
                {
                    PDFRect r = mAnnot.GetRect();
                    mAnnotBBox.Set(r.x1, r.y1, r.x2, r.y2);
                    mAnnotBBox.Normalize();
                }
                catch (Exception)
                {
                }
            }
        }

        protected void GetModifierKeyStates()
        {
            mIsCtrlDown = ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control);

            mIsShiftDown = ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift);

            mIsAltDown = ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt);

            mIsModifierKeyDown = mIsCtrlDown || mIsShiftDown || mIsAltDown;

            pdftron.PDF.PDFViewWPF.PagePresentationMode presentationMode = mPDFView.GetPagePresentationMode();
            mIsContinuousMode = (presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing_continuous ||
                        presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing_continuous_cover ||
                        presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_single_continuous);
        }

        protected double GetLineEndingLength(double strokThickenss)
        {
            return 3.5 * (Math.Pow(strokThickenss, 0.65) + strokThickenss);
        }

        /// <summary>
        /// Returns all page numbers that are (partially) inside the rectangle
        /// </summary>
        /// <param name="rect">the rectangle in PDFViewCtrl space</param>
        /// <returns>A list of indexes of pages inside the rect.</returns>
        protected List<int> GetPagesInRect(PDFRect rect)
        {
            // TODO: Make more powerful, for now, we just return one page.
            List<int> retList = new List<int>();
            int pageNum1 = mPDFView.GetPageNumberFromScreenPt(rect.x1, rect.y1);
            int pageNum2 = mPDFView.GetPageNumberFromScreenPt(rect.x2, rect.y2);

            if (pageNum1 < 1 && pageNum2 < 1)
            {
                retList.Add(mPDFView.GetCurrentPage());
            }
            else if (pageNum2 > 0)
            {
                retList.Add(pageNum2);
            }
            else
            {
                retList.Add(pageNum1);
            }

            return retList;
        }

        /// <summary>
        /// Converts a rectangle from page space to canvas space
        /// 
        /// Note: This is the canvas space of PDFViewCtrl.GetCanvas.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="pageNumber"></param>
        internal void ConvertPageRectToCanvasRect(PDFRect rect, int pageNumber)
        {
            double x1 = rect.x1;
            double y1 = rect.y1;
            double x2 = rect.x2;
            double y2 = rect.y2;

            mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, pageNumber);
            mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, pageNumber);

            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            rect.Set(x1 + sx, y1 + sy, x2 + sx, y2 + sy);
            rect.Normalize();
        }

        /// <summary>
        /// Converts a rectangle from canvas space to page space
        /// 
        /// Note: This is the canvas space of PDFViewCtrl.GetCanvas.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="pageNumber"></param>
        internal void ConvertCanvasRectToPageRect(PDFRect rect, int pageNumber)
        {

            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            double x1 = rect.x1 - sx;
            double y1 = rect.y1 - sy;
            double x2 = rect.x2 - sx;
            double y2 = rect.y2 - sy;

            mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, pageNumber);
            mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, pageNumber);

            rect.Set(x1, y1, x2, y2);
            rect.Normalize();
        }

        /// <summary>
        /// Converts a rectangle from page space to screen space
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="pageNumber"></param>
        internal void ConvertPageRectToScreenRect(PDFRect rect, int pageNumber)
        {
            double x1 = rect.x1;
            double y1 = rect.y1;
            double x2 = rect.x2;
            double y2 = rect.y2;

            mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, pageNumber);
            mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, pageNumber);

            rect.Set(x1, y1, x2, y2);
            rect.Normalize();
        }

        /// <summary>
        /// Converts a rectangle from screen space to page space
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="pageNumber"></param>
        internal void ConvertScreenRectToPageRect(PDFRect rect, int pageNumber)
        {
            double x1 = rect.x1;
            double y1 = rect.y1;
            double x2 = rect.x2;
            double y2 = rect.y2;

            mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, pageNumber);
            mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, pageNumber);

            rect.Set(x1, y1, x2, y2);
            rect.Normalize();
        }

        // Gets the distance between two UI points
        protected double GetDistance(UIPoint p1, UIPoint p2)
        {
            return Math.Sqrt(((p1.X - p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }


        public static UIPoint GetPosition(InputEventArgs e, IInputElement relativeTo)
        {
            if (e is MouseEventArgs)
            {
                MouseEventArgs me = e as MouseEventArgs;
                return me.GetPosition(relativeTo);
            }
            if (e is TouchEventArgs)
            {
                TouchEventArgs te = e as TouchEventArgs;
                return te.GetTouchPoint(relativeTo).Position;
                
            }
            if (e is StylusEventArgs)
            {
                StylusEventArgs te = e as StylusEventArgs;
                return te.GetPosition(null);

            }
            throw new ArgumentException("Parameter must be of type MouseEventArgs or TouchEventArgs");
        }


        #endregion Utility Functions



        #region Handle Selected Text

        public void TextSelectionHasChanged()
        {
            mToolManager.TextSelectionCanvas.Children.Clear();
            mPagesOnScreen.Clear();
            mSelectionCanvases.Clear();
            DrawSelection();
        }



        /// <summary>
        /// Call this funtion when a tool is created to clear text selection and suppress any context menu options related to it.
        /// </summary>
        protected void DisallowTextSelection()
        {
            DeselectAllText();
            mAllowTextSelectionOptions = false;
        }


        /// <summary>
        /// This functions draws the current text selection to the screen.
        /// To only include text selection where it is visible, we draw all quads for a
        /// specific page on one canvas in page space. This canvas can then be positioned,
        /// rotated, scaled, and added or removed based on the state of the viewer.
        /// </summary>
        /// <param name="reposition">If true, everything will be repositoned. Set to true after zoom or changing view 
        /// mode when every canvas has to be positioned</param>
        public void DrawSelection(bool reposition = false)
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();
            mSelectionStartPage = mPDFView.GetSelectionBeginPage();
            mSelectionEndPage = mPDFView.GetSelectionEndPage();
            int[] pgnums = mPDFView.GetVisiblePages();
            List<int> pagesOnScreen;
            if (pgnums == null)
            {
                pagesOnScreen = new List<int>();
            }
            else
            {
                pagesOnScreen = new List<int>(pgnums);
            }
            List<int> addedPages = GetPageDifference(pagesOnScreen, mPagesOnScreen);
            List<int> removedPages = GetPageDifference(mPagesOnScreen, pagesOnScreen);
            mPagesOnScreen = pagesOnScreen;

            // Pages removed from screen should be detached
            foreach (int pgnm in removedPages)
            {
                if (mSelectionCanvases.ContainsKey(pgnm) && mSelectionCanvases[pgnm].IsAttached)
                {
                    SelectionDrawing sd = mSelectionCanvases[pgnm];
                    sd.IsAttached = false;
                    mToolManager.TextSelectionCanvas.Children.Remove(mSelectionCanvases[pgnm].Canvas);
                }
            }

            try
            {
                mPDFView.DocLockRead();
                foreach (int pgnm in addedPages)
                {
                    if (!mPDFView.HasSelectionOnPage(pgnm))
                    {
                        continue;
                    }

                    // if we already have the page set up, we just need to stick it back in and position it.
                    if (mSelectionCanvases.ContainsKey(pgnm))
                    {
                        SelectionDrawing sd = mSelectionCanvases[pgnm];
                        sd.IsAttached = true;
                        mToolManager.TextSelectionCanvas.Children.Add(mSelectionCanvases[pgnm].Canvas);
                        PositionPageCanvas(pgnm, mSelectionCanvases[pgnm].Canvas);
                        continue;
                    }

                    // create a new page for selection, at normalized scale
                    SelectionDrawing selDrawing = new SelectionDrawing();
                    selDrawing.IsAttached = true;
                    selDrawing.Canvas = new Canvas();
                    selDrawing.Canvas.IsHitTestVisible = false;

                    pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(pgnm);
                    selDrawing.PageHeight = page.GetCropBox().Height();
                    selDrawing.Quads = new List<PDFRect>();

                    mSelectionCanvases[pgnm] = selDrawing;
                    mToolManager.TextSelectionCanvas.Children.Add(selDrawing.Canvas);
                    PositionPageCanvas(pgnm, selDrawing.Canvas);


                    PDFViewWPF.Selection sel = mPDFView.GetSelection(pgnm);

                    double[] quads = sel.GetQuads();
                    int sz = quads.Length / 8;

                    int k = 0;
                    PDFRect drawRect;

                    // each quad consists of 8 consecutive points
                    for (int i = 0; i < sz; ++i, k += 8)
                    {
                        drawRect = new PDFRect(quads[k], selDrawing.PageHeight - quads[k + 1], quads[k + 4], selDrawing.PageHeight - quads[k + 5]);
                        drawRect.Normalize();

                        // draw rectangle on selected text
                        Rectangle rect = new Rectangle();
                        rect.SetValue(Canvas.LeftProperty, drawRect.x1);
                        rect.SetValue(Canvas.TopProperty, drawRect.y1);
                        rect.Width = drawRect.x2 - drawRect.x1;
                        rect.Height = drawRect.y2 - drawRect.y1;
                        rect.Fill = mTextSelectionBrush;

                        // This will add the rectangle to the screen
                        selDrawing.Canvas.Children.Add(rect);
                        selDrawing.Quads.Add(drawRect);
                    }
                }

                // We need to reposition pages that remained on the screen
                if (reposition)
                {
                    foreach (int pgnm in pagesOnScreen)
                    {
                        if (mSelectionCanvases.ContainsKey(pgnm) && !addedPages.Contains(pgnm))
                        {
                            PositionPageCanvas(pgnm, mSelectionCanvases[pgnm].Canvas);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                mPDFView.DocUnlockRead();
            }
        }

        /// <summary>
        /// This function position a page cancas so that it overlays the canvas in the PDFViewCtrl.
        /// Scaling and rotation is taken care of here too.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="canvas"></param>
        private void PositionPageCanvas(int pageNumber, Canvas canvas)
        {
            TransformGroup transGroup = new TransformGroup();

            pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(pageNumber);
            double x = 0;
            double y = page.GetCropBox().Height();
            int rotation = ((int)page.GetRotation() + (int)mPDFView.GetRotation()) % 4;

            mPDFView.ConvPagePtToScreenPt(ref x, ref y, pageNumber);
            x += mPDFView.GetHScrollPos();
            y += mPDFView.GetVScrollPos();
            canvas.SetValue(Canvas.LeftProperty, x);
            canvas.SetValue(Canvas.TopProperty, y);

            // Selection Canvases are always created in page space, assuming a scale factor of 1.
            // So we need to scale it
            ScaleTransform st = new ScaleTransform();
            st.ScaleX = mPDFView.GetZoom();
            st.ScaleY = mPDFView.GetZoom();
            RotateTransform rt = new RotateTransform(rotation * 90);
            System.Windows.Media.MatrixTransform mt = new MatrixTransform(rt.Value * st.Value);
            canvas.RenderTransform = mt;
        }


        /// <summary>
        /// Figures out if the point is inside one of the rectangles representing our selection
        /// </summary>
        /// <param name="p">The point we're looking for</param>
        /// <returns>True if the point is inside one of the rectangles</returns>
        protected bool IsPointInSelection(UIPoint p)
        {
            int pgnm = mPDFView.GetPageNumberFromScreenPt(p.X, p.Y);
            if (mSelectionCanvases.ContainsKey(pgnm))
            {
                double x = p.X;
                double y = p.Y;
                mPDFView.ConvScreenPtToPagePt(ref x, ref y, pgnm);

                y = mSelectionCanvases[pgnm].PageHeight - y;
                foreach (PDFRect rect in mSelectionCanvases[pgnm].Quads)
                {
                    if (rect.Contains(x, y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a list of all integers in pageList1 that are not in pageList2
        /// </summary>
        /// <param name="pageList1"></param>
        /// <param name="pageList2"></param>
        /// <returns></returns>
        protected List<int> GetPageDifference(List<int> pageList1, List<int> pageList2)
        {
            List<int> difference = new List<int>();

            foreach (int page in pageList1)
            {
                if (!pageList2.Contains(page))
                {
                    difference.Add(page);
                }
            }
            return difference;
        }

        /// <summary>
        /// Copies the current text selection to the clipboard as a unicode string.
        /// </summary>
        protected void CopySelectedTextToClipBoard()
        {
            string text = "";

            // Extract selected text
            if (mPDFView.HasSelection())
            {
                mSelectionStartPage = mPDFView.GetSelectionBeginPage();
                mSelectionEndPage = mPDFView.GetSelectionEndPage();
                if (mSelectionStartPage > 0)
                {
                    for (int pgnm = mSelectionStartPage; pgnm <= mSelectionEndPage; pgnm++)
                    {
                        if (mPDFView.HasSelectionOnPage(pgnm))
                        {
                            text += mPDFView.GetSelection(pgnm).GetAsUnicode();
                        }
                    }
                }
            }

            Clipboard.SetData(DataFormats.UnicodeText, text);
        }


        /// <summary>
        /// Helper function to selct all text.
        /// </summary>
        protected void SelectAllText()
        {
            mPDFView.SelectAll();
            mToolManager.TextSelectionCanvas.Children.Clear();
            mPagesOnScreen.Clear();
            mSelectionCanvases.Clear();
            DrawSelection();
        }

        /// <summary>
        /// Helper function to deselct all text.
        /// </summary>
        protected void DeselectAllText()
        {
            if (mPDFView.HasSelection())
            {
                mPDFView.ClearSelection();
                mToolManager.TextSelectionCanvas.Children.Clear();
                mPagesOnScreen.Clear();
                mSelectionCanvases.Clear();
                DrawSelection();
            }
        }

        /// <summary>
        /// Clears all text highlighing without changing the selction.
        /// </summary>
        public void ClearAllTextHighlights()
        {
            mToolManager.TextSelectionCanvas.Children.Clear();
            mPagesOnScreen.Clear();
            mSelectionCanvases.Clear();
        }

        #endregion Handle Selected Text

    }
}