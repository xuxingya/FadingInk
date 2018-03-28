using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Threading;
using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;
using pdftron.Common;

namespace pdftron.PDF.Tools
{

    public class SimpleShapeCreate : Tool
    {

        protected int START_DRAWING_THRESHOLD = 5;

        protected bool mIsDrawing = false;
        protected UIPoint mDownPoint;
        protected UIPoint mDragPoint;
        protected int mDownPageNumber = -1;
        protected PDFRect mPageCropOnClient;

        // Visuals for shapes
        protected double mStrokeThickness = 1;
        protected double mZoomLevel = 1;
        protected double mDrawThickness;
        protected SolidColorBrush mStrokeBrush;
        protected SolidColorBrush mDrawBrush;
        protected SolidColorBrush mFillBrush;
        protected double mOpacity = 1.0;
        protected bool mUseStroke = true;
        protected bool mUseFill = false;
        protected bool mShapeHasBeenCreated = false;
        protected double mHalfThickness = 1;

        // Transforms (most shape creation tools will use these two)
        protected TransformGroup mShapeTransform;
        protected ScaleTransform mScaleTransform;
        protected TranslateTransform mTranslateTransform;

        protected bool mMultiTouch = false;



        public SimpleShapeCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mViewerCanvas = mToolManager.AnnotationCanvas;
            DisallowTextSelection();

        }

