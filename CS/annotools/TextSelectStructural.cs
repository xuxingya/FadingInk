using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

using UIPoint = System.Windows.Point;
using UIRect = System.Windows.Rect;

using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;
using System.Windows.Controls;
using System.Windows;
using pdftron.PDF.Annots;


namespace pdftron.PDF.Tools
{




    public class TextSelectStructural : Tool
    {

        protected UIPoint mAnchorPoint;
        protected PDFRect mAnchorRectangle; // used when double clicking.
        protected UIPoint mDragPoint;
        protected int mDownPageNumber = -1;
        protected bool mHasAnchor = false;
        protected bool mIsDoubleClick = false;
        protected double mDoubleClickSelectionQuadLength;

        // touch selection
        protected bool mIsSelecting;
        protected UIPoint mTouchDownPoint;
        protected bool mIsMultiTouch;

        // scrolling
        protected UIPoint mDownPoint;
        protected bool mCanScroll = false;
        protected bool mStartScrolling = false;

        public TextSelectStructural(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mViewerCanvas = mToolManager.AnnotationCanvas;
            mPDFView.SetTextSelectionMode(PDFViewWPF.TextSelectionMode.e_structural);
            mSelectedAreasForHitTest = new List<PDFRect>();
            mViewerCanvas.Children.Insert(0, this);
            mToolMode = ToolManager.ToolType.e_text_select;
            mNextToolMode = ToolManager.ToolType.e_text_select;
            mPDFView.Cursor = System.Windows.Input.Cursors.IBeam;
        }


        internal override void OnClose()
        {
            base.OnClose();
            mPDFView.ClearSelection();
        }


        internal override void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonDownHandler(sender, e);
            if (mIsContextMenuOpen)
            {
                return;
            }
            ProcessInputDown(e);
            mViewerCanvas.CaptureMouse();
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            base.TouchDownHandler(sender, e);
            mViewerCanvas.CaptureTouch(e.TouchDevice);
            e.Handled = true;

            mIsSelecting = false;

            if (mToolManager.TouchIDs.Count == 1)
            {
                mIsMultiTouch = false;
                mDownPoint = GetPosition(e, mPDFView);
                mCanScroll = false;
                mTouchDownPoint = e.GetTouchPoint(null).Position;
                mAnchorPoint = e.GetTouchPoint(mViewerCanvas).Position;
            }
            else
            {
                mIsMultiTouch = true;
                DeselectAllText();
            }
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            UIPoint downPoint = GetPosition(e, mViewerCanvas);

            mDownPoint = GetPosition(e, mPDFView);
            mCanScroll = false;

            if (mIsDoubleClick)
            {
                mIsDoubleClick = false;
                return;
            }

            // handle shift clicks and set the anchor point if necessary
            if (mIsShiftDown)
            {
                if (mAnchorRectangle != null)
                {
                    SelectAndDraw(mAnchorRectangle, downPoint);
                }
                else if (!mHasAnchor)
                {
                    mAnchorPoint = downPoint;
                    mHasAnchor = true;
                }
                else
                {
                    SelectAndDraw(mAnchorPoint, downPoint);
                }
            }
            else
            {
                mAnchorRectangle = null;
                mPDFView.ClearSelection();
                DrawSelection();
                mAnchorPoint = downPoint;
                mHasAnchor = true;
            }
        }

        internal override void MouseMovedHandler(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!mIsDragging)
            {
                return;
            }

            ProcessInputMove(e);
        }

        public override void TouchMoveHandler(object sender, TouchEventArgs e)
        {
            base.TouchMoveHandler(sender, e);
            if (mToolManager.TouchIDs.Count == 1 && !mIsMultiTouch)
            {
                if (!mIsSelecting)
                {
                    UIPoint movePoint = e.GetTouchPoint(null).Position;
                    double xDist = movePoint.X - mTouchDownPoint.X;
                    double yDist = movePoint.Y - mTouchDownPoint.Y;
                    if ((xDist * xDist) + (yDist * yDist) > mToolManager.TOUCH_PRESS_DIST_THRESHOLD)
                    {
                        mIsSelecting = true;
                    }
                }
                if (mIsSelecting)
                {
                    ProcessInputMove(e);
                }
            }
        }

        private void ProcessInputMove(InputEventArgs e)
        {
            mDragPoint = GetPosition(e, mViewerCanvas);
            if (mAnchorRectangle != null)
            {
                SelectAndDraw(mAnchorRectangle, mDragPoint);
            }
            else
            {
                SelectAndDraw(mAnchorPoint, mDragPoint);
            }
            ScrollingHandler(GetPosition(e, mPDFView));
        }

        internal override void MouseLeftButtonUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            mViewerCanvas.ReleaseMouseCapture();
        }

        public override void TouchUpHandler(object sender, TouchEventArgs e)
        {
            base.TouchUpHandler(sender, e);
            mViewerCanvas.ReleaseTouchCapture(e.TouchDevice);
        }

        internal override void KeyDownAction(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (mIsCtrlDown)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.C:
                        CopySelectedTextToClipBoard();
                        e.Handled = true;
                        return;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Escape:
                        mPDFView.ClearSelection();
                        mHasAnchor = false;
                        e.Handled = true;
                        return;
                }
            }
            base.KeyDownAction(sender, e);
        }


        internal override void PreviewMouseWheelHandler(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (mIsDragging && mIsCtrlDown)
            {
                e.Handled = true;
            }
            else
            {
                base.PreviewMouseWheelHandler(sender, e);
            }
        }

        internal override void MouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            mIsDoubleClick = true;

            if (mIsShiftDown)
            {
                // ignore this
            }
            else
            {
                try
                {
                    mPDFView.DocLockRead();
                    UIPoint downPoint = GetPosition(e, mPDFView);
                    double x1 = downPoint.X - 0.5;
                    double x2 = downPoint.X + 0.5;
                    double y1 = downPoint.Y - 0.5;
                    double y2 = downPoint.Y + 0.5;
                    CleanSelectionState();
                    mPDFView.SetTextSelectionMode(pdftron.PDF.PDFViewWPF.TextSelectionMode.e_rectangular);
                    mPDFView.Select(x1, y1, x2, y2);
                    mPDFView.SetTextSelectionMode(pdftron.PDF.PDFViewWPF.TextSelectionMode.e_structural);
                    DrawSelection(true);
                    
                    // create anchor points if applicable
                    if (mPDFView.HasSelection())
                    {
                        int pgNum = mPDFView.GetSelectionBeginPage();
                        if (pgNum > 0)
                        {
                            pdftron.PDF.PDFViewWPF.Selection selection = mPDFView.GetSelection(pgNum);
                            double[] quads = selection.GetQuads();
                            if (quads.Length >= 8)
                            {
                                // we only care about the first quad.
                                PDFRect selRect = new PDFRect(quads[0], quads[1], quads[4], quads[5]);
                                mDoubleClickSelectionQuadLength = selRect.Width();
                                ConvertPageRectToCanvasRect(selRect, pgNum);
                                selRect.Normalize();
                                mAnchorRectangle = selRect;
                            }
                            
                        }
                    }

                }
                catch (System.Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlockRead();
                }
            }
            base.MouseDoubleClickHandler(sender, e);
        }

        internal override void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            mHasAnchor = false;
            mAnchorRectangle = null;
            base.ZoomChangedHandler(sender, e);
        }

        internal override void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            mHasAnchor = false;
            base.LayoutChangedHandler(sender, e);
        }


        #region Utility Functions

        /// <summary>
        /// Takes two points in the space of mViewerCanvas, selects in the PDFViewWPF with these points
        /// and then draws the selection as an overlay over the PDFViewWPF.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        protected void SelectAndDraw(UIPoint p1, UIPoint p2)
        {
            Select(p1, p2);
            CleanSelectionState();
            DrawSelection();
        }

        /// <summary>
        /// Takes a point and a rectangle in the space of mViewerCanvas, selects in the PDFViewWPF with the point 
        /// and the corner of the rectangle that gives the largest selection. 
        /// Then draws the selection as an overlay over the PDFViewWPF.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="p2"></param>
        protected void SelectAndDraw(PDFRect rect, UIPoint p2)
        {
            UIPoint anchorPoint1 = new UIPoint(rect.x1, rect.y2);
            UIPoint anchorPoint2 = new UIPoint(rect.x2, rect.y1);
            if (mToolManager.IsRightToLeftLanguage)
            {
                anchorPoint1.X = rect.x2;
                anchorPoint2.X = rect.x1;
            }

            // see if point is in the original selection rect.
            if (rect.Contains(p2.X, p2.Y))
            {
                SelectAndDraw(anchorPoint1, anchorPoint2);
                return;
            }

            int sel1PgNum = 0;
            int sel2PgNum = 0;
            int sel1QdNum = 0;
            int sel2QdNum = 0;
            double sel1StartQuadLength = 0;
            double sel1EndQuadLength = 0;
            double sel2StartQuadLength = 0;
            double sel2EndQuadLength = 0;

            GetSelectionData(anchorPoint1, p2, ref sel1PgNum, ref sel1QdNum, ref sel1StartQuadLength, ref sel1EndQuadLength);
            GetSelectionData(anchorPoint2, p2, ref sel2PgNum, ref sel2QdNum, ref sel2StartQuadLength, ref sel2EndQuadLength);

            bool useFirstPoint = true;
            if (sel2PgNum > sel1PgNum)
            {
                useFirstPoint = false;
            }
            else if (sel2PgNum == sel1PgNum)
            {
                if (sel2QdNum > sel1QdNum)
                {
                    useFirstPoint = false;
                }
                else if (sel2QdNum == sel1QdNum)
                {
                    if (sel2EndQuadLength + sel2StartQuadLength > sel1EndQuadLength + sel1StartQuadLength)
                    {
                        useFirstPoint = false;
                    }
                }
            }

            if (Math.Max(sel1PgNum, sel2PgNum) == 1 && Math.Max(sel1QdNum, sel2QdNum) == 16
                && Math.Max(sel2EndQuadLength, sel1EndQuadLength) < mDoubleClickSelectionQuadLength)
            {
                SelectAndDraw(anchorPoint1, anchorPoint2);
            }
            else if (useFirstPoint)
            {
                SelectAndDraw(anchorPoint1, p2);
            }
            else
            {
                SelectAndDraw(anchorPoint2, p2);
            }
        }


        protected void Select(UIPoint p1, UIPoint p2)
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();
            mPDFView.Select(p1.X - sx, p1.Y - sy, p2.X - sx, p2.Y - sy);
        }

        protected void CleanSelectionState()
        {
            mToolManager.TextSelectionCanvas.Children.Clear();
            mPagesOnScreen.Clear();
            mSelectionCanvases.Clear();
        }

        /// <summary>
        /// Selects text between the two points in mAnnotationCanvas space and then measures the data
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="numPages"></param>
        /// <param name="numQuads"></param>
        /// <param name="startQuadlength"></param>
        /// <param name="endQuadlength"></param>
        protected void GetSelectionData(UIPoint p1, UIPoint p2, ref int numPages, ref int numQuads, ref double startQuadLength, ref double endQuadLength)
        {
            numPages = 0;
            numQuads = 0;
            startQuadLength = 0;
            endQuadLength = 0;

            mPDFView.ClearSelection();
            Select(p1, p2);
            int selBegin = mPDFView.GetSelectionBeginPage();
            int selEnd = mPDFView.GetSelectionEndPage();
            if (selBegin < 1)
            {
                return;
            }
            numPages = selEnd - selBegin + 1;
            pdftron.PDF.PDFViewWPF.Selection selection = mPDFView.GetSelection(selBegin);
            double[] startQuads = selection.GetQuads();
            selection = mPDFView.GetSelection(selEnd);
            double[] endQuads = selection.GetQuads();
            
            if (startQuads != null)
            {
                numQuads += startQuads.Length;
                startQuadLength = Math.Abs(startQuads[0] - startQuads[4]);
            }
            if (endQuads != null)
            {
                numQuads += endQuads.Length;
                endQuadLength = Math.Abs(endQuads[endQuads.Length - 8] - endQuads[endQuads.Length - 4]);
            }
            return;
        }

        protected void ScrollingHandler(UIPoint currentPoint)
        {
            if (!mCanScroll)
            {
                double xDist = currentPoint.X - mDownPoint.X;
                double yDist = currentPoint.Y - mDownPoint.Y;
                if ((xDist * xDist) + (yDist * yDist) > mToolManager.TOUCH_PRESS_DIST_THRESHOLD)
                {
                    mCanScroll = true;
                }
            }
            else
            {
                // x scrolling
                double xSpeed = 0;
                if (currentPoint.X < 0)
                {
                    double xRatio = currentPoint.X / mToolManager.TEXT_SELECT_SCROLL_MARGIN_X;
                    if (xRatio < -1)
                    {
                        xRatio = -mToolManager.TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_X;
                    }
                    xSpeed = xRatio * mToolManager.TEXT_SELECT_SCROLL_SPEED_X;
                }
                if (currentPoint.X > mPDFView.ActualWidth - mToolManager.TEXT_SELECT_SCROLL_MARGIN_X)
                {
                    double xRatio = (currentPoint.X + mToolManager.TEXT_SELECT_SCROLL_MARGIN_X - mPDFView.ActualWidth) / mToolManager.TEXT_SELECT_SCROLL_MARGIN_X;
                    if (xRatio > 1)
                    {
                        xRatio = mToolManager.TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_X;
                    }
                    xSpeed = xRatio * mToolManager.TEXT_SELECT_SCROLL_SPEED_X;
                }
                if (xSpeed != 0)
                {
                    mPDFView.SetHScrollPos(mPDFView.GetHScrollPos() + xSpeed);
                }

                // y scrolling
                double ySpeed = 0;
                if (currentPoint.Y < 0)
                {
                    double yRatio = currentPoint.Y / mToolManager.TEXT_SELECT_SCROLL_MARGIN_Y;
                    if (yRatio < -1)
                    {
                        yRatio = -mToolManager.TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_Y;
                    }
                    ySpeed = yRatio * mToolManager.TEXT_SELECT_SCROLL_SPEED_Y;
                }
                if (currentPoint.Y > mPDFView.ActualHeight)// - mToolManager.TEXT_SELECT_SCROLL_MARGIN_Y)
                {
                    double yRatio = (currentPoint.Y - mPDFView.ActualHeight) / mToolManager.TEXT_SELECT_SCROLL_MARGIN_Y;
                    if (yRatio > 1)
                    {
                        yRatio = mToolManager.TEXT_SELECT_SCROLL_SPEED_MULTIPLIER_IF_POST_MARGIN_Y;
                    }
                    ySpeed = yRatio * mToolManager.TEXT_SELECT_SCROLL_SPEED_Y;
                }
                if (ySpeed != 0)
                {
                    mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() + ySpeed);
                }
            }

        }

        #endregion Utility Functions

    }
}