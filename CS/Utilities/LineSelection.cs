using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Shapes;

using UIPoint = System.Windows.Point;

using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFLine = pdftron.PDF.Annots.Line;


namespace pdftron.PDF.Tools
{
    /// <summary>
    /// This class is used by Annot Edit to handle lines and arrow especially.
    /// </summary>
    internal class LineSelection : SelectionHelper
    {
        protected PDFLine mLine;

        protected Path mSelectionLine;
        protected LineGeometry mLineGeometry;

        //////////////////////////////////////////////////////////////////////////
        // Control points
        protected const int E_START = 1;
        protected const int E_END = 2;
        protected const double MINIMUM_LINE_DISTANCE_THRESHOLD = 8;

        // The line's start and end points
        protected UIPoint mStartPoint;
        protected UIPoint mEndPoint;
        // The lines start and endpoint as we move the shape.
        protected UIPoint mMovingStartPoint;
        protected UIPoint mMovingEndPoint;


        /// <summary>
        /// Minimal constructor, which creates a minimal object so that it can be compared against selectionHelpers in Containers.
        /// </summary>
        /// <param name="annot"></param>
        internal LineSelection(Annot annot)
            : base(annot)
        {

        }


        /// <summary>
        /// Main Constructor, will create a full fledged object.
        /// </summary>
        /// <param name="view">the PDGFViewCtrl</param>
        /// <param name="annot">The Line Annotation</param>
        /// <param name="pageNumber">The Annotation's page number</param>
        internal LineSelection(PDFViewWPF view, Tool tool, Annot annot, int pageNumber)
            : base(view, tool, annot, pageNumber)
        {
            mLine = new PDFLine(annot);
            mStartPoint = new UIPoint();
            mEndPoint = new UIPoint();
            mMovingStartPoint = new UIPoint();
            mMovingEndPoint = new UIPoint();

            CalculateBoundingBox();
        }




        #region override functions

        /// <summary>
        /// Gets the current control point from the annotation.
        /// </summary>
        /// <param name="point">A point in Canvas Space (i.e. screen space + scroll)</param>
        /// <returns></returns>
        internal override int GetControlPoint(UIPoint point)
        {
            mDownPoint = point;
            mEffectiveControlPoint = -1;

            double shortestDistance = GetControlPointDistanceThreshold();
            double dist;

            // Find the closest control point
            for (int i = 0; i < 2; i++)
            {
                dist = ((mSelectionEllipseGeometries[i].Center.X - point.X) * (mSelectionEllipseGeometries[i].Center.X - point.X))
                    + ((mSelectionEllipseGeometries[i].Center.Y - point.Y) * (mSelectionEllipseGeometries[i].Center.Y - point.Y));
                if (dist <= shortestDistance)
                {
                    shortestDistance = dist;
                    mEffectiveControlPoint = i;
                }
            }

            if (mEffectiveControlPoint == E_CENTER)
            {
                mEffectiveControlPoint = E_END;
            }

            if (mEffectiveControlPoint < 0)
            {
                dist = PointToLineDistance(point.X, point.Y);
                double thresh = MINIMUM_LINE_DISTANCE_THRESHOLD;
                if (IsUsingTouch)
                {
                    thresh *= 4;
                }
                try
                {
                    mPDFView.DocLockRead();

                    pdftron.PDF.Annot.BorderStyle bs = mLine.GetBorderStyle();
                    thresh = Math.Max(thresh, bs.width * mPDFView.GetZoom());
                }
                catch (System.Exception)
                { }
                finally
                {
                    mPDFView.DocUnlockRead();
                }
                if (dist < thresh)
                {
                    mEffectiveControlPoint = E_CENTER;
                }
            }
            return mEffectiveControlPoint;

            
        }

        /// <summary>
        /// Tell the LineSelection that you are about to start moving it, so that it can prepare.
        /// For example, this is needed to calculate a bounding box for the move.
        /// </summary>
        /// <param name="point">The current mouse point in the viewers GetCanvas() space </param>
        /// <returns></returns>
        internal override void PrepareForMove(UIPoint point)
        {
            mDownPoint = point;
            mEffectiveControlPoint = GetControlPoint(point);
            

            if (mEffectiveControlPoint >= E_CENTER)
            {
                mPageCropBox = mHandlingTool.BuildPageBoundBoxOnClient(mPageNumber);
                if (mEffectiveControlPoint > 0)
                {
                    mMovementBoundingBox = mPageCropBox;
                }
                else if (mEffectiveControlPoint == E_CENTER)
                {
                    mMovementBoundingBox = new PDFRect(mPageCropBox.x1 + mDownPoint.X - mBoundingBoxRect.x1,
                        mPageCropBox.y1 + mDownPoint.Y - mBoundingBoxRect.y1,
                        mPageCropBox.x2 + mDownPoint.X - mBoundingBoxRect.x2,
                        mPageCropBox.y2 + mDownPoint.Y - mBoundingBoxRect.y2);

                }
                try
                {
                    mPDFView.DocLockRead();

                    pdftron.PDF.Annot.BorderStyle bs = mLine.GetBorderStyle();
                    mMovementBoundingBox.Inflate(-bs.width * mPDFView.GetZoom() / 2);
                }
                catch (System.Exception)
                { }
                finally
                {
                    mPDFView.DocUnlockRead();
                }

            }
        }


