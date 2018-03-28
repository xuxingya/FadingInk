using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using pdftron.PDF;

using UIPoint = System.Windows.Point;
using UIRect = System.Windows.Rect;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.Common;

namespace pdftron.PDF.Tools
{

    public class RectCreate : SingleDragShapeCreate
    {
        protected System.Windows.Shapes.Path mShape;
        protected RectangleGeometry mRectangleGeometry;
        


        public RectCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mNextToolMode = ToolManager.ToolType.e_rect_create;
            mToolMode = ToolManager.ToolType.e_rect_create;
        }

        #region Override Functions

        protected override void Draw()
        {
            if (!mShapeHasBeenCreated)
            {
                mShapeHasBeenCreated = true;

                mRectangleGeometry = new RectangleGeometry();
                mShape = new Path();
                mShape.Data = mRectangleGeometry;
                if (mUseStroke)
                {
                    mShape.Stroke = mStrokeBrush;
                }
                if (mUseFill)
                {
                    mShape.Fill = mFillBrush;
                }

                this.Children.Add(mShape);
            }
            else
            {

                double tempThickness = Math.Min(mDrawThickness, Math.Min(this.Width, this.Height) / 2);
                mShape.StrokeThickness = tempThickness;

                double minX = Math.Min(mDownPoint.X, mDragPoint.X) - mPageCropOnClient.x1;
                double minY = Math.Min(mDownPoint.Y, mDragPoint.Y) - mPageCropOnClient.y1;
                double xDist = Math.Abs(mDownPoint.X - mDragPoint.X);
                double yDist = Math.Abs(mDownPoint.Y - mDragPoint.Y);

                mRectangleGeometry.Rect = new UIRect(minX, minY, xDist, yDist);
            }
        }

        protected override void Create()
        {
            try
            {
                mPDFView.DocLock(true);

                // get a bounding rectangle
                PDFRect rect = GetShapeBBox();
                rect.Inflate(mStrokeThickness / 2);

                // create a square annotation with the rectangle
                pdftron.PDF.Annots.Square square = pdftron.PDF.Annots.Square.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                SetStyle(square, true);
                square.RefreshAppearance();

                pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                page.AnnotPushBack(square);

                mAnnotPageNum = mDownPageNumber;
                mAnnot = square;
                mPDFView.Update(square, mDownPageNumber);
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