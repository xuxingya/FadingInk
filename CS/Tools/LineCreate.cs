using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;

using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;



namespace pdftron.PDF.Tools
{

    public class LineCreate : SingleDragShapeCreate
    {
        protected Path mShape;
        protected LineGeometry mLineGeometry;


        public LineCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_line_create;
            mNextToolMode = ToolManager.ToolType.e_line_create;
        }




        #region Override Functions

        protected override void Draw()
        {
            if (!mShapeHasBeenCreated)
            {
                mShapeHasBeenCreated = true;
                mShape = new Path();
                mShape.Opacity = mOpacity;

                mLineGeometry = new LineGeometry();
                mLineGeometry.StartPoint = new UIPoint(mDownPoint.X - mPageCropOnClient.x1, mDownPoint.Y - mPageCropOnClient.y1);
                mLineGeometry.EndPoint = new UIPoint(mDragPoint.X - mPageCropOnClient.x1, mDragPoint.Y - mPageCropOnClient.y1);
                mShape.Data = mLineGeometry;
                mShape.StrokeThickness = mDrawThickness;
                mShape.Stroke = mStrokeBrush;
                this.Children.Add(mShape);

            }
            else
            {
                mLineGeometry.EndPoint = new UIPoint(mDragPoint.X - mPageCropOnClient.x1, mDragPoint.Y - mPageCropOnClient.y1);
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
                rect.Inflate(mStrokeThickness / 2);

                // create a line annotation with the rectangle
                pdftron.PDF.Annots.Line line = pdftron.PDF.Annots.Line.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

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


        #endregion Override Functions
    }
}