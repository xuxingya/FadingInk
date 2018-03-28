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
using PDFRect = pdftron.PDF.Rect;

namespace pdftron.PDF.Tools
{

    public class Pan : Tool
    {

        protected UIPoint mLastPoint;

        protected Rectangle mWidgetRectangle;
        protected Annot mCurrentWidgetAnnot;
        protected int mCurrentWidgetPageNumber;

        protected bool mIsDoubleClicking = false;
        protected UIPoint mDoubleClickDownPoint;
        protected const int DOUBLE_CLICK_THRESHOLD = 5; // The distance we need to drag before the annot starts moving

        
        public Pan(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_pan;
            mNextToolMode = ToolManager.ToolType.e_pan;
            mPDFView.Cursor = Cursors.Arrow;

            mWidgetRectangle = new Rectangle();
            mWidgetRectangle.Stroke = System.Windows.Media.Brushes.Black;
            mWidgetRectangle.StrokeThickness = 1;

            mViewerCanvas = mToolManager.AnnotationCanvas;
            mViewerCanvas.Children.Add(this);
            this.Children.Add(mWidgetRectangle);
        }


        #region Event Handlers

        internal override void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            if (mCurrentWidgetAnnot != null)
            {
                DrawWidgetFrame(mCurrentWidgetAnnot, mCurrentWidgetPageNumber);
            }
            base.ZoomChangedHandler(sender, e);
        }

