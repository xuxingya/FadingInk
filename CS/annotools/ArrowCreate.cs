using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using pdftron.PDF;

using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.Common;




namespace pdftron.PDF.Tools
{

    

    public class ArrowCreate : LineCreate
    {
        protected PathGeometry mPathGeometry;
        protected UIPoint aPt1, aPt2, aPt3; // the end points of the shorter lines that form the arrow
        protected double mArrowHeadLength;
        protected double mCos, mSin;

        public ArrowCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_arrow_create;
            mNextToolMode = ToolManager.ToolType.e_arrow_create;

            mCos = Math.Cos(3.14159265 / 6); //30 degree
            mSin = Math.Sin(3.14159265 / 6);
        }

        internal override void OnCreate()
        {
            base.OnCreate();
        }

        #region Override Functions

        protected override void Draw()
        {
            if (!mShapeHasBeenCreated)
            {
                mArrowHeadLength = mZoomLevel * GetLineEndingLength(mStrokeThickness); // matches the ones drawn by PDFNet
                mShapeHasBeenCreated = true;
                mHalfThickness = mStrokeThickness / 2;
                mShape = new System.Windows.Shapes.Path();
                mShape.Opacity = mOpacity;
                PathGeometry mPathGeometry = new PathGeometry();
                mShape.StrokeLineJoin = PenLineJoin.Miter;
                mShape.StrokeMiterLimit = 2;
                mShape.Data = mPathGeometry;               
                mShape.StrokeThickness = mDrawThickness;
                mShape.Stroke = mStrokeBrush;
                this.Children.Add(mShape);
            }
            else
            {
                CalcArrow();
                UIPoint tip = new UIPoint(mDownPoint.X - mPageCropOnClient.x1, mDownPoint.Y - mPageCropOnClient.y1);
                UIPoint heel = new UIPoint(mDragPoint.X - mPageCropOnClient.x1, mDragPoint.Y - mPageCropOnClient.y1);

                PathFigureCollection arrowPoints = new PathFigureCollection();

                // arrow head
                PathFigure a_head = new PathFigure();
                a_head.StartPoint = aPt1;
                a_head.Segments.Add(new LineSegment() { Point = tip });
                a_head.Segments.Add(new LineSegment() { Point = aPt2 });
                arrowPoints.Add(a_head);

                // arrow shaft
                PathFigure a_shaft = new PathFigure();
                a_shaft.StartPoint = aPt3;
                a_shaft.Segments.Add(new LineSegment() { Point = heel });
                arrowPoints.Add(a_shaft);

                (mShape.Data as PathGeometry).Figures = arrowPoints;
            }
        }

        protected override void Create()
        {
            try
            {
                mPDFView.DocLock(true);

                // transform the 2 points to page space
                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();

                double x1 = mDownPoint.X - sx;
                double y1 = mDownPoint.Y - sy;
                double x2 = mDragPoint.X - sx;
                double y2 = mDragPoint.Y - sy;

                mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mDownPageNumber);
                mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mDownPageNumber);

                // get a bounding rectangle
                PDFRect rect = new PDFRect(x1, y1, x2, y2);
                rect.Normalize();
                rect.Inflate(mHalfThickness + GetLineEndingLength(mStrokeThickness));

                // create a line annotation with the rectangle
                pdftron.PDF.Annots.Line line = pdftron.PDF.Annots.Line.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                // Add a style to the tip of the line
                line.SetStartStyle(pdftron.PDF.Annots.Line.EndingStyle.e_OpenArrow);

                // we need to explicitly set start and endpoints, else it'll go from the rect's bottom left to top right corner
                line.SetStartPoint(new Point(x1, y1));
                line.SetEndPoint(new Point(x2, y2));

                SetStyle(line);
                line.RefreshAppearance();

                pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                page.AnnotPushBack(line);

                mAnnotPageNum = mDownPageNumber;
                mAnnot = line;
                mPDFView.Update(line, mDownPageNumber);
            }
            catch (System.Exception)
            {

            }
            finally
            {
                mPDFView.DocUnlock();
            }
            mToolManager.RaiseAnnotationAddedEvent(mAnnot);
        }

        #endregion Override Functions

        #region Utility Functions

        private void CalcArrow()
        {
            // aPt1 and aPt2 are the ends of the two lines forming the arrow head.
            double dx = mDragPoint.X - mDownPoint.X;
            double dy = mDragPoint.Y - mDownPoint.Y;
            double len = (dx * dx) + (dy * dy);

            if (len != 0)
            {
                len = Math.Sqrt(len);
                dx /= len;
                dy /= len;

                double dx1 = (dx * mCos) - (dy * mSin);
                double dy1 = (dy * mCos) + (dx * mSin);
                aPt1.X = (mArrowHeadLength * dx1) + mDownPoint.X - mPageCropOnClient.x1;
                aPt1.Y = (mArrowHeadLength * dy1) + mDownPoint.Y - mPageCropOnClient.y1;


                double dx2 = (dx * mCos) + (dy * mSin);
                double dy2 = (dy * mCos) - (dx * mSin);
                aPt2.X = (mArrowHeadLength * dx2) + mDownPoint.X - mPageCropOnClient.x1;
                aPt2.Y = (mArrowHeadLength * dy2) + mDownPoint.Y - mPageCropOnClient.y1;

                // offset the top of the shaft by mDrawThickness, so that it's thickness doesn't blunt the tip.
                // mDrawThickness works because we have exactly 30 degree offset for the arrows.
                aPt3.X = (mDrawThickness * dx) + mDownPoint.X - mPageCropOnClient.x1;
                aPt3.Y = (mDrawThickness * dy) + mDownPoint.Y - mPageCropOnClient.y1;
            }
        }

        #endregion Utility Functions


    }
}