        /// <summary>
        /// Will update the appearance of the selection tool when the control point is dragged.
        /// </summary>
        /// <param name="point">A point in Canvas Space (i.e. screen space + scroll)</param>
        internal override void Move(UIPoint point)
        {
            mIsMoved = true;
            mDragPoint = point;

            CheckBounds();

            switch (mEffectiveControlPoint)
            {
                case E_CENTER:
                    mMovingStartPoint.X = mStartPoint.X + mDragPoint.X - mDownPoint.X;
                    mMovingStartPoint.Y = mStartPoint.Y + mDragPoint.Y - mDownPoint.Y;
                    mMovingEndPoint.X = mEndPoint.X + mDragPoint.X - mDownPoint.X;
                    mMovingEndPoint.Y = mEndPoint.Y + mDragPoint.Y - mDownPoint.Y;
                    break;
                case E_START:
                    mMovingStartPoint.X = mStartPoint.X + mDragPoint.X - mDownPoint.X;
                    mMovingStartPoint.Y = mStartPoint.Y + mDragPoint.Y - mDownPoint.Y;
                    break;
                case E_END:
                    mMovingEndPoint.X = mEndPoint.X + mDragPoint.X - mDownPoint.X;
                    mMovingEndPoint.Y = mEndPoint.Y + mDragPoint.Y - mDownPoint.Y;
                    break;
            }

            mSelectionEllipseGeometries[0].Center = mMovingEndPoint;
            mSelectionEllipseGeometries[1].Center = mMovingStartPoint;
            // Here, we do not perform the offset calculations that we do in ShowAppearance. The reason is that when we move this, the line as we move it will
            // indicate exactly where the annotation will end up.
            mLineGeometry.StartPoint = mMovingStartPoint;
            mLineGeometry.EndPoint = mMovingEndPoint;

            // We should add the two endpoints here.
            List<UIPoint> targetPoints = new List<UIPoint>();
            targetPoints.Add(mMovingStartPoint);
            targetPoints.Add(mMovingEndPoint);
            mHandlingTool.ToolManager.NoteManager.AnnotationMoved(mMarkup, targetPoints);
        }

        double GetDistance(UIPoint p1, UIPoint p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }


        /// <summary>
        /// Will complete the manipulation and push back the annotation's new appearance
        /// </summary>
        internal override void Finished()
        {
            if (!mIsMoved)
            {
                return;
            }
            mIsMoved = false;
            Rect updateRect = new Rect();
            try
            {
                mPDFView.DocLock(true);

                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();

                double x1 = mMovingStartPoint.X - sx;
                double y1 = mMovingStartPoint.Y - sy;
                double x2 = mMovingEndPoint.X - sx;
                double y2 = mMovingEndPoint.Y - sy;

                mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mPageNumber);
                mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mPageNumber);

                Rect newAnnotRect = new Rect(x1, y1, x2, y2);
                newAnnotRect.Normalize();

                mAnnot.Resize(newAnnotRect);
                mLine.SetStartPoint(new PDFPoint(x1, y1));
                mLine.SetEndPoint(new PDFPoint(x2, y2));

                mAnnot.RefreshAppearance();

                UpdateDate();

                // We need to update the area that the old bounding box occupied before we update it.
                updateRect = new Rect(mStartPoint.X - sx, mStartPoint.Y - sy,
                    mEndPoint.X - sx, mEndPoint.Y - sy);
                updateRect.Inflate(20 * mPDFView.GetZoom());

                CalculateBoundingBox();
                ShowAppearance();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Couldn't update Line because: {0}", ex.Message);
            }
            finally
            {
                mPDFView.DocUnlock();
            }

            mPDFView.Update(mAnnot, mPageNumber);
            mPDFView.Update(updateRect);

            mHandlingTool.ToolManager.RaiseAnnotationEditedEvent(mAnnot);
        }

        /// <summary>
        /// Calculates the look of the cursor for various manipulation widgets.
        /// </summary>
        /// <param name="mousePoint">A point in the viewer's canvas space</param>
        internal override void SetCursor(UIPoint mousePoint)
        {
            int cp = GetControlPoint(mousePoint);
            if (cp >= 0)
            {
                mPDFView.Cursor = System.Windows.Input.Cursors.SizeAll;
            }
            else
            {
                mPDFView.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        /// <summary>
        /// Makes the Selection draw it's appearance on the tool canvas
        /// 
        /// Note that the Selection expects the tool canvas to correspond to PDFViewCtrl's canvas
        /// </summary>
        /// <param name="toolCanvas"></param>
        internal override void ShowAppearance()
        {
            if (!mHandlingTool.Children.Contains(mSelectionLine))
            {
                mHandlingTool.Children.Add(mSelectionLine);
                mHandlingTool.Children.Add(mSelectionEllipses[0]);
                mHandlingTool.Children.Add(mSelectionEllipses[1]);
            }

            mSelectionEllipseGeometries[0].Center = new UIPoint(mMovingEndPoint.X, mMovingEndPoint.Y); // This is the end point, since E_END = 2
            mSelectionEllipseGeometries[E_START].Center = new UIPoint(mMovingStartPoint.X, mMovingStartPoint.Y);

            UIPoint pt = new UIPoint(mStartPoint.X, mStartPoint.Y);
            double offx = ELLIPSE_RADIUS * (mMovingEndPoint.X - mMovingStartPoint.X) / GetDistance(mMovingStartPoint, mMovingEndPoint);
            double offy = ELLIPSE_RADIUS * (mMovingEndPoint.Y - mMovingStartPoint.Y) / GetDistance(mMovingStartPoint, mMovingEndPoint);
            pt.Offset(offx, offy);
            mLineGeometry.StartPoint = pt;

            pt = new UIPoint(mEndPoint.X, mEndPoint.Y);
            pt.Offset(-offx, -offy);
            mLineGeometry.EndPoint = pt;

        }

        /// <summary>
        ///  Will remove all UI elements from toolCanvas
        /// </summary>
        /// <param name="toolCanvas"></param>
        internal override void HideAppearance()
        {
            if (mHandlingTool.Children.Contains(mSelectionLine))
            {
                mHandlingTool.Children.Remove(mSelectionLine);
                mHandlingTool.Children.Remove(mSelectionEllipses[0]);
                mHandlingTool.Children.Remove(mSelectionEllipses[1]);
            }
        }


        /// <summary>
        /// The line edit tool will not have a simple rectangular bounding box. Therefore, we need to override the appearance.
        /// </summary>
        internal override void CreateAppearance()
        {
            mSelectionLine = new Path();
            mLineGeometry = new LineGeometry();
            mSelectionLine.Data = mLineGeometry;
            mSelectionLine.Stroke = mSelectionWidgetBrush;
            mSelectionLine.StrokeThickness = STROKE_THICKNESS;

            mSelectionEllipses = new List<Path>();
            mSelectionEllipseGeometries = new List<EllipseGeometry>();
            mSelectionEllipseCenters = new List<UIPoint>();

            for (int i = 0; i < 2; i++)
            {
                Path ellipse = new Path();
                EllipseGeometry geom = new EllipseGeometry();
                ellipse.Data = geom;
                geom.RadiusX = ELLIPSE_RADIUS;
                geom.RadiusY = ELLIPSE_RADIUS;

                ellipse.StrokeThickness = STROKE_THICKNESS;
                ellipse.Stroke = mSelectionWidgetBrush;
                // Comment out this line to remove the fill from the control point, so that the tip of the arrow can be seen.
                ellipse.Fill = mSelectionWidgetBrush;

                mSelectionEllipses.Add(ellipse);
                mSelectionEllipseGeometries.Add(geom);
                mSelectionEllipseCenters.Add(new UIPoint());
            }
        }

        /// <summary>
        /// Calculates the bounding box in Canvas space
        /// </summary>
        internal override void CalculateBoundingBox()
        {
            GetEndPoints();
            if (mLine != null)
            {
                mBoundingBoxRect = new PDFRect(mStartPoint.X, mStartPoint.Y, mEndPoint.X, mEndPoint.Y);
                mBoundingBoxRect.Normalize();
            }
        }



        /// <summary>
        /// Will move the annotation by distance. The page will not be refreshed
        /// NOTE: Must have a write lock before calling this function
        /// </summary>
        /// <param name="distance">a distance in page space</param>
        internal override void MoveAnnotation(UIPoint distance)
        {
            //Rect oldAnnotRect = mAnnot.GetRect();
            //Rect newAnnotRect = new Rect(oldAnnotRect.x1 + distance.X, oldAnnotRect.y1 + distance.Y,
            //    oldAnnotRect.x2 + distance.X, oldAnnotRect.y2 + distance.Y);
            //newAnnotRect.Normalize();
            Rect newAnnotRect = new Rect();
            PDFPoint point = mLine.GetStartPoint();
            point.x = point.x + distance.X;
            point.y = point.y + distance.Y;
            PDFPoint sp = new PDFPoint(point.x, point.y);
            newAnnotRect.x1 = sp.x;
            newAnnotRect.y1 = sp.y;


            point = mLine.GetEndPoint();
            point.x = point.x + distance.X;
            point.y = point.y + distance.Y;
            PDFPoint ep = new PDFPoint(point.x, point.y);

            newAnnotRect.x2 = ep.x;
            newAnnotRect.y2 = ep.y;

            mAnnot.Resize(newAnnotRect);
            mLine.SetStartPoint(sp);
            mLine.SetEndPoint(ep);
            mAnnot.RefreshAppearance();

            PDFRect newRect = mAnnot.GetRect();

            CalculateBoundingBox();
        }

        /// <summary>
        /// Gets a list of target points for this type of selection for placing the popup menu
        /// </summary>
        /// <returns></returns>
        internal override List<UIPoint> GetTargetPoints()
        {
            List<UIPoint> targetPoints = new List<UIPoint>();
            targetPoints.Add(new UIPoint(mMovingStartPoint.X, mMovingStartPoint.Y));
            targetPoints.Add(new UIPoint(mMovingEndPoint.X, mMovingEndPoint.Y));
            return targetPoints;
        }

        /// <summary>
        /// Will graphically highlight the annotation, to distinguish it from others.
        /// For example, this is used when the properties menu is open, in case there are many selections
        /// so that it is possible to see which one the properties menu belongs to.
        /// 
        /// Note: This funtion should always be paired with RemoveSelectionHighlight()
        /// </summary>
        internal override void HighlightSelection()
        {
            if (!mHandlingTool.Children.Contains(mSelectionLine))
            {
                base.HighlightSelection();
            }
        }

        /// <summary>
        ///  Removes the highlighting added by HighlightSelection()
        /// </summary>
        internal override void RemoveSelectionHighlight()
        {
            if (!mHandlingTool.Children.Contains(mSelectionLine))
            {
                base.RemoveSelectionHighlight();
            }
        }
        
        #endregion override functions

        #region Utility functions

        /// <summary>
        /// Computes the distance between a point (x, y) and the line annotation's center
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected double PointToLineDistance(double x, double y)
        {
            double lineXDist = mStartPoint.X - mEndPoint.X;
            double lineYDist = mStartPoint.Y - mEndPoint.Y;

            double squaredDist = (lineXDist * lineXDist) + (lineYDist * lineYDist);

            double distRatio = ((x - mEndPoint.X) * lineXDist + (y - mEndPoint.Y) * lineYDist) / squaredDist;

            if (distRatio < 0)
            {
                distRatio = 0; // This way, we will compare against mBasePoint
            }
            if (distRatio > 1)
            {
                distRatio = 1; // This way, we will compare against mTipPoint
            }

            double dx = mEndPoint.X - x + distRatio * lineXDist;
            double dy = mEndPoint.Y - y + distRatio * lineYDist;

            double dist = (dx * dx) + (dy * dy);

            return dist;
        }

        /// <summary>
        /// This function will compute the position of the two end points in the coordinates of the viewer canvas.
        /// </summary>
        protected void GetEndPoints()
        {
            try
            {
                mPDFView.DocLockRead();

                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();

                PDFPoint startPoint = mLine.GetStartPoint();
                double x = startPoint.x;
                double y = startPoint.y;
                mPDFView.ConvPagePtToScreenPt(ref x, ref y, mPageNumber);
                mStartPoint.X = x + sx;
                mStartPoint.Y = y + sy;

                PDFPoint endPoint = mLine.GetEndPoint();
                x = endPoint.x;
                y = endPoint.y;
                mPDFView.ConvPagePtToScreenPt(ref x, ref y, mPageNumber);
                mEndPoint.X = x + sx;
                mEndPoint.Y = y + sy;

                mMovingStartPoint.X = mStartPoint.X;
                mMovingStartPoint.Y = mStartPoint.Y;
                mMovingEndPoint.X = mEndPoint.X;
                mMovingEndPoint.Y = mEndPoint.Y;
            }
            catch (System.Exception)
            {
            	
            }
            finally
            {
                mPDFView.DocUnlockRead();
            }

        }

        #endregion Utility Functions

    }
}