        internal override void OnCreate()
        {

            // Fetch style data from Settings
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.StrokeColor;
            //color of the stroke
            col.R = 0;
            col.G = 0;
            col.B = 0;
            mUseStroke = col.Use;
            mStrokeBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));
            if (mToolManager.FadingOn == true)
            {
                mDrawBrush = new SolidColorBrush(Color.FromArgb(255, 99, 177, 175));
            }
            else
            {
                mDrawBrush = mStrokeBrush;
            }
            //mDrawBrush = mStrokeBrush;
            col = pdftron.PDF.Tools.Utilities.ColorSettings.FillColor;
            mUseFill = col.Use;
            mFillBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

            mStrokeThickness = pdftron.PDF.Tools.Properties.Settings.Default.StrokeThickness;

            mOpacity = pdftron.PDF.Tools.Properties.Settings.Default.MarkupOpacity;
        }

        internal override void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            HandleResizing();
        }

        internal override void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            HandleResizing();
        }



        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
           // base.TouchDownHandler(sender, e);
            //e.Handled = true;
            //if (mToolManager.TouchIDs.Count > 1)
            //{
            //    mMultiTouch = true;
            //    MultiTouchDetected();
            //    return;
            //}
            //mMultiTouch = false;
            //ProcessInputDown(e);

            //mViewerCanvas.CaptureTouch(e.TouchDevice);
            //e.Handled = true;
        }

        private int stylusid =2;
        private int offset = 55;
        public override void StylusDownHandler(object sender, StylusEventArgs e)
        {
            base.StylusDownHandler(sender, e);
            var id = e.StylusDevice.Id;
            Trace.WriteLine("stylus down process id is " + id);
            if (id == stylusid)
            {               
                ProcessInputDown(e);
                mViewerCanvas.CaptureStylus();
            }
            //e.Handled = true;
        }
        private void ProcessInputDown(InputEventArgs e)
        {
            PDFDoc doc = mPDFView.GetDoc();
            if (doc == null)
            {
                mNextToolMode = ToolManager.ToolType.e_pan;
            }

            UIPoint screenPoint = GetPosition(e, mPDFView);
            // Ensure we're on a page

            mDownPageNumber = mPDFView.GetPageNumberFromScreenPt(screenPoint.X, screenPoint.Y);
            if (mDownPageNumber < 1)
            {
                return;
            }
            // Get the page's bounding box in canvas space
            mPageCropOnClient = BuildPageBoundBoxOnClient(mDownPageNumber);

            mZoomLevel = mPDFView.GetZoom();
            mDrawThickness = mStrokeThickness * mZoomLevel;
            mHalfThickness = mDrawThickness / 2;

            mDownPoint = GetPosition(e, mViewerCanvas);
            mDownPoint.X += mPDFView.GetHScrollPos();
            mDownPoint.Y -= offset - mPDFView.GetVScrollPos();
            CheckBounds(ref mDownPoint);

            mDragPoint = new UIPoint(mDownPoint.X, mDownPoint.Y);
        }



        public override void TouchMoveHandler(object sender, TouchEventArgs e)
        {
            //base.TouchMoveHandler(sender, e);
            //if (!mMultiTouch)
            //{
            //    ProcessInputMove(e);
            //}
            //Console.WriteLine("touch move process");
            //e.Handled = true;
        }

        public override void StylusMoveHandler(object sender, StylusEventArgs e)
        {
            var id = e.StylusDevice.Id;
            base.StylusMoveHandler(sender, e);
            if (id== stylusid)
            {
                ProcessInputMove(e);
                //Console.WriteLine("stylus move process id is "+id);
            }
            //e.Handled = true;
        }

        private void ProcessInputMove(InputEventArgs e)
        {
            mDragPoint = GetPosition(e, mViewerCanvas);
            mDragPoint.X += mPDFView.GetHScrollPos();
            mDragPoint.Y -= offset - mPDFView.GetVScrollPos();;
            CheckBounds(ref mDragPoint);
            CheckScreenBounds(ref mDragPoint);
            if (!mIsDrawing && mDownPageNumber > 0)
            {
                // wait until you're at least START_DRAWING_THRESHOLD distance from the start
                //if ((Math.Abs(mDownPoint.X - mDragPoint.X) > START_DRAWING_THRESHOLD)
                //    || (Math.Abs(mDownPoint.Y - mDragPoint.Y) > START_DRAWING_THRESHOLD))
                //{
                    mIsDrawing = true;  
                    mViewerCanvas.Children.Insert(0, this);
                    this.SetValue(Canvas.LeftProperty, mPageCropOnClient.x1);
                    this.SetValue(Canvas.TopProperty, mPageCropOnClient.y1);
                    this.Width = mPageCropOnClient.x2 - mPageCropOnClient.x1;
                    this.Height = mPageCropOnClient.y2 - mPageCropOnClient.y1;
                    this.Opacity = mOpacity;
                    Draw();
              //  }
            }
            if (mIsDrawing)
            {
                Draw();
            }
        }


        public override void TouchUpHandler(object sender, TouchEventArgs e)
        {
            //base.TouchUpHandler(sender, e);
            //mViewerCanvas.ReleaseTouchCapture(e.TouchDevice);
            // e.Handled = true;
        }
        public override void StylusUpHandler(object sender, StylusEventArgs e)
        {
            //base.StylusUpHandler(sender, e);
            //e.Handled = true;
            //var device = e.StylusDevice;
            //if(device.Captured== mViewerCanvas)
            //mViewerCanvas.ReleaseStylusCapture();
        }
        /// <summary>
        /// If ctrl is pressed, this will zoom in or out on the PDFViewCtrl.
        /// 
        /// Note: This only works if the PDFViewCtrl is in focus, otherwise, it won't.
        /// The reason is that the modifier keys (ctrl) are hooked up only to the PDFViewWPF to keep the tool more modular.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>




        //////////////////////////////////////////////////////////////////////////
        // These functions can be extended by the specific tools if the behavior is not desired
        // In some cases, it has to be extended.
        #region Virtual Functions

        /// <summary>
        /// This needs to be implemented by the specific tool to determine what the tool should look like when drawn.
        /// </summary>
        protected virtual void Draw()
        {
            return;
        }


        /// <summary>
        /// This needs to be implemented by the specific tool in order to add the annotation to the document.
        /// </summary>
        protected virtual void Create()
        {
            return;
        }

        /// <summary>
        /// Creates a bounding box that is tightly bounding mDownPoint and mDragPoint
        /// </summary>
        /// <returns>A tight PDFRect</returns>
        protected virtual PDFRect GetShapeBBox()
        {
            //computes the bounding box of the rubber band in page space.
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            double x1 = mDownPoint.X - sx;
            double y1 = mDownPoint.Y - sy;
            double x2 = mDragPoint.X - sx;
            double y2 = mDragPoint.Y - sy;

            mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mDownPageNumber);
            mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mDownPageNumber);

            pdftron.PDF.Rect rect;
            rect = new pdftron.PDF.Rect(x1, y1, x2, y2);
            rect.Normalize();
            return rect;
        }


        protected virtual void CheckBounds(ref UIPoint point)
        {
            if (mPageCropOnClient != null)
            {
                if (point.X < mPageCropOnClient.x1)
                {
                    point.X = mPageCropOnClient.x1;
                }
                if (point.X > mPageCropOnClient.x2)
                {
                    point.X = mPageCropOnClient.x2;
                }
                if (point.Y < mPageCropOnClient.y1)
                {
                    point.Y = mPageCropOnClient.y1;
                }
                if (point.Y > mPageCropOnClient.y2)
                {
                    point.Y = mPageCropOnClient.y2;
                }
            }
        }


        protected virtual void HandleResizing()
        {
            return;
        }


        protected virtual void MultiTouchDetected()
        {
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }


        #endregion Virtual Functions

        #region Utility Functions

        /// <summary>
        /// Expects a point relative to the canvas, and will ensure that it's not larger than the screen point.
        /// </summary>
        /// <param name="point"></param>
        protected void CheckScreenBounds(ref UIPoint point)
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();
            if (point.X < sx)
            {
                point.X = sx;
            }
            if (point.X > sx + mPDFView.ActualWidth)
            {
                point.X = sx + mPDFView.ActualWidth;
            }
            if (point.Y < sy)
            {
                point.Y = sy;
            }
            if (point.Y > sy + mPDFView.ActualHeight)
            {
                point.Y = sy + mPDFView.ActualHeight;
            }
        }


        /// <summary>
        /// Sets the look of a markup annotation.
        /// </summary>
        /// <param name="annot">The annotation</param>
        /// <param name="hasFill">Whether the annotation has a fill color or not</param>
        protected void SetStyle(pdftron.PDF.Annots.Markup annot, bool hasFill = false)
        {
            double r = mStrokeBrush.Color.R / 255.0;
            double g = mStrokeBrush.Color.G / 255.0;
            double b = mStrokeBrush.Color.B / 255.0;
            //if(mToolManager.FadingOn == true)
            //{
            //    r = 99/255.0;
            //    g = 177 / 255.0;
            //    b = 175 / 255.0;
            //}

            ColorPt color = new ColorPt(r, g, b);
            if (!hasFill || mUseStroke)
            {
                annot.SetColor(color, 3);
            }
            else
            {
                annot.SetColor(color, 0); // 0 for transparent
            }
            if (hasFill)
            {
                r = mFillBrush.Color.R / 255.0;
                g = mFillBrush.Color.G / 255.0;
                b = mFillBrush.Color.B / 255.0;
                color = new ColorPt(r, g, b);
                if (mUseFill)
                {
                    annot.SetInteriorColor(color, 3);
                }
                else
                {
                    annot.SetInteriorColor(color, 0); // 0 for transparent
                }
            }
            pdftron.PDF.Annot.BorderStyle bs = annot.GetBorderStyle();
            bs.width = mStrokeThickness;
            annot.SetBorderStyle(bs);
            annot.SetOpacity(mOpacity);
            annot.SetContents("1");
        } 

        #endregion Utility Functions

    }

}