        internal override void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            if (mCurrentWidgetAnnot != null)
            {
                DrawWidgetFrame(mCurrentWidgetAnnot, mCurrentWidgetPageNumber);
            }
            base.LayoutChangedHandler(sender, e);
        }


        internal override void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            base.MouseLeftButtonDownHandler(sender, e);
            ProcessInputDown(e);
            mViewerCanvas.CaptureMouse();
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            base.TouchDownHandler(sender, e);
            mJustSwitchedFromAnotherTool = false; // this prevents tap events from being rejected when we've 
            // tapped for the first time after creating the pan tool, but alsp lets the TapHandler check to prevent infinite loops
            //ProcessInputDown(e);
            //mViewerCanvas.CaptureTouch(e.TouchDevice);
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            mLastPoint = GetPosition(e, mPDFView);
            SelectAnnot((int)mLastPoint.X, (int)mLastPoint.Y);
            if (mAnnot == null)
            {
                mPDFView.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            else if (mAnnot.GetType() == Annot.Type.e_Link)
            {
                mNextToolMode = ToolManager.ToolType.e_link_action;
            }
            else if (IsWidget(mAnnot))
            {
                mNextToolMode = ToolManager.ToolType.e_form_fill;
            }
            else
            {
                mPDFView.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        internal override void MouseMovedHandler(object sender, MouseEventArgs e)
        {
            UIPoint currPoint = e.GetPosition(mPDFView);
            if (mIsDragging)
            {
                // This means we should move the View to follow the mouse
                if (currPoint.X != mLastPoint.X)
                {
                    mPDFView.SetHScrollPos(mPDFView.GetHScrollPos() - currPoint.X + mLastPoint.X);
                }
                if (currPoint.Y != mLastPoint.Y)
                {
                    mPDFView.SetVScrollPos(mPDFView.GetVScrollPos() - currPoint.Y + mLastPoint.Y);
                }
                mLastPoint = currPoint;
            }
            else
            {
                bool overLink = false;
                bool overWidget = false;

                pdftron.SDF.Obj annotObj = mPDFView.GetAnnotationAt(currPoint.X, currPoint.Y);
                if (annotObj != null)
                {
                    Annot annot = new Annot(annotObj);
                    if (mCurrentWidgetAnnot == annot)
                    {
                        overWidget = true;
                    }
                    else if (annot.GetType() == Annot.Type.e_Widget)
                    {

                        if (IsWidget(annot))
                        {
                            overWidget = true;
                            mCurrentWidgetPageNumber = mPDFView.GetPageNumberFromScreenPt(currPoint.X, currPoint.Y);
                            DrawWidgetFrame(annot, mCurrentWidgetPageNumber);
                            // draw small rectangle around Annot

                        }
                    }
                    if (annot.GetType() == Annot.Type.e_Link)
                    {
                        overLink = true;
                    }

                }

                if (overWidget)
                {
                    mPDFView.Cursor = System.Windows.Input.Cursors.Hand;
                }
                else
                {
                    mCurrentWidgetAnnot = null;
                    mWidgetRectangle.Visibility = Visibility.Collapsed;
                    if (overLink)
                    {
                        mPDFView.Cursor = System.Windows.Input.Cursors.Hand;
                    }
                    else
                    {
                        mPDFView.Cursor = System.Windows.Input.Cursors.Arrow;
                    }
                }
            }
        }

        internal override void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            mViewerCanvas.ReleaseMouseCapture();
            if (mIsDoubleClicking)
            {
                // if this release is after a double click, we want to open the note of a sticky annotation
                mIsDoubleClicking = false;
                UIPoint doubleClickPoint = e.GetPosition(mPDFView);

                if (GetDistance(doubleClickPoint, mDoubleClickDownPoint) < DOUBLE_CLICK_THRESHOLD)
                {
                    pdftron.SDF.Obj annotObj = mPDFView.GetAnnotationAt(doubleClickPoint.X, doubleClickPoint.Y);
                    if (annotObj != null)
                    {
                        Annot annot = new Annot(annotObj);
                        if (annot.IsValid() && annot.GetType() == Annot.Type.e_Text)
                        {
                            SelectionHelper selection = new SelectionHelper(mPDFView, this, annot, 
                                mPDFView.GetPageNumberFromScreenPt(doubleClickPoint.X, doubleClickPoint.Y));
                            selection.HandleDoubleClick();
                        }
                    }
                }
            }
        }

        internal override void MouseLeaveHandler(object sender, MouseEventArgs e)
        {
            base.MouseLeaveHandler(sender, e);
            mCurrentWidgetAnnot = null;
            mWidgetRectangle.Visibility = Visibility.Collapsed;
        }

        internal override void PreviewMouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            if (mIsDragging)
            {
                e.Handled = true;
            }
            else
            {
                base.PreviewMouseWheelHandler(sender, e);
            }
        }

        internal override void MouseDoubleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseDoubleClickHandler(sender, e);
            mDoubleClickDownPoint = e.GetPosition(mPDFView);
            // We want to handle double clicks when the mouse is released, as opposed to when it's pressed.
            mIsDoubleClicking = true;
        }

        public override void TapHandler(object sender, TouchEventArgs e)
        {
            base.TapHandler(sender, e);
            if (mJustSwitchedFromAnotherTool)
            {
                mJustSwitchedFromAnotherTool = false;
                return;
            }
            ProcessInputDown(e);
        }

        #endregion Event Handlers


        #region Utility Function

        protected bool IsWidget(Annot annot)
        {
            bool isWidget = false;
            if (annot.GetType() == Annot.Type.e_Widget)
            {
                try
                {
                    mPDFView.DocLockRead();
                    pdftron.PDF.Annots.Widget w = new pdftron.PDF.Annots.Widget(annot);
                    Field field = w.GetField();

                    if (field.IsValid() && !field.GetFlag(Field.Flag.e_read_only))
                    {
                        switch (field.GetType())
                        {
                            case Field.Type.e_check:
                            case Field.Type.e_radio:
                            case Field.Type.e_button:
                            case Field.Type.e_text:
                            case Field.Type.e_choice:
                                isWidget = true;
                                break;
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
            return isWidget;
        }

        protected void DrawWidgetFrame(Annot annot, int pageNumber)
        {
            PDFRect rect = annot.GetRect();
            ConvertPageRectToCanvasRect(rect, pageNumber);

            mWidgetRectangle.Width = rect.Width() + 2;
            mWidgetRectangle.Height = rect.Height() + 2;
            mWidgetRectangle.SetValue(Canvas.LeftProperty, rect.x1 - 1);
            mWidgetRectangle.SetValue(Canvas.TopProperty, rect.y1 - 1);
            mWidgetRectangle.Visibility = Visibility.Visible;
            mCurrentWidgetAnnot = annot;
        }

        #endregion Utility Functions

    }
}