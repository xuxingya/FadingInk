using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using pdftron.PDF;

using UIPoint = System.Windows.Point;
using UIRect = System.Windows.Rect;

using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF.Annots;
using System.Windows;




namespace pdftron.PDF.Tools
{
    /// <summary>
    /// This class is the base class for wrapping annotations into selection objects.
    /// This class will use the annotations bounding box and gives the user 8 control points to move around.
    /// 
    /// Create a subclass of this to make a more specific tool for other annotations.
    /// </summary>
    internal class NoteHost
    {
        protected NoteManager mNoteManager;
        protected Canvas mArrowCanvas;
        protected Canvas mNoteCanvas;

        protected int mPageNumber;
        protected Markup mMarkup;
        protected Popup mPopup;

        protected const int DEFAULT_WIDTH = 200;
        protected const int DEFAULT_HEIGHT = 200;
        protected const int DEFAULT_OFFSET = 50;
        protected const int DEFAULT_ARROW_BASE_WIDTH = 20; // this is actually the half width, it will be applid twice

        protected Border mNoteBorder;
        protected Border mMinimizeBorder;
        protected TextBox mTextBox;
        protected TextBlock mNameText;
        protected TextBlock mDateText;
        protected Rectangle mMinimizeShape;

        protected Path mArrow;
        protected PathGeometry mArrowPathFigure;

        protected SolidColorBrush mBackgroundBrush;
        protected SolidColorBrush mForegroundBrush;

        protected IList<UIPoint> mAnnotTargetPointsInPageSpace;
        protected IList<UIPoint> mAnnotTargetPoints;
        protected int mAnnotTargetPointIndex;
        protected UIPoint mCanvasSpaceTopLeft;
        protected UIPoint mPageSpaceTopLeft;

        protected UIPoint mMousePoint;
        protected bool mIsDragging = false;
        protected bool mIsMinimizeClick = false;

        // Touch input
        protected bool mIsMultiTouch = false;

        internal int PageNumber
        {
            get { return mPageNumber; }
        }
        
        internal Visibility Visibility
        {
            set 
            { 
                mNoteBorder.Visibility = value;
                mArrow.Visibility = value;
            }
        }


        internal NoteHost(NoteManager noteManager, Canvas arrowCanvas, Canvas noteCanvas, Markup markup, int pageNumber, IList<UIPoint> targetPoints)
        {
            mNoteManager = noteManager;
            mArrowCanvas = arrowCanvas;
            mNoteCanvas = noteCanvas;
            mPageNumber = pageNumber;
            mMarkup = markup;
            mAnnotTargetPoints = targetPoints;

            mCanvasSpaceTopLeft = new UIPoint(0, 0);
            mPageSpaceTopLeft = new UIPoint(0, 0);

            CreateNoteAndArrow();
            ModifyAppearance();
            Reposition();
            UpdateTargetPointsInPageSpace();
            UpdateArrowTarget();
        }



        protected void CreateNoteAndArrow()
        {
            mNoteBorder = new Border();
            mNoteBorder.Width = DEFAULT_WIDTH;
            mNoteBorder.Height = DEFAULT_HEIGHT;
            mNoteBorder.MinWidth = DEFAULT_WIDTH;
            mNoteBorder.MinHeight = DEFAULT_HEIGHT;
            mNoteBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            mNoteBorder.CornerRadius = new CornerRadius(5);
            mNoteBorder.BorderThickness = new Thickness(2);

            mNoteBorder.MouseLeftButtonDown += NoteBorder_MouseLeftButtonDown;
            mNoteBorder.MouseMove += NoteBorder_MouseMove;
            mNoteBorder.MouseLeftButtonUp += NoteBorder_MouseLeftButtonUp;

            mNoteBorder.TouchDown += mNoteBorder_TouchDown;
            mNoteBorder.TouchMove += mNoteBorder_TouchMove;
            mNoteBorder.TouchUp += mNoteBorder_TouchUp;
            mNoteBorder.ManipulationDelta += mNoteBorder_ManipulationDelta;
            mNoteBorder.ManipulationCompleted += mNoteBorder_ManipulationCompleted;

            mNoteBorder.IsManipulationEnabled = true;

            Grid grid = new Grid();
            RowDefinition row0 = new RowDefinition();
            row0.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto);
            RowDefinition row1 = new RowDefinition();
            row1.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto);
            RowDefinition row2 = new RowDefinition();
            row2.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
            RowDefinition row3 = new RowDefinition();
            row3.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto);
            grid.RowDefinitions.Add(row0);
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);
            grid.RowDefinitions.Add(row3);

            grid.Margin = new Thickness(5);

            mNoteBorder.Child = grid;


            mNameText = CreateName();
            mNameText.HorizontalAlignment = HorizontalAlignment.Left;
            grid.Children.Add(mNameText);

            mMinimizeBorder = CreateMinimizeButton();
            mMinimizeBorder.HorizontalAlignment = HorizontalAlignment.Right;
            mMinimizeBorder.MouseEnter += MinimizeBorder_MouseEnter;
            mMinimizeBorder.MouseLeave += MinimizeBorder_MouseLeave;
            mMinimizeBorder.TouchEnter += mMinimizeBorder_TouchEnter;
            mMinimizeBorder.TouchLeave += mMinimizeBorder_TouchLeave;
            mMinimizeBorder.IsManipulationEnabled = true;
            grid.Children.Add(mMinimizeBorder);

            mDateText = CreateDate();
            mDateText.SetValue(Grid.RowProperty, 1);
            mDateText.Margin = new Thickness(0, 5, 0, 0);
            grid.Children.Add(mDateText);

            mTextBox = CreateTextBox();
            mTextBox.SetValue(Grid.RowProperty, 2);
            mTextBox.Margin = new Thickness(0, 5, 0, 0);
            mTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(TextBoxScrollChanged), true);
            mTextBox.TextChanged += mTextBox_TextChanged;
            mTextBox.GotFocus += mTextBox_GotFocus;
            mTextBox.Loaded += mTextBox_Loaded;
            mTextBox.TouchDown += mTextBox_TouchDown;
            mTextBox.TouchUp += mTextBox_TouchUp;
            mTextBox.LostFocus += mTextBox_LostFocus;
            mTextBox.IsManipulationEnabled = true;
            grid.Children.Add(mTextBox);

            // Add control for dragging to resize here.

            mNoteCanvas.Children.Add(mNoteBorder);


            // Create arrow
            mArrow = new Path();
            mArrow.StrokeLineJoin = PenLineJoin.Miter;
            mArrow.StrokeMiterLimit = 2;
            mArrow.StrokeThickness = 2;

            mArrowPathFigure = new PathGeometry();
            mArrow.Data = mArrowPathFigure;

            mArrowCanvas.Children.Add(mArrow);            
        }

        protected TextBlock CreateName()
        {
            TextBlock nameText = new TextBlock();

            switch (mMarkup.GetType())
            {
                case Annot.Type.e_Line:
                    nameText.Text = "Line";
                    break;
                case Annot.Type.e_Circle:
                    nameText.Text = "Ellipse";
                    break;
                case Annot.Type.e_Square:
                    nameText.Text = "Rectangle";
                    break;
                case Annot.Type.e_Ink:
                    nameText.Text = "Free Hand";
                    break;
                case Annot.Type.e_Polyline:
                    nameText.Text = "Polyline";
                    break;
                case Annot.Type.e_Polygon:
                    nameText.Text = "Polygon";
                    break;
                case Annot.Type.e_Highlight:
                    nameText.Text = "Highlight";
                    break;
                case Annot.Type.e_Underline:
                    nameText.Text = "Underline";
                    break;
                case Annot.Type.e_StrikeOut:
                    nameText.Text = "Strikeout";
                    break;
                case Annot.Type.e_Squiggly:
                    nameText.Text = "Squiggly";
                    break;
            }
            nameText.IsHitTestVisible = false;
            return nameText;
        }

        protected TextBlock CreateDate()
        {
            TextBlock dateText = new TextBlock();
            Date date = mMarkup.GetDate();

            dateText.Text = string.Format("{0:0000}-{1:00}-{2:00}  {3:00}:{4:00}:{5:00}", date.year, date.month, date.day, date.hour, date.minute, date.second);
            dateText.IsHitTestVisible = false;
            return dateText;
        }

        protected Border CreateMinimizeButton()
        {
            Border minBorder = new Border();
            minBorder.Width = 15;
            minBorder.Height = 15;
            minBorder.CornerRadius = new System.Windows.CornerRadius(3);
            minBorder.BorderThickness = new Thickness(1);

            Grid g = new Grid();            
            mMinimizeShape = new Rectangle();
            mMinimizeShape.Height = 3;
            mMinimizeShape.Width = 10;
            mMinimizeShape.HorizontalAlignment = HorizontalAlignment.Center;
            mMinimizeShape.VerticalAlignment = VerticalAlignment.Bottom;
            mMinimizeShape.IsHitTestVisible = false;
            mMinimizeShape.Margin = new Thickness(0, 0, 0, 3);
            

            minBorder.Child = mMinimizeShape;

            return minBorder;
        }

        protected TextBox CreateTextBox()
        {
            TextBox tb = new TextBox();
            tb.AcceptsReturn = true;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalAlignment = VerticalAlignment.Stretch;

            // create a new popup if necessary
            mPopup = mMarkup.GetPopup();
            if (mPopup == null || !mPopup.IsValid())
            {
                mPopup = pdftron.PDF.Annots.Popup.Create(mNoteManager.PDFView.GetDoc().GetSDFDoc(), mMarkup.GetRect());
                mMarkup.SetPopup(mPopup);
                mPopup.SetParent(mMarkup);
            }

            tb.Text = mPopup.GetContents();

            return tb;
        }


        protected void DrawArrow()
        {
            DrawArrow(new UIPoint(mCanvasSpaceTopLeft.X + (mNoteBorder.Width / 2), mCanvasSpaceTopLeft.Y + (mNoteBorder.Width / 2)));
        }

        protected void DrawArrow(UIPoint center)
        {
            UIPoint tip = mAnnotTargetPoints[mAnnotTargetPointIndex];

            double theta = Math.Atan2((center.Y - tip.Y), (center.X - tip.X));

            double xStep = DEFAULT_ARROW_BASE_WIDTH * Math.Sin(theta);
            double yStep = DEFAULT_ARROW_BASE_WIDTH * Math.Cos(theta);

            UIPoint anchor1 = new UIPoint(center.X + xStep, center.Y - yStep);
            UIPoint anchor2 = new UIPoint(center.X - xStep, center.Y + yStep);
            


            PathFigure a_head = new PathFigure();
            a_head.StartPoint = tip;
            a_head.Segments.Add(new LineSegment() { Point = anchor1 });
            a_head.Segments.Add(new LineSegment() { Point = anchor2 });
            a_head.Segments.Add(new LineSegment() { Point = tip });
            mArrowPathFigure.Figures.Clear();
            mArrowPathFigure.Figures.Add(a_head);
        }


        /// <summary>
        /// Makes sure that the entire view doesn't scroll to accommodate the textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void TextBoxScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true;
        }



        /// <summary>
        /// Makes the note reposition itself, as well as it's arrow
        /// </summary>
        internal void Reposition()
        {
            PDFRect pageBoxRect = mNoteManager.ToolManager.CurrentTool.BuildPageBoundBoxOnClient(mPageNumber);
            PDFRect annotBoxRect = mMarkup.GetRect();
            mNoteManager.ToolManager.CurrentTool.ConvertPageRectToCanvasRect(annotBoxRect, mPageNumber);

            double sx = mNoteManager.PDFView.GetHScrollPos();
            double sy = mNoteManager.PDFView.GetVScrollPos();

            // make sure it's inside screen
            if (pageBoxRect.x1 < sx)
            {
                pageBoxRect.x1 = sx;
            }
            if (pageBoxRect.x2 > sx + mNoteManager.PDFView.ActualWidth)
            {
                pageBoxRect.x2 = sx + mNoteManager.PDFView.ActualWidth;
            }
            if (pageBoxRect.y1 < sy)
            {
                pageBoxRect.y1 = sy;
            }
            if (pageBoxRect.y2 > sy + mNoteManager.PDFView.ActualHeight)
            {
                pageBoxRect.y2 = sy + mNoteManager.PDFView.ActualHeight;
            }

            double annotMidX = (annotBoxRect.x1 + annotBoxRect.x2) / 2;
            double annotMidY = (annotBoxRect.y1 + annotBoxRect.y2) / 2;
            double pageMidX = (pageBoxRect.x1 + pageBoxRect.x2) / 2;
            double pageMidY = (pageBoxRect.y1 + pageBoxRect.y2) / 2;

            bool above = true;
            bool left = true;

            if (annotMidX < pageMidX)
            {
                left = false;
            }

            if (annotBoxRect.y1 - pageBoxRect.y1 < DEFAULT_HEIGHT && annotMidY < pageMidY)
            {
                above = false;
            }

            double x;
            double y;
            if (left)
            {
                x = annotBoxRect.x1 - DEFAULT_OFFSET - DEFAULT_WIDTH;
            }
            else
            {
                x = annotBoxRect.x2 + DEFAULT_OFFSET;
                if (x > pageBoxRect.x2 - DEFAULT_WIDTH)
                {
                    x = pageBoxRect.x2 - DEFAULT_WIDTH;
                }
            }
            if (above)
            {
                y = annotBoxRect.y1 - DEFAULT_HEIGHT - DEFAULT_OFFSET;
            }
            else
            {
                y = annotBoxRect.y2 + DEFAULT_OFFSET;
                if (y > pageBoxRect.y2 + DEFAULT_HEIGHT)
                {
                    y = pageBoxRect.y2 + DEFAULT_HEIGHT;
                }
            }
            if (x < pageBoxRect.x1)
            {
                x = pageBoxRect.x1;
            }
            if (y < pageBoxRect.y1)
            {
                y = pageBoxRect.y1;
            }

            mCanvasSpaceTopLeft.X = x;
            mCanvasSpaceTopLeft.Y = y;
            mNoteBorder.SetValue(Canvas.LeftProperty, mCanvasSpaceTopLeft.X);
            mNoteBorder.SetValue(Canvas.TopProperty, mCanvasSpaceTopLeft.Y);
            UpdatePageCoordinates();
        }

        /// <summary>
        /// Will update each Note so that it's top left corner is on the same position relative to the page it belongs to.
        /// </summary>
        internal void RepositionAfterZoom()
        {
            PositionRelativeToPage();
            UpdateTargetPointsFromPageSpace();
            UpdateArrowTarget();
        }

        /// <summary>
        /// Will bring the popup to the front and place the cursor in the TextBox
        /// </summary>
        internal void Activate()
        {
            PutOnTop();
            mTextBox.Focus();
        }

        /// <summary>
        /// Will update the point to which the arrow should point, and also redraw the arrow accordingly
        /// </summary>
        /// <param name="targetPoints">A list of points in canvas space</param>
        internal void AnnotationMoving(IList<UIPoint> targetPoints)
        {
            mAnnotTargetPoints = targetPoints;
            UpdateArrowTarget();
            UpdateTargetPointsInPageSpace();
        }


        /// <summary>
        /// Will figure out the main color of the control according to the annotation's color.
        /// </summary>
        /// <param name="annot"></param>
        internal void ModifyAppearance()
        {
            ColorPt color;
            double annotColorLuminance;

            switch (mMarkup.GetType())
            {
                case Annot.Type.e_Line:
                case Annot.Type.e_Polyline:
                case Annot.Type.e_Ink:
                case Annot.Type.e_Highlight:
                case Annot.Type.e_Underline:
                case Annot.Type.e_StrikeOut:
                case Annot.Type.e_Squiggly:
                    color = mMarkup.GetColorAsRGB();
                    mBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    break;
                case Annot.Type.e_Square:
                case Annot.Type.e_Circle:
                case Annot.Type.e_Polygon:
                    bool useStroke = false;
                    Markup markup = new Markup(mMarkup);
                    if (markup.GetInteriorColorCompNum() == 0)
                    {
                        useStroke = true;
                    }
                    else
                    {
                        color = markup.GetInteriorColor();
                        Color col = Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5));
                        annotColorLuminance = (0.3 * col.R + 0.59 * col.G + 0.11 * col.B) / 255;
                        if (annotColorLuminance > 0.9)
                        {
                            useStroke = true;
                        }
                        else
                        {
                            mBackgroundBrush = new SolidColorBrush(col);
                        }
                    }

                    if (useStroke)
                    {
                        if (markup.GetColorCompNum() == 0)
                        {
                            mBackgroundBrush = Brushes.LightGray;
                        }
                        else
                        {
                            color = mMarkup.GetColorAsRGB();
                            mBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                        }
                    }
                    break;

                case Annot.Type.e_Text:
                    mBackgroundBrush = Brushes.Yellow;
                    break;
                default:
                    mBackgroundBrush = Brushes.LightGray;
                    break;
            }
            
            
            
            annotColorLuminance = (0.3 * mBackgroundBrush.Color.R + 0.59 * mBackgroundBrush.Color.G + 0.11 * mBackgroundBrush.Color.B) / 255;

            
            if (annotColorLuminance > 0.5)
                mForegroundBrush = Brushes.Black;
            else
                mForegroundBrush = Brushes.White;

            PaintMenu();
        }

        // Will paint everything in the newly selected colors.
        protected void PaintMenu()
        {
            mNoteBorder.Background = mBackgroundBrush;
            mNameText.Foreground = mForegroundBrush;
            mDateText.Foreground = mForegroundBrush;
            mMinimizeBorder.BorderBrush = mForegroundBrush;
            mMinimizeBorder.Background = mBackgroundBrush;
            mMinimizeShape.Fill = mForegroundBrush;

            mArrow.Stroke = new SolidColorBrush(Color.FromArgb(200, mBackgroundBrush.Color.R, mBackgroundBrush.Color.G, mBackgroundBrush.Color.B));
            mArrow.Fill = new SolidColorBrush(Color.FromArgb(100, mBackgroundBrush.Color.R, mBackgroundBrush.Color.G, mBackgroundBrush.Color.B));
        }

        // Will put the canvas on top
        internal void PutOnTop()
        {
            mNoteCanvas.Children.Remove(mNoteBorder);
            mNoteCanvas.Children.Add(mNoteBorder);
            mArrowCanvas.Children.Remove(mArrow);
            mArrowCanvas.Children.Add(mArrow);
        }

        /// <summary>
        /// Positions the note relative to the page coordinates of the top left corner.
        /// </summary>
        protected void PositionRelativeToPage()
        {
            double x = mPageSpaceTopLeft.X;
            double y = mPageSpaceTopLeft.Y;

            mNoteManager.PDFView.ConvPagePtToScreenPt(ref x, ref y, mPageNumber);

            mCanvasSpaceTopLeft.X = x + mNoteManager.PDFView.GetHScrollPos();
            mCanvasSpaceTopLeft.Y = y + mNoteManager.PDFView.GetVScrollPos();

            mNoteBorder.SetValue(Canvas.LeftProperty, mCanvasSpaceTopLeft.X);
            mNoteBorder.SetValue(Canvas.TopProperty, mCanvasSpaceTopLeft.Y);
        }

        /// <summary>
        /// Updates the page coordinates to reflect the coordinates on the canvas
        /// </summary>
        protected void UpdatePageCoordinates()
        {
            double x = mCanvasSpaceTopLeft.X - mNoteManager.PDFView.GetHScrollPos();
            double y = mCanvasSpaceTopLeft.Y - mNoteManager.PDFView.GetVScrollPos();

            mNoteManager.PDFView.ConvScreenPtToPagePt(ref x, ref y, mPageNumber);

            mPageSpaceTopLeft.X = x;
            mPageSpaceTopLeft.Y = y;
        }


        protected void UpdateTargetPointsInPageSpace()
        {
            mAnnotTargetPointsInPageSpace = new List<UIPoint>();
            double sx = mNoteManager.PDFView.GetHScrollPos();
            double sy = mNoteManager.PDFView.GetVScrollPos();
            foreach (UIPoint point in mAnnotTargetPoints)
            {
                double x = point.X - sx;
                double y = point.Y - sy;

                mNoteManager.PDFView.ConvScreenPtToPagePt(ref x, ref y, mPageNumber);

                mAnnotTargetPointsInPageSpace.Add(new UIPoint(x, y));
            }
        }

        protected void UpdateTargetPointsFromPageSpace()
        {
            mAnnotTargetPoints = new List<UIPoint>();
            double sx = mNoteManager.PDFView.GetHScrollPos();
            double sy = mNoteManager.PDFView.GetVScrollPos();
            foreach (UIPoint point in mAnnotTargetPointsInPageSpace)
            {
                double x = point.X;
                double y = point.Y;

                mNoteManager.PDFView.ConvPagePtToScreenPt(ref x, ref y, mPageNumber);

                mAnnotTargetPoints.Add(new UIPoint(x + sx, y + sy));
            }
        }

        /// <summary>
        /// Updates the point to which the arrow points.
        /// </summary>
        protected void UpdateArrowTarget()
        {
            // at this point, we can assume that we have a list of snap points.

            // first, figure out which point we should target
            mAnnotTargetPointIndex = 0;
            double minDist = -1;
            double centerX = mCanvasSpaceTopLeft.X + (mNoteBorder.Width / 2);
            double centerY = mCanvasSpaceTopLeft.Y + (mNoteBorder.Height / 2);
            for (int i = 0; i < mAnnotTargetPoints.Count; i++)
            {
                double dist = Math.Pow(mAnnotTargetPoints[i].X - centerX, 2) + Math.Pow(mAnnotTargetPoints[i].Y - centerY, 2);
                if (dist < minDist || minDist < 0)
                {
                    minDist = dist;
                    mAnnotTargetPointIndex = i;
                }
            }

            DrawArrow(new UIPoint(centerX, centerY));
        }

        /// <summary>
        /// Removes the note from the visual tree.
        /// </summary>
        internal void RemoveFromCanvas()
        {
            if (mArrowCanvas.Children.Contains(mArrow))
            {
                mArrowCanvas.Children.Remove(mArrow);
            }
            if (mNoteCanvas.Children.Contains(mNoteBorder))
            {
                mNoteCanvas.Children.Remove(mNoteBorder);
            }
        }


        #region Event Handling


        void NoteBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessInputDown(e);
        }

        void mNoteBorder_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (mIsMinimizeClick)
            {
                return;
            }
            ProcessInputDown(e);
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            if (e.OriginalSource == mNoteBorder || e.OriginalSource == mMinimizeBorder)
            {
                PutOnTop();
                e.Handled = true;
                mIsDragging = true;
                mMousePoint = Tool.GetPosition(e, mNoteCanvas);
                if (e.OriginalSource == mMinimizeBorder)
                {
                    mIsMinimizeClick = true;
                    mMinimizeBorder.Background = mForegroundBrush;
                    mMinimizeShape.Fill = mBackgroundBrush;
                }
                else
                {
                    CaptureInput(mNoteBorder, e);
                    mIsMinimizeClick = false;
                }
            }
        }


        void NoteBorder_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ProcessInputMove(e);
        }

        void mNoteBorder_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (!mIsMultiTouch)
            {
                ProcessInputMove(e);
            }
        }

        private void ProcessInputMove(InputEventArgs e)
        {
            e.Handled = true;
            if (mIsDragging && !mIsMinimizeClick)
            {
                UIPoint newPoint = Tool.GetPosition(e, mNoteCanvas);
                mCanvasSpaceTopLeft.X += newPoint.X - mMousePoint.X;
                mCanvasSpaceTopLeft.Y += newPoint.Y - mMousePoint.Y;
                mMousePoint.X = newPoint.X;
                mMousePoint.Y = newPoint.Y;
                mNoteBorder.SetValue(Canvas.LeftProperty, mCanvasSpaceTopLeft.X);
                mNoteBorder.SetValue(Canvas.TopProperty, mCanvasSpaceTopLeft.Y);
                UpdateArrowTarget();
            }
        }

        void NoteBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessInputUp(e);
        }

        void mNoteBorder_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            ProcessInputUp(e);
            //mNoteBorder.ReleaseTouchCapture(e.TouchDevice);
        }

        private void ProcessInputUp(InputEventArgs e)
        {
            e.Handled = true;
            if (mIsDragging)
            {
                if (mIsMinimizeClick)
                {
                    if (e.OriginalSource == mMinimizeBorder)
                    {
                        //mMinimizeShape.Fill = mForegroundBrush;
                        //mMinimizeBorder.Background = mBackgroundBrush;
                        //mNoteCanvas.Children.Remove(mNoteBorder);
                        //mArrowCanvas.Children.Remove(mArrow);
                        mNoteManager.CloseNote(mMarkup);

                    }
                    ReleaseInputCapture(mMinimizeBorder, e);
                }
                else
                {
                    UpdatePageCoordinates();

                    ReleaseInputCapture(mNoteBorder, e);
                }
            }
            mIsDragging = false;
            mIsMinimizeClick = false;
        }

        void mNoteBorder_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            e.Handled = true;
            if (e.Manipulators.Count() > 1)
            {
                mIsMultiTouch = true;
            }
        }

        void mNoteBorder_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
            mIsMultiTouch = false;
            mIsMinimizeClick = false;
        }






        private void MinimizeBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ProcessInputEnter(e);
        }

        void mMinimizeBorder_TouchEnter(object sender, TouchEventArgs e)
        {
            ProcessInputEnter(e);
        }

        private void ProcessInputEnter(InputEventArgs e)
        {
            if (mIsMinimizeClick)
            {
                mMinimizeBorder.Background = mForegroundBrush;
                mMinimizeShape.Fill = mBackgroundBrush;
            }
        }

        private void MinimizeBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ProcessInputLeave(e);
        }

        void mMinimizeBorder_TouchLeave(object sender, TouchEventArgs e)
        {
            ProcessInputLeave(e);
        }

        private void ProcessInputLeave(InputEventArgs e)
        {
            if (mIsMinimizeClick)
            {
                mMinimizeBorder.Background = mBackgroundBrush;
                mMinimizeShape.Fill = mForegroundBrush;
            }
        }


        void mTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PutOnTop();
        }

        void mTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            mNoteManager.ToolManager.RaiseAnnotationNoteChangedEvent(mMarkup);
        }

        void mTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mPopup.SetContents(mTextBox.Text);
            DateTime now = DateTime.Now;
            Date date = new Date((short)now.Year, (byte)now.Month, (byte)now.Day, (byte)now.Hour, (byte)now.Minute, (byte)now.Second);
            mMarkup.SetDate(date);
        }


        void mTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            mTextBox.Focus();
        }

        void mTextBox_TouchDown(object sender, TouchEventArgs e)
        {
            mTextBox.Focus();
            e.Handled = true;
        }

        void mTextBox_TouchUp(object sender, TouchEventArgs e)
        {
            UIPoint point = e.GetTouchPoint(mTextBox).Position;
            int idx = mTextBox.GetCharacterIndexFromPoint(point, true);
            int hitSomething = mTextBox.GetCharacterIndexFromPoint(point, false);
            if (hitSomething < 0)
            {
                int lineIndex = mTextBox.GetLineIndexFromCharacterIndex(idx);
                string text = mTextBox.GetLineText(lineIndex);
                char[] chars = new char[] { '\r', '\n' };
                if (text.Trim(chars).Length > 0)
                {
                    mTextBox.CaretIndex = idx + 1;
                }
                else
                {
                    mTextBox.CaretIndex = idx;
                }
            }
            else
            {
                mTextBox.CaretIndex = idx;
            }
            mTextBox.SelectionLength = 0;
        }


        #endregion Event Handling

        #region Utility Functions

        protected void CaptureInput(UIElement capturingElement, InputEventArgs e)
        {
            if (e is MouseEventArgs)
            {
                capturingElement.CaptureMouse();
            }
            else if (e is TouchEventArgs)
            {
                TouchEventArgs te = e as TouchEventArgs;
                capturingElement.CaptureTouch(te.TouchDevice);
            }
            else
            {
                throw new ArgumentException("Parameter must be of type MouseEventArgs or TouchEventArgs");
            }
        }

        protected void ReleaseInputCapture(UIElement capturingElement, InputEventArgs e)
        {
            if (e is MouseEventArgs)
            {
                capturingElement.ReleaseMouseCapture();
            }
            else if (e is TouchEventArgs)
            {
                TouchEventArgs te = e as TouchEventArgs;
                capturingElement.ReleaseTouchCapture(te.TouchDevice);
            }
            else
            {
                throw new ArgumentException("Parameter must be of type MouseEventArgs or TouchEventArgs");
            }
        }


        #endregion Utility Functions

    }
}