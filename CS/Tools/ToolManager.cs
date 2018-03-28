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
using System.Windows.Threading;
using PDFViewWPFToolsCS2013.Utilities;

namespace pdftron.PDF.Tools
{
    
    public delegate void AnnotationModificationHandler(Annot annotation);
    public delegate void AnnotationSelectionChanged();

    public delegate void ToolModeChangedHandler(ToolManager.ToolType newToolType);


    /// <summary>
    /// This is the main class that manages all the tools that interact directly with the viewer through user input.
    /// Generally, this class should be the only point at which a user should interact with the tools library,
    /// although there are Exceptions, such as when the FreeHand tool is used to draw shapes with multiple strokes.
    /// </summary>
    public class ToolManager
    {
        /// <summary>
        /// The various types of tools available for use.
        /// </summary>
        public enum ToolType
        {
            e_none, e_pan, e_annot_edit, e_line_create, e_arrow_create,
            e_rect_create, e_oval_create, e_ink_create, e_text_annot_create, e_link_action,
            e_text_select, e_text_select_rectangular, e_form_fill, e_sticky_note_create,
            e_text_highlight, e_text_underline, e_text_strikeout, e_text_squiggly, e_line_edit,
        };



        private bool mSuppressContextMenu = false;
        /// <summary>
        /// Sets whether or not the tool should bring up a context menu on right clicks.
        /// Use this in conjugation with PDFViewWPF.PreviewMouseRightButtonUp in order to
        /// determine at runtime whether or not to show the menu.
        /// </summary>
        public bool SuppressContextMenu
        {
            set { mSuppressContextMenu = value; }
            internal get { return mSuppressContextMenu; }
        }

        private bool mIsRightToLeftLanguage = false;
        /// <summary>
        /// Sets whether or not the language is right to left.
        /// </summary>
        public bool IsRightToLeftLanguage
        {
            internal get { return mIsRightToLeftLanguage; }
            set { mIsRightToLeftLanguage = value; }
        }

        private List<Annot> mCurrentCopiedAnnotations;
        /// <summary>
        /// Gets a list of currently copied annotations.
        /// </summary>
        public List<Annot> CurrentCopiedAnnotations
        {
            get { return mCurrentCopiedAnnotations; }
        }

        private NoteManager mNoteManager;
        /// <summary>
        /// Gets the class that displays and manages the notes for various annotations.
        /// </summary>
        internal NoteManager NoteManager
        {
            get { return mNoteManager; }
        }

        private PDFViewWPF mPDFView;
        private Tool mCurrentTool;
        /// <summary>
        /// Returns the tool that is currently active.
        /// </summary>
        public Tool CurrentTool
        {
            get { return mCurrentTool; }
        }

        private Canvas mAnnotationCanvas;
        /// <summary>
        /// Gets the canvas on which the tools draw temporary annotation shapes, adds widgets for form filling, and more.
        /// </summary>
        public Canvas AnnotationCanvas
        {
            get { return mAnnotationCanvas; }
        }

        private Canvas mTextSelectionCanvas;
        /// <summary>
        /// Gets the canvas on which tools draw text selection.
        /// </summary>
        public Canvas TextSelectionCanvas
        {
            get { return mTextSelectionCanvas; }
        }

        /// <summary>
        /// Returns the currently selected annotations.
        /// </summary>
        public List<Annot> SelectedAnnotations
        {
            get
            {
                AnnotEdit editTool = CurrentTool as AnnotEdit;
                if (editTool != null)
                {
                    return editTool.SelectedAnnotations;
                }
                return new List<Annot>();
            }
        }

        /// <summary>
        /// This event is fired every time the tool mode is changed.
        /// </summary>
        public event ToolModeChangedHandler ToolModeChanged;

        /// <summary>
        /// This event is fired when an annotation has been added to a document
        /// </summary>
        public event AnnotationModificationHandler AnnotationAdded;

        /// <summary>
        /// This event is raised whenever an annotation on the current document has been edited
        /// </summary>
        public event AnnotationModificationHandler AnnotationEdited;

        /// <summary>
        /// This event is raised whenever an annotation has been deleted from the document
        /// </summary>
        public event AnnotationModificationHandler AnnotationRemoved;

        /// <summary>
        /// This event is raised when the selected annotations change
        /// </summary>
        public event AnnotationSelectionChanged SelectedAnnotationsChanged;

        /// <summary>
        /// This event is raised whenever an annotation on the current document has been edited
        /// </summary>
        public event AnnotationModificationHandler AnnotationNoteChanged;

        private bool _IsEnabled = true;
        /// <summary>
        /// Gets or sets whether the tools respond to interaction from the PDFViewWPF
        /// </summary>
        public bool IsEnabled { get { return _IsEnabled; } set { _IsEnabled = value; } }

        //////////////////////////////////////////////////////////////////////////
        // Generating click (tap) events
        protected bool mShallClick = false;
        protected UIPoint mClickPoint;
        protected const double CLICK_THRESHOLD = 4;


        //////////////////////////////////////////////////////////////////////////
        // Touch interaction
        private IList<int> mTouchIDs;

        /// <summary>
        /// A list of touch ID's currently on the screen
        /// </summary>
        public IList<int> TouchIDs
        {
            get
            {
                if (mTouchIDs == null)
                {
                    mTouchIDs = new List<int>();
                }
                return mTouchIDs;
            }
        }
        //fading ink mode
        // long press and tap
        public double TOUCH_PRESS_DIST_THRESHOLD = 25;
        public double LONG_PRESS_TIME_THRESHOLD = 0.8;
        public double TAP_TIME_THRESHOLD = 0.3;
        public double TAP_Circle_Radius = 25;
        private DateTime _LongPressStartTime;
        private UIPoint _LongPressDownPoint;
        private bool _TouchPressAborted; // too much movement, or a second finger.
        public double Stylus_PRESS_DIST_THRESHOLD = 25;

        // text selection scroll speeds when dragging to edge.
        // the speed with which we scroll
        public double TEXT_SELECT_SCROLL_SPEED_X = 20;
        public double TEXT_SELECT_SCROLL_SPEED_Y = 50;
        // speed increases linearly if within this margin
        public double TEXT_SELECT_SCROLL_MARGIN_X = 4;
        public double TEXT_SELECT_SCROLL_MARGIN_Y = 50;
        // once margin is passed, increase speed by factor
        public double TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_X = 8;
        public double TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_Y = 4;

        /// <summary>
        /// Creates a ToolManager that will attach to view and provide interactive behavior
        /// </summary>
        /// <param name="view">The PDFViewWPF to attach to the ToolManager</param>
        public ToolManager(PDFViewWPF view)
        {
            mPDFView = view;
            mCurrentCopiedAnnotations = new List<Annot>();
            mAnnotationCanvas = new Canvas();

            SubscribeToolManager();
            Canvas viewerCanvas = mPDFView.GetCanvas();
            viewerCanvas.Children.Add(mAnnotationCanvas);
            System.Windows.Data.Binding binder = new System.Windows.Data.Binding("ActualWidth");
            binder.Source = viewerCanvas;
            mAnnotationCanvas.SetBinding(Canvas.WidthProperty, binder);
            binder = new System.Windows.Data.Binding("ActualHeight");
            binder.Source = viewerCanvas;
            mAnnotationCanvas.SetBinding(Canvas.HeightProperty, binder);

            mTextSelectionCanvas = new Canvas();
            viewerCanvas.Children.Add(mTextSelectionCanvas);

            mNoteManager = new NoteManager(mPDFView, this);

            mDelayRemoveTimers = new List<DelayRemoveTimer>();

            CreateTool(ToolType.e_pan, null);
        }

        /// <summary>
        /// Subscribes the toolmanager to any events it needs.
        /// </summary>
        private void SubscribeToolManager()
        {
            Canvas drawCanvas = mPDFView.GetCanvas();
            //mPDFView.MouseLeftButtonDown += PDFView_MouseLeftButtonDown;
            //mPDFView.MouseRightButtonDown += PDFView_MouseRightButtonDown;
            //mPDFView.MouseLeftButtonUp += PDFView_MouseLeftButtonUp;
            //mPDFView.MouseRightButtonUp += PDFView_MouseRightButtonUp;
            //mPDFView.MouseMove += PDFView_MouseMove;

            //mPDFView.PreviewMouseWheel += PDFView_PreviewMouseWheel;
            //mPDFView.MouseWheel += PDFView_MouseWheel;

            //mPDFView.MouseEnter += PDFView_MouseEnter;
            //mPDFView.MouseLeave += PDFView_MouseLeave;

            //mPDFView.PreviewKeyDown += PDFView_KeyDown;
            //mPDFView.PreviewKeyUp += PDFView_KeyUp;

            //mPDFView.PreviewMouseDoubleClick += PDFView_PreviewMouseDoubleClick;
            //mPDFView.MouseDoubleClick += PDFView_MouseDoubleClick;
            //mPDFView.PreviewMouseRightButtonUp += PDFView_PreviewMouseRightButtonUp;

            //timer = new DispatcherTimer();
            //timer.Tick += timer_Tick;
            //timer.Interval = TimeSpan.FromMilliseconds(500);

            mPDFView.CurrentZoomChanged += PDFView_CurrentZoomChanged;
            mPDFView.LayoutChanged += PDFView_LayoutChanged;
            mPDFView.CurrentPageNumberChanged += PDFView_CurrentPageNumberChanged;
            mPDFView.CurrentScrollChanged += PDFView_CurrentScrollChanged;
            //mPDFView.On

          

            // touch events
            mPDFView.TouchDown += mPDFView_TouchDown;
            mPDFView.TouchMove += mPDFView_TouchMove;
            mPDFView.TouchUp += mPDFView_TouchUp;
            mPDFView.LostTouchCapture += mPDFView_LostTouchCapture;
            //mPDFView.Touch

            // stylus events
            mPDFView.StylusDown += mPDFView_StylusDown;
            mPDFView.StylusMove += mPDFView_StylusMove;
            mPDFView.StylusUp += mPDFView_StylusUp;
            // mPDFView.LostStylusCapture += mPDFView_LostStylusCapture;
            //mPDFView.Stylus

            //mPDFView.PreviewMouseDown += mPDFView_PreviewMouseDown;
            //mPDFView.PreviewMouseMove += mPDFView_PreviewMouseMove;
            //mPDFView.PreviewMouseUp += mPDFView_PreviewMouseUp;

            mPDFView.OnRenderFinished += mPDFView_OnRenderFinished;
        }
        //private void timer_Tick(object sender, EventArgs e)
        //{
        //    (sender as DispatcherTimer).Stop();
        //    //Trace.WriteLine("Scrolling stopped ("+somevalue+")");
        //    FadingOn = true;
        //    somevalue = 0;
        //}

        private void Timer_Tick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void mPDFView_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Evaluate(e);
        }

        void mPDFView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Evaluate(e);
        }

        void mPDFView_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Evaluate(e);
        }

        private void Evaluate(System.Windows.Input.MouseEventArgs e)
        {
            if (e.StylusDevice != null)
            {
                e.Handled = true;
            }
        }

        void mPDFView_ManipulationStarting(object sender, System.Windows.Input.ManipulationStartingEventArgs e)
        {
        }

        void mPDFView_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
        }

        void mPDFView_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
        }


        /// <summary>
        /// Unsubscribes the tool manager.
        /// </summary>
        private void UnsubscribeToolManager()
        {
            //mPDFView.MouseLeftButtonDown -= PDFView_MouseLeftButtonDown;
            //mPDFView.MouseRightButtonDown -= PDFView_MouseRightButtonDown;
            //mPDFView.MouseLeftButtonUp -= PDFView_MouseLeftButtonUp;
            //mPDFView.MouseRightButtonUp -= PDFView_MouseRightButtonUp;
            //mPDFView.MouseMove -= PDFView_MouseMove;

            //mPDFView.MouseEnter -= PDFView_MouseEnter;
            //mPDFView.MouseLeave -= PDFView_MouseLeave;

            //mPDFView.PreviewMouseWheel -= PDFView_PreviewMouseWheel;
            //mPDFView.MouseWheel -= PDFView_MouseWheel;

            //mPDFView.PreviewKeyDown -= PDFView_KeyDown;
            //mPDFView.PreviewKeyUp -= PDFView_KeyUp;

            //mPDFView.PreviewMouseDoubleClick -= PDFView_PreviewMouseDoubleClick;
            //mPDFView.MouseDoubleClick -= PDFView_MouseDoubleClick;
            //mPDFView.PreviewMouseRightButtonUp -= PDFView_PreviewMouseRightButtonUp;

            mPDFView.CurrentZoomChanged -= PDFView_CurrentZoomChanged;
            mPDFView.LayoutChanged -= PDFView_LayoutChanged;
            mPDFView.CurrentPageNumberChanged -= PDFView_CurrentPageNumberChanged;
            mPDFView.CurrentScrollChanged -= PDFView_CurrentScrollChanged;


            // touch events
            mPDFView.TouchDown -= mPDFView_TouchDown;
            mPDFView.TouchMove -= mPDFView_TouchMove;
            mPDFView.TouchUp -= mPDFView_TouchUp;
            // stylus events
            mPDFView.StylusDown -= mPDFView_StylusDown;
            mPDFView.StylusMove -= mPDFView_StylusMove;
            mPDFView.StylusUp -= mPDFView_StylusUp;
            //mPDFView.Stylus

            //mPDFView.PreviewMouseDown -= mPDFView_PreviewMouseDown;
            //mPDFView.PreviewMouseMove -= mPDFView_PreviewMouseMove;
            //mPDFView.PreviewMouseUp -= mPDFView_PreviewMouseUp;

            mPDFView.OnRenderFinished -= mPDFView_OnRenderFinished;

            CleanUpTimers();
        }


        /// <summary>
        /// Creates a new tool of type mode
        /// </summary>
        /// <param name="mode">The type of tool to create</param>
        /// <returns>The newly created tool</returns>
        public Tool CreateTool(ToolType mode)
        {
            return CreateTool(mode, null);
        }

        /// <summary>
        /// Creates a new tool of type mode
        /// </summary>
        /// <param name="mode">The type of tool to create</param>
        /// <param name="current_tool">The current tool that is used. This will transfer some information to the new tool. </param>
        /// <returns>The newly created tool</returns>
        public Tool CreateTool(ToolType mode, Tool current_tool)
        {
            return CreateTool(mode, current_tool, false);
        }

        /// <summary>
        /// Creates a new tool of type mode
        /// </summary>
        /// <param name="mode">The type of tool to create</param>
        /// <param name="current_tool">The current tool that is used. This will transfer some information to the new tool. </param>
        /// <param name="toolIsPersistent">True if the tool will persists (e.g. you can draw multiple lines without reselecting the tool.
        /// false (default) otherwise.</param>
        /// <returns>The newly created tool</returns>
        public Tool CreateTool(ToolType mode, Tool current_tool, bool toolIsPersistent)
        {
            Tool t = null;

            switch (mode)
            {
                case ToolType.e_pan:
                    t = new Pan(mPDFView, this);
                    break;
                case ToolType.e_annot_edit:
                    t = new AnnotEdit(mPDFView, this);
                    break;
                case ToolType.e_line_create:
                    t = new LineCreate(mPDFView, this);
                    break;
                case ToolType.e_arrow_create:
                    t = new ArrowCreate(mPDFView, this);
                    break;
                case ToolType.e_rect_create:
                    t = new RectCreate(mPDFView, this);
                    break;
                case ToolType.e_oval_create:
                    t = new OvalCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_ink_create:
                    t = new FreehandCreate(mPDFView, this);
                    Trace.WriteLine("freehand creat");
                    break;
                case ToolManager.ToolType.e_text_annot_create:
                    t = new FreeTextCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_link_action:
                    t = new LinkAction(mPDFView, this);
                    break;
                case ToolType.e_text_select:
                    t = new TextSelectStructural(mPDFView, this);
                    break;
                case ToolType.e_text_select_rectangular:
                    t = new TextSelectRectangular(mPDFView, this);
                    break;
                case ToolType.e_form_fill:
                    t = new FormFill(mPDFView, this);
                    break;
                case ToolType.e_sticky_note_create:
                    t = new StickyNoteCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_text_highlight:
                    t = new TextHightlightCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_text_underline:
                    t = new TextUnderlineCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_text_strikeout:
                    t = new TextStrikeoutCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_text_squiggly:
                    t = new TextSquigglyCreate(mPDFView, this);
                    break;
                case ToolManager.ToolType.e_none:
                    t = null;
                    break;
                default:
                    t = new Pan(mPDFView, this);
                    break;
            }

            if (current_tool != null)
            {
                // Transfer some data between tools
                if (t != null)
                {
                    t.Transfer((Tool)current_tool);
                }
                current_tool.OnClose();		//close the old tool; old tool can use this to clean up things.
            }

            // This can happen when the user explicitly creates new tools
            if (mCurrentTool != null && mCurrentTool != current_tool)
            {
                mCurrentTool.OnClose();
            }

            //call a tool's onCreate() function in which the tool can initialize things that require the transferred properties. 
            if (t != null)
            {
                t.OnCreate();
                t.UseSameToolWhenDone = toolIsPersistent;
            }

            mCurrentTool = t;
            if (ToolModeChanged != null)
            {
                ToolModeChanged(CurrentTool.ToolMode);
            }

            return t;
        }

        /// <summary>
        /// Tells the tools to remove any highlighted text from text selection
        /// </summary>
        public void ClearSelectedTextHighlighting()
        {
            if (CurrentTool != null)
            {
                CurrentTool.ClearAllTextHighlights();
            }
        }

        #region Annotation Modification

        /// <summary>
        /// Lets the various tools raise the AnnotationAdded event from a unified location.
        /// </summary>
        /// <param name="annot"></param>
        internal void RaiseAnnotationAddedEvent(Annot annot)
        {
            if (AnnotationAdded != null)
            {
                AnnotationAdded(annot);
            }
        }

        /// <summary>
        /// Lets the various tools raise the AnnotationEdited event from a unified location.
        /// </summary>
        /// <param name="annot"></param>
        internal void RaiseAnnotationEditedEvent(Annot annot)
        {
            if (AnnotationEdited != null)
            {
                AnnotationEdited(annot);
            }
        }

        /// <summary>
        /// Lets the various tools raise the AnnotationRemoved event from a unified location.
        /// </summary>
        /// <param name="annot"></param>
        internal void RaiseAnnotationRemovedEvent(Annot annot)
        {
            if (AnnotationRemoved != null)
            {
                AnnotationRemoved(annot);
            }
        }

        /// <summary>
        /// This lets the user AnnotEdit tool raise an event when the selection is changed.
        /// </summary>
        internal void RaiseSelectedAnnotationsChangedEvent()
        {
            if (SelectedAnnotationsChanged != null)
            {
                SelectedAnnotationsChanged();
            }
        }

        /// <summary>
        /// Lets the various tools raise the AnnotationEdited event from a unified location.
        /// </summary>
        /// <param name="annot"></param>
        internal void RaiseAnnotationNoteChangedEvent(Annot annot)
        {
            if (AnnotationNoteChanged != null)
            {
                AnnotationNoteChanged(annot);
            }
        }

        #endregion Annotation Modification


        #region Event Handlers
        //////////////////////////////////////////////////////////////////////////
        // All events that the ToolManager gets follows the same pattern.
        // First, the event is forwarded to the current tool. 
        // Once the current tool is finished, we look to see if the next tool mode
        // is the same as the current tool mode. This gives the current tool a 
        // chance to process the event, and if it detects that another tool should
        // handle this event, it can set the next tool mode to the appropriate
        // tool.
        // This is done for example when the Pan tool handles a MouseLeftButtonDown
        // event and notices a form field at the current cursor location.
        //////////////////////////////////////////////////////////////////////////

        #region Mouse Events      

        void PDFView_CurrentZoomChanged(PDFViewWPF viewer)
        {
            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            //        mCurrentTool.ZoomChangedHandler(viewer, new RoutedEventArgs());
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }


        void PDFView_LayoutChanged(PDFViewWPF viewer)
        {
            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            //        mCurrentTool.LayoutChangedHandler(viewer, new RoutedEventArgs());
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }


        void PDFView_CurrentPageNumberChanged(PDFViewWPF viewer, int currentPage, int totalPages)
        {
            RemoveTimersOnHiddenPages(currentPage);

            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            //        mCurrentTool.CurrentPageNumberChangedHandler(viewer, currentPage, totalPages);
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }


        void PDFView_CurrentScrollChanged(PDFViewWPF viewer, ScrollChangedEventArgs e)
        {
            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            //        mCurrentTool.CurrentScrollChangedHandler(viewer, e);
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
            //somevalue += e.VerticalChange;
            //timer.Stop();
            //timer.Start();
            //FadingOn = false;
            //Trace.WriteLine("scorll changed");
        }

        #endregion Mouse Event
        //////////////////////////////////////////////////////////////////////////
        // Touch events
        bool AlreadySwiped = false;
        protected TouchPoint TouchStart;
        void mPDFView_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (mPDFView.GetDoc() != null)
            {
                TouchIDs.Add(e.TouchDevice.Id);
                mPDFView.IsManipulationEnabled = true;
                if (TouchIDs.Count == 1)
                {
                    _LongPressDownPoint = e.GetTouchPoint(null).Position;
                    _LongPressStartTime = DateTime.Now;
                    _TouchPressAborted = false;
                }
                else
                {
                    _TouchPressAborted = true;
                }
                //if (mCurrentTool != null && _IsEnabled)
                //{
                //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
                //    ToolManager.ToolType next_tm;
                //    while (true)
                //    {
                //        mCurrentTool.TouchDownHandler(sender, e);
                //        next_tm = mCurrentTool.NextToolMode;
                //        if (prev_tm != next_tm)
                //        {
                //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
                //            prev_tm = next_tm;
                //        }
                //        else
                //        {
                //            break;
                //        }
                //    }
                //}

                TouchStart = e.GetTouchPoint(mPDFView);
            }
        }

        double PointDistance(UIPoint A, UIPoint B)
        {
            double xDist = A.X - B.X;
            double yDist = A.Y - B.Y;
            double pointDistance = (xDist * xDist) + (yDist * yDist);
            return pointDistance;
        }
        void mPDFView_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (mPDFView.GetDoc() != null)
            {
                UIPoint movePoint = e.GetTouchPoint(null).Position;
            _TouchPressAborted = PointDistance(movePoint, _LongPressDownPoint) > TOUCH_PRESS_DIST_THRESHOLD;
            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            //        mCurrentTool.TouchMoveHandler(sender, e);
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }
        }
        UIPoint? _lastTapLocation;
        private readonly Stopwatch _doubleTapStopWatch = new Stopwatch();
        private bool IsDoubleTap()
        {
            UIPoint currentTapPosition = TouchStart.Position;
            bool tapAreCloseInDistance = false;
            if (_lastTapLocation != null)
            {
                //Console.WriteLine("distance is  " + PointDistance(currentTapPosition, (UIPoint)_lastTapLocation));
                tapAreCloseInDistance = PointDistance(currentTapPosition, (UIPoint)_lastTapLocation) < 500;
            }
            _lastTapLocation = currentTapPosition;
            TimeSpan elapsed = _doubleTapStopWatch.Elapsed;
            _doubleTapStopWatch.Restart();
            bool tapsAreCloseInTime = (elapsed != TimeSpan.Zero && elapsed < TimeSpan.FromSeconds(2));
            if (tapAreCloseInDistance && tapsAreCloseInTime)
            {
                _lastTapLocation = null;
            }
           Trace.WriteLine("tapAreCloseInDistance "+ tapAreCloseInDistance+ "tapsAreCloseInTime "+ tapsAreCloseInTime);

            return tapAreCloseInDistance && tapsAreCloseInTime;
        }

        void mPDFView_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (mPDFView.GetDoc() != null)
            {
            if (!_TouchPressAborted)
            {

                _TouchPressAborted = true;
                TimeSpan delay = DateTime.Now.Subtract(_LongPressStartTime);
                if (delay.TotalSeconds < TAP_TIME_THRESHOLD)
                {
                    if (IsDoubleTap())
                    {
                        mPDFView_DoubleTap(sender, e);
                        Trace.WriteLine("touchup doubletap");
                    }
                    else {
                        mPDFView_Tap(sender, e);

                    }
                }
                }

            if (TouchIDs.Contains(e.TouchDevice.Id))
            {
                TouchIDs.Remove(e.TouchDevice.Id);
            }
            //if (mCurrentTool != null && !e.Handled && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    AlreadySwiped = false;
            //    while (true)
            //    {
            //        mCurrentTool.TouchUpHandler(sender, e);
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
          }
        }

        void mPDFView_LostTouchCapture(object sender, System.Windows.Input.TouchEventArgs e)
        {
        }


        private void AnnoInTap()
        {
            
            int pagenum = mPDFView.GetCurrentPage();
            Page page = mPDFView.GetDoc().GetPage(pagenum);
            int AnnotNum = page.GetNumAnnots();        
                if (AnnotNum > 0)
                {
                    mPDFView.DocLock(true);
                    var dx = TouchStart.Position.X;
                    var dy = TouchStart.Position.Y;
                    mPDFView.ConvScreenPtToPagePt(ref dx, ref dy, pagenum);
                    for (int t = 0; t < AnnotNum; t++)
                    {
                        var CurrentAnno = page.GetAnnot(t);
                        if (CurrentAnno.GetRect().Contains(dx, dy))
                        {
                            updateanno_tap(CurrentAnno);
                        }
                    }
                    mPDFView.DocUnlock();
                }         
        }

        private void updateanno_tap(Annot newink)
        {
            int pagenum = mPDFView.GetCurrentPage();
            Page page = mPDFView.GetDoc().GetPage(pagenum);
            Annots.Ink mMarkup = new Annots.Ink(newink);
            if (mMarkup.GetOpacity() != 0 && mMarkup.GetContents() != "0")
            {
                int tapNum = int.Parse(mMarkup.GetContents()) + 1;
                mMarkup.SetContents(tapNum.ToString());
                mMarkup.SetOpacity(1);
                Date CurrentDate = new Date();
                CurrentDate.SetCurrentTime();
                mMarkup.SetDate(CurrentDate);
                mMarkup.RefreshAppearance();
                mPDFView.Update(mMarkup, page.GetIndex());
                FreehandCreate.logtxt.Append("tap " + "Time " + DateTime.Now.ToString() + "\r\n");
            }

        }
        private void AnnoInDoubleTap()
        {

            int pagenum = mPDFView.GetCurrentPage();
            Page page = mPDFView.GetDoc().GetPage(pagenum);
            int AnnotNum = page.GetNumAnnots();
            Trace.WriteLine("double  tap 0");

                if (AnnotNum > 0)
                {
                    mPDFView.DocLock(true);
                    var dx = TouchStart.Position.X;
                    var dy = TouchStart.Position.Y;
                    mPDFView.ConvScreenPtToPagePt(ref dx, ref dy, pagenum);
                    for (int t = 0; t < AnnotNum; t++)
                    {
                        var CurrentAnno = page.GetAnnot(t);
                        if (CurrentAnno.GetRect().Contains(dx, dy))
                        {
                            updateanno_doubletap(CurrentAnno);
                            Trace.WriteLine("success double tap2");
                        }
                    }
                mPDFView.DocUnlock();
            }           
        }

        //change the appearance of selected annotation
        private void updateanno_doubletap(Annot newink)
        {
            int pagenum = mPDFView.GetCurrentPage();
            Page page = mPDFView.GetDoc().GetPage(pagenum);
            Annots.Ink mMarkup = new Annots.Ink(newink);
            if (mMarkup.GetOpacity() != 0)
            {
                if (mMarkup.GetContents() == "0")
                {
                    mMarkup.SetContents("1");
                    //Console.WriteLine("retap");
                    mMarkup.SetColor(new ColorPt(0, 0, 0));
                }
                else
                {
                    mMarkup.SetContents("0");
                    mMarkup.SetColor(new ColorPt(99 / 255.0, 177 / 255.0, 175 / 255.0));
                }
                mMarkup.SetOpacity(1);
                Date CurrentDate = new Date();
                CurrentDate.SetCurrentTime();
                mMarkup.SetDate(CurrentDate);
                mMarkup.RefreshAppearance();
                mPDFView.Update(mMarkup, page.GetIndex());
                FreehandCreate.logtxt.Append("double tap " + "Time " + DateTime.Now.ToString() + "\r\n");
            }
        }


        void mPDFView_Tap(object sender, System.Windows.Input.TouchEventArgs e)
        {
                AnnoInTap();

        }
        void mPDFView_DoubleTap(object sender, System.Windows.Input.TouchEventArgs e)
        {
                AnnoInDoubleTap();
        }
        //Stylus events
        public bool createeink = true;
        void mPDFView_StylusDown(object sender, System.Windows.Input.StylusEventArgs e)
        {
            // Console.WriteLine("stylus position" +e.GetPosition(mPDFView));

            if (mCurrentTool != null && _IsEnabled)
            {
                //ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
                //ToolManager.ToolType next_tm;
                var id = e.StylusDevice.Id;
                if (id == SimpleShapeCreate.stylusid)
                {
                    Trace.WriteLine(mCurrentTool.ToolMode.ToString());
                    if (createeink)
                    {
                        CreateTool(ToolType.e_ink_create, null, true);
                        createeink = false;
                    }
                    //while (true)
                    //{
                    mCurrentTool.StylusDownHandler(sender, e);
                }
                //Trace.WriteLine("downdown");
                //    next_tm = mCurrentTool.NextToolMode;
                //    if (prev_tm != next_tm)
                //    {
                //        mCurrentTool = CreateTool(next_tm, mCurrentTool);
                //        prev_tm = next_tm;
                //    }
                //    else
                //    {
                //        break;
                //    }
                //}
            }
        }

        void mPDFView_StylusMove(object sender, System.Windows.Input.StylusEventArgs e)
        {

            //if (mCurrentTool != null && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;
            //    while (true)
            //    {
            mCurrentTool.StylusMoveHandler(sender, e);
            //Trace.WriteLine("movemove");
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }

        void mPDFView_StylusUp(object sender, System.Windows.Input.StylusEventArgs e)
        {

            //if (mCurrentTool != null && !e.Handled && _IsEnabled)
            //{
            //    ToolManager.ToolType prev_tm = mCurrentTool.ToolMode;
            //    ToolManager.ToolType next_tm;

            //    while (true)
            //    {
            mCurrentTool.StylusUpHandler(sender, e);
            //Trace.WriteLine("upup");
            //        next_tm = mCurrentTool.NextToolMode;
            //        if (prev_tm != next_tm)
            //        {
            //            mCurrentTool = CreateTool(next_tm, mCurrentTool);
            //            prev_tm = next_tm;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //}
        }

        #endregion Event Handlers


        #region Handling Annotation Drawing

        private List<DelayRemoveTimer> mDelayRemoveTimers;
        internal IList<DelayRemoveTimer> DelayRemoveTimers { get { return mDelayRemoveTimers; } }

        void mPDFView_OnRenderFinished(PDFViewWPF viewer)
        {
            CleanUpTimers();
        }

        private void RemoveTimersOnHiddenPages(int currentPage)
        {
            pdftron.PDF.PDFViewWPF.PagePresentationMode presentationMode = mPDFView.GetPagePresentationMode();


            if (presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_single_page||
                        presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing||
                        presentationMode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing_cover)
            {
                List<int> pages = new List<int>();
                pages.Add(currentPage);

                if (presentationMode == PDFViewWPF.PagePresentationMode.e_facing)
                {
                    pages.Add(currentPage % 2 == 1 ? currentPage + 1 : currentPage - 1);
                }
                else if (presentationMode == PDFViewWPF.PagePresentationMode.e_facing_cover)
                {
                    pages.Add(currentPage % 2 == 0 ? currentPage + 1 : currentPage - 1);
                }

                List<DelayRemoveTimer> removedTimers = new List<DelayRemoveTimer>();
                foreach (DelayRemoveTimer timer in mDelayRemoveTimers)
                {
                    if (!pages.Contains(timer.PageNumber))
                    {
                        removedTimers.Add(timer);
                        timer.Destroy();
                    }
                }
                foreach (DelayRemoveTimer timer in removedTimers)
                {
                    mDelayRemoveTimers.Remove(timer);
                }
            }

        }

        public void CleanUpTimers()
        {
            foreach (DelayRemoveTimer timer in mDelayRemoveTimers)
            {
                timer.Destroy();
            }
            mDelayRemoveTimers.Clear();
        }

        #endregion Handling Annotation Drawing
    }
}
