using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

namespace pdftron.PDF.Tools
{
    public class FreehandCreate : SimpleShapeCreate
    {
        protected Path mShape;
        protected List<PDFPoint> mCurrentStrokeList = null; // A list of strokes
        protected List<List<PDFPoint>> mListOfStrokeLists = null; // Contains all the lists above
        //static List<List<PDFPoint>> newmListOfStrokeLists =null;
        protected PathFigureCollection mPathPoints;
        protected PathFigure mCurrentPathFigure;
        protected bool mFirstTime = false; // Whether or not a pointer has been pressed already.
        public static double mLeftmost, mRightmost, mTopmost, mBottommost;
        protected int mShapePageNumber = 0;
        protected bool mIgnoreMouseEvents = false;
        protected int strokePath = 0;
        protected bool mCreateMultiPathAnnotation = true;
        public static StringBuilder logtxt=new StringBuilder();
        DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);



        public bool CreateMultiPathAnnotation
        {
            get
            {
                return mCreateMultiPathAnnotation;
            }
            set
            {
                mCreateMultiPathAnnotation = value;
            }
        }
        public FreehandCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mNextToolMode = ToolManager.ToolType.e_pan;
            mToolMode = ToolManager.ToolType.e_ink_create;
            mListOfStrokeLists = new List<List<PDFPoint>>();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,0,400);
            dispatcherTimer.Stop();
        }
        /// <summary>
        /// We need to override the base class and add some functionality here in case we want multiple strokes 
        /// to be part of 1 ink annotation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Trace.WriteLine("timer up");
            mShapeHasBeenCreated = false;
            if (mIsDrawing)
            {
                Create();
                EndCurrentTool(ToolManager.ToolType.e_pan);
                mToolManager.createeink = true;
                dispatcherTimer.Tick -= new EventHandler(dispatcherTimer_Tick);
            }
           
        }

        public override void TouchUpHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            //base.TouchUpHandler(sender, e);
            //ProcessInputUp(e);
        }
        public override void StylusUpHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {           
            base.StylusUpHandler(sender, e);
            mShapeHasBeenCreated = false;
            //ProcessInputUp(e);
            dispatcherTimer.Start();
        }
        public override void StylusDownHandler(object sender, System.Windows.Input.StylusEventArgs e)
        {
            var id = e.StylusDevice.Id;
            if (id == SimpleShapeCreate.stylusid)
            {
                base.StylusDownHandler(sender, e);
                Trace.WriteLine("stylusdown");
                dispatcherTimer.Stop();
            }
        }

        public void ProcessInputUp(System.Windows.Input.StylusEventArgs e)
        {
            if (mCreateMultiPathAnnotation)
            {
                mShapeHasBeenCreated = false;
            }
            else if (mIsDrawing)
            {
                Create();
                EndCurrentTool(ToolManager.ToolType.e_pan);
            }
        }

        //internal override void KeyDownAction(object sender, System.Windows.Input.KeyEventArgs e)
        //{
        //    if (!mIsDragging)
        //    {
        //        if (e.Key == System.Windows.Input.Key.Y)
        //        {
        //            Create();
        //            EndCurrentTool(ToolManager.ToolType.e_pan);
        //        }
        //        else if (e.Key == System.Windows.Input.Key.N)
        //        {
        //            EndCurrentTool(ToolManager.ToolType.e_pan);
        //        }
        //    }
        //}


        #region Override Functions

        protected override void Draw()
        {
            mPDFView.DocLock(true);
            if (mIgnoreMouseEvents)
            {
                return;
            }

            double x, y;

            if (!mShapeHasBeenCreated)
            {
               mShapeHasBeenCreated = true;
                x = mDownPoint.X - mPDFView.GetHScrollPos();
                y = mDownPoint.Y - mPDFView.GetVScrollPos();
                mPDFView.ConvScreenPtToPagePt(ref x, ref y, mDownPageNumber);
                //Console.WriteLine("PT is"+x+" "+y);
                if (!mFirstTime)
                {
                    mFirstTime = true;
                    mShapePageNumber = mDownPageNumber; // from now on, mouse presses outside of this page number will be ignored

                    // set bounds
                    mLeftmost = x;
                    mRightmost = x;
                    mTopmost = y;
                    mBottommost = y;
                    mPathPoints = new PathFigureCollection();
                }  

                mHalfThickness = mStrokeThickness / 2;
                mCurrentPathFigure = new PathFigure();
                mCurrentPathFigure.StartPoint = new UIPoint(mDownPoint.X - mPageCropOnClient.x1, mDownPoint.Y - mPageCropOnClient.y1);
                mPathPoints.Add(mCurrentPathFigure);
                mListOfStrokeLists.Add(new List<PDFPoint>());
                Trace.WriteLine("addnewstrokelist");
                mCurrentStrokeList = mListOfStrokeLists[mListOfStrokeLists.Count() - 1];

                mCurrentStrokeList.Add(new PDFPoint(x, y));

                PathGeometry geom = new PathGeometry();
                mShape = new Path();
                mShape.StrokeLineJoin = PenLineJoin.Miter;
                mShape.StrokeMiterLimit = mDrawThickness/2;
                mShape.Data = geom;
                mShape.StrokeThickness = mDrawThickness;
                mShape.Stroke = mDrawBrush;
                
                this.Children.Add(mShape);
            }

            mCurrentPathFigure.Segments.Add(new LineSegment() { Point = new UIPoint(mDragPoint.X - 
                mPageCropOnClient.x1, mDragPoint.Y - mPageCropOnClient.y1) });

            x = mDragPoint.X - mPDFView.GetHScrollPos();
            y = mDragPoint.Y - mPDFView.GetVScrollPos();
            mPDFView.ConvScreenPtToPagePt(ref x, ref y, mDownPageNumber);
            //Console.WriteLine("I am drawing "+x+" "+y);
            mCurrentStrokeList.Add(new PDFPoint(x, y));
            // Update bounds
            if (x < mLeftmost)
            {
                mLeftmost = x;
            }
            if (x > mRightmost)
            {
                mRightmost = x;
            }
            if (y > mTopmost)
            {
                mTopmost = y;
            }
            if (y < mBottommost)
            {
                mBottommost = y;
            }

            (mShape.Data as PathGeometry).Figures = mPathPoints;
            mPDFView.DocUnlock();
        }

        protected override void Create()
        {
            if (mListOfStrokeLists.Count() == 0)
            {
                return;
            }

            try
            {
                //Trace.WriteLine("strokenum is "+ mListOfStrokeLists.Count()); 
                mPDFView.DocLock(true);
                double shape= (mRightmost - mLeftmost) / (mTopmost - mBottommost);
                if (shape > 20)
                {
                    mBottommost -= 10;
                    mTopmost += 10;
                }else if (shape < 0.05)
                {
                    mLeftmost -= 10;
                    mRightmost += 10;
                }

                Rect rect = new Rect(mLeftmost, mBottommost, mRightmost, mTopmost);
                //Console.WriteLine("rect is "+ mLeftmost+" "+mBottommost+" " + " "+mRightmost+" " + mTopmost);
                if (rect != null)
                {
                    pdftron.PDF.Annots.Ink ink = pdftron.PDF.Annots.Ink.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                    Annot.BorderStyle bs = ink.GetBorderStyle();
                    bs.width = mStrokeThickness;
                    ink.SetBorderStyle(bs);

                    double sx = mPDFView.GetHScrollPos();
                    double sy = mPDFView.GetVScrollPos();

                    // Shove the points into the Doc
                    int i = 0;
                    PDFPoint pdfp = new PDFPoint();
                    foreach (List<PDFPoint> pointList in mListOfStrokeLists)
                    {
                        int j = 0;
                        foreach (PDFPoint p in pointList)
                        {
                            pdfp.x = p.x;
                            pdfp.y = p.y;
                            ink.SetPoint(i, j, pdfp);
                            j++;
                        }
                        i++;
                        Trace.WriteLine("stroke i = "+i);
                    }

                    SDF.Obj mp_obj = ink.GetSDFObj();
                    SDF.Obj mk_dict = mp_obj.FindObj("MK");

                    if (mk_dict == null || !mk_dict.IsDict())
                    {
                        mk_dict = mp_obj.PutDict("MK");
                    }
                    mk_dict.PutBool("__smooth_with_bezier_curve", false);
                    SetStyle(ink);
                    ink.RefreshAppearance();
                    if (mDownPageNumber > 0)
                    {
                        pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                        page.AnnotPushBack(ink);
                        mAnnotPageNum = mDownPageNumber;
                        mAnnot = ink;
                        mPDFView.Update(mAnnot, mAnnotPageNum);
                        logtxt.Append("new ink " + "Time: " + DateTime.Now.ToString() + "\r\n");
                    }
                    //AnnotEffect(ink);

                }
            }
            catch (System.Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }
            mToolManager.RaiseAnnotationAddedEvent(mAnnot);
            mToolManager.DelayRemoveTimers.Add(new PDFViewWPFToolsCS2013.Utilities.DelayRemoveTimer(mViewerCanvas, this, this, mDownPageNumber));
        }

        
        private void AnnotEffect(Annots.Ink newink)
        {
            for (int h = 0; h < 3; h++)
            {
                for (int i = 0; i < 5; i++)
                {
                    double opacity = newink.GetOpacity();
                    newink.SetOpacity(opacity - 0.2);
                    newink.RefreshAppearance();
                }
                for (int j = 0; j < 5; j++)
                {
                    double opacity = newink.GetOpacity();
                    newink.SetOpacity(opacity + 0.2);
                    newink.RefreshAppearance();
                }
            }
        }
        protected override void CheckBounds(ref UIPoint point)
        {
            if (mPageCropOnClient != null)
            {
                if (point.X < mPageCropOnClient.x1 + mHalfThickness)
                {
                    point.X = mPageCropOnClient.x1 + mHalfThickness;
                }
                if (point.X > mPageCropOnClient.x2 - mHalfThickness)
                {
                    point.X = mPageCropOnClient.x2 - mHalfThickness;
                }
                if (point.Y < mPageCropOnClient.y1 + mHalfThickness)
                {
                    point.Y = mPageCropOnClient.y1 + mHalfThickness;
                }
                if (point.Y > mPageCropOnClient.y2 - mHalfThickness)
                {
                    point.Y = mPageCropOnClient.y2 - mHalfThickness;
                }
            }
        }

        protected override void HandleResizing()
        {
            //if (!mFirstTime)
            //{
            //    return;
            //}

            //mPageCropOnClient = BuildPageBoundBoxOnClient(mDownPageNumber);
            //this.SetValue(System.Windows.Controls.Canvas.LeftProperty, mPageCropOnClient.x1);
            //this.SetValue(System.Windows.Controls.Canvas.TopProperty, mPageCropOnClient.y1);
            //this.Width = mPageCropOnClient.x2 - mPageCropOnClient.x1;
            //this.Height = mPageCropOnClient.y2 - mPageCropOnClient.y1;

            //double sx = mPDFView.GetHScrollPos();
            //double sy = mPDFView.GetVScrollPos();
            //double x, y;

            //mLeftmost = -1;
            //mRightmost = -1;
            //mTopmost = -1;
            //mBottommost = -1;

            //mZoomLevel = mPDFView.GetZoom();
            //mDrawThickness = mStrokeThickness * mZoomLevel;
            //mHalfThickness = mDrawThickness / 2;

            //this.Children.Clear();

            //PathGeometry geom = new PathGeometry();
            //mShape = new Path();
            //mShape.StrokeLineJoin = PenLineJoin.Miter;
            //mShape.StrokeMiterLimit = 1;
            //mShape.Data = geom;
            //mShape.StrokeThickness = mDrawThickness;
            //mShape.Stroke = mStrokeBrush;
            //this.Children.Add(mShape);

            //mPathPoints = new PathFigureCollection();

            //foreach (List<PDFPoint> strokeList in mListOfStrokeLists)
            //{
            //    mCurrentPathFigure = new PathFigure();
            //    mPathPoints.Add(mCurrentPathFigure);
            //    bool first = true;
            //    foreach (PDFPoint point in strokeList)
            //    {
            //        x = point.x;
            //        y = point.y;
            //        mPDFView.ConvPagePtToScreenPt(ref x, ref y, mDownPageNumber);

            //        if (first)
            //        {
            //            first = false;
            //            mCurrentPathFigure.StartPoint = new UIPoint(x + sx - mPageCropOnClient.x1, y + sy - mPageCropOnClient.y1);
            //        }
            //        else
            //        {
            //            mCurrentPathFigure.Segments.Add(new LineSegment() { Point = new UIPoint(x + sx - 
            //                mPageCropOnClient.x1, y + sy - mPageCropOnClient.y1) });
            //        }

            //    }
            //}
            //(mShape.Data as PathGeometry).Figures = mPathPoints;
        }

        protected override void MultiTouchDetected()
        {
            //if (mPathPoints.Contains(mCurrentPathFigure))
            //{
            //    mPathPoints.Remove(mCurrentPathFigure);
            //}
            //if (mListOfStrokeLists.Contains(mCurrentStrokeList))
            //{
            //    mListOfStrokeLists.Remove(mCurrentStrokeList);
            //}
        }

        #endregion Override Functions


        #region Context Menu

        public override void AddContextMenuItems(System.Windows.Controls.ContextMenu menu, double x, double y)
        {
            base.AddContextMenuItems(menu, x, y);

            // We're drawing a shape
            if (mFirstTime)
            {
                Separator sep = new Separator();
                menu.Items.Add(sep);

                MenuItem m = new MenuItem();
                m.Header = "Save Shape";
                m.Click += ContextMenu_Finish;
                menu.Items.Add(m);


                m = new MenuItem();
                m.Header = "Discard Shape";
                m.Click += ContextMenu_Discard;
                menu.Items.Add(m);
            }
        }

        private void ContextMenu_Finish(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveShape();
        }

        private void ContextMenu_Discard(object sender, System.Windows.RoutedEventArgs e)
        {
            DiscardShape();
        }


        #endregion Context Menu


        /// <summary>
        /// Save the current drawing
        /// </summary>
        public void SaveShape()
        {
            Create();
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }

        // Discard the current drawing
        public void DiscardShape()
        {
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }
    }
}
