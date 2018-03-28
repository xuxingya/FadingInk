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

    public class OvalCreate : SingleDragShapeCreate
    {
        protected System.Windows.Shapes.Path mShape;
        protected EllipseGeometry mEllipseGeometry;



        public OvalCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mNextToolMode = ToolManager.ToolType.e_oval_create;
            mToolMode = ToolManager.ToolType.e_oval_create;
        }

        #region Override Functions

        protected override void Draw()
        {
            if (!mShapeHasBeenCreated)
            {
                mShapeHasBeenCreated = true;
                mEllipseGeometry = new EllipseGeometry();
                mShape = new Path();
                mShape.Data = mEllipseGeometry;
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
                // Don't draw larger than canvas / 2, since it looks smoother
                double tempThickness = Math.Min(mDrawThickness, Math.Min(this.Width, this.Height) / 2);
                mShape.StrokeThickness = tempThickness;

                double xDist = Math.Abs(mDownPoint.X - mDragPoint.X);
                double yDist = Math.Abs(mDownPoint.Y - mDragPoint.Y);

                mEllipseGeometry.Center = new UIPoint(((mDownPoint.X + mDragPoint.X) / 2) - mPageCropOnClient.x1, 
                    ((mDownPoint.Y + mDragPoint.Y) / 2) - mPageCropOnClient.y1);
                mEllipseGeometry.RadiusX = (xDist / 2) - (tempThickness / 2);
                mEllipseGeometry.RadiusY = (yDist / 2) - (tempThickness / 2);
            }
        }

        protected override void Create()
        {
            try
            {
                mPDFView.DocLock(true);

                // get a bounding rectangle
                PDFRect rect = GetShapeBBox();
                rect.Normalize();
                //rect.Inflate(mHalfThickness);

                // create a line annotation with the rectangle
                pdftron.PDF.Annots.Circle circle = pdftron.PDF.Annots.Circle.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                SetStyle(circle, true);
                circle.RefreshAppearance();

                pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                page.AnnotPushBack(circle);

                mAnnotPageNum = mDownPageNumber;
                mAnnot = circle;
                mPDFView.Update(circle, mDownPageNumber);
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

    }
}