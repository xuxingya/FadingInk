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




namespace pdftron.PDF.Tools
{
    /// <summary>
    /// This class is the base class for wrapping annotations into selection objects.
    /// This class will use the annotations bounding box and gives the user 8 control points to move around.
    /// 
    /// Create a subclass of this to make a more specific tool for other annotations.
    /// </summary>
    internal class SelectionHelper
    {
        protected PDFViewWPF mPDFView;
        protected Tool mHandlingTool;
        protected pdftron.PDF.Annots.Markup mMarkup;
        protected pdftron.PDF.Annots.FreeText mFreeText;
        protected Annot mAnnot;
        internal Annot Annot
        {
            get { return mAnnot; }
        }
        protected int mPageNumber;
        internal int PageNumber
        {
            get { return mPageNumber; }
        }
        protected PDFRect mBoundingBoxRect;
        /// <summary>
        /// Returns a rectangle in the viewer's canvas space that contains the annotation.
        /// </summary>
        internal PDFRect BoundingBoxRect
        {
            get { return mBoundingBoxRect; }
        }

        protected bool mIsMovable = true;
        /// <summary>
        /// True if the annotation can be moved.
        /// </summary>
        internal bool IsMovable
        {
            get { return mIsMovable; }
        }

        protected bool mIsScalable = true;
        /// <summary>
        /// True if the annotation can be scaled
        /// </summary>
        internal bool IsScalable
        {
            get { return mIsScalable; }
        }

        protected bool mCanBeCopied = false;
        /// <summary>
        /// True if the annotation can be copied
        /// </summary>
        internal bool CanBeCopied
        {
            get { return mCanBeCopied; }
        }

        internal PDFRect mPageCropBox;
        internal UIPoint mDownPoint;
        internal UIPoint mDragPoint;
        internal double mLeft, mTop, mBottom, mRight;
        internal bool mIsMoved = false;
        internal PDFRect mMovementBoundingBox; // When we click on a control point, we create this box to bound where we can drag the mouse.

        internal List<Control> mMenuItems;

        //////////////////////////////////////////////////////////////////////////
        // Editing Free Text Annotations
        protected Border mTextPopupBorder;
        protected TextBox mTextBox;
        protected bool mIsTextPopupOpen = false;
        protected bool mSaveTextWhenExiting = true;
        protected string mOldText;
        protected bool mIsContentUpToDate = true;
        protected bool mIsFreeTextEdited = false;

        
        //////////////////////////////////////////////////////////////////////////
        // Control points and manipulations
        protected const int E_CENTER = 0; // The main part of the annotation, moves the whole thing. (Should be 0 for all derived classes).
        protected const int E_LM = 1;	//lower middle control point
        protected const int E_LR = 2;	//lower right
        protected const int E_MR = 3;	//middle right
        protected const int E_UR = 4;	//upper right
        protected const int E_UM = 5;	//upper middle
        protected const int E_UL = 6;	//upper left
        protected const int E_ML = 7;	//middle left
        protected const int E_LL = 8;	//lower left - will have index 0 in our lists
        protected int mEffectiveControlPoint;



        protected Path mSelectionRectangle;
        protected RectangleGeometry mRectangleGeometry;
        protected List<Path> mSelectionEllipses;
        protected List<EllipseGeometry> mSelectionEllipseGeometries;
        protected List<UIPoint> mSelectionEllipseCenters;

        protected SolidColorBrush mSelectionWidgetBrush = Brushes.Blue;
        protected double ELLIPSE_RADIUS = 5;
        protected double STROKE_THICKNESS = 1;

        /// <summary>
        /// Returns true if touch is used.
        /// </summary>
        public bool IsUsingTouch { get; set; }

        //////////////////////////////////////////////////////////////////////////
        // Options for the properties menu
        protected bool mHasLineColor = false;
        internal bool HasLineColor
        {
            get { return mHasLineColor; }
        }
        protected bool mHasTextColor = false;
        internal bool HasTextColor
        {
            get { return mHasTextColor; }
        }
        protected bool mHasFillColor = false;
        internal bool HasFillColor
        {
            get { return mHasFillColor; }
        }
        protected bool mHasLineThickness = false;
        internal bool HasLineThickness
        {
            get { return mHasLineThickness; }
        }
        protected bool mHasLineStyle = false;
        internal bool HasLineStyle
        {
            get { return mHasLineStyle; }
        }
        protected bool mHasLineStartStyle = false;
        internal bool HasLineStartStyle
        {
            get { return mHasLineStartStyle; }
        }
        protected bool mHasLineEndStyle = false;
        internal bool HasLineEndStyle
        {
            get { return mHasLineEndStyle; }
        }
        protected bool mHasOpacity = false;
        internal bool HasOpacity
        {
            get { return mHasOpacity; }
        }
        protected bool mHasFontSize = false;
        internal bool HasFontSize
        {
            get { return mHasFontSize; }
        }

        protected bool mCanLineThicknessBeZero = false;
        internal bool CanLineThicknessBeZero
        {
            get { return mCanLineThicknessBeZero; }
        }

        internal SolidColorBrush LineColor
        {
            get
            {
                return GetColorFromAnnot(e_stroke);
            }
            set
            {
                SetColorOfAnnot(value, e_stroke);
            }
        }
        internal bool CanLineColorBeEmpty
        {
            get { return CanColorBeEmpty(e_stroke); }
        }

        internal SolidColorBrush FillColor
        {
            get
            {
                return GetColorFromAnnot(e_fill);
            }
            set
            {
                SetColorOfAnnot(value, e_fill);
            }
        }
        internal bool CanFillColorBeEmpty
        {
            get { return CanColorBeEmpty(e_fill); }
        }

        internal SolidColorBrush TextColor
        {
            get
            {
                return GetColorFromAnnot(e_text_stroke);
            }
            set
            {
                SetColorOfAnnot(value, e_text_stroke);
            }
        }
        internal bool CanTextColorBeEmpty
        {
            get { return CanColorBeEmpty(e_text_stroke); }
        }

        internal double LineThickness
        {
            get
            {
                pdftron.PDF.Annot.BorderStyle bStyle = mAnnot.GetBorderStyle();
                return bStyle.width;
            }
            set
            {
                pdftron.PDF.Annot.BorderStyle bStyle = mAnnot.GetBorderStyle();
                bStyle.width = value;
                mAnnot.SetBorderStyle(bStyle);

                switch (mAnnot.GetType())
                {
                    case Annot.Type.e_Line:
                    case Annot.Type.e_Square:
                    case Annot.Type.e_Circle:
                    case Annot.Type.e_Polyline:
                    case Annot.Type.e_Polygon:
                    case Annot.Type.e_Ink:
                        pdftron.PDF.Tools.Properties.Settings.Default.StrokeThickness = value;
                        break;
                    case Annot.Type.e_Underline:
                    case Annot.Type.e_StrikeOut:
                    case Annot.Type.e_Squiggly:
                        pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupThickness = value;
                        break;
                    case Annot.Type.e_FreeText:
                        pdftron.PDF.Tools.Properties.Settings.Default.TextLineThickness = value;
                        break;
                    case Annot.Type.e_Link:
                        // In case of links, we need to actually remove the appearance stream.
                        if (value == 0)
                        {
                            pdftron.SDF.Obj obj = mAnnot.GetSDFObj();
                            pdftron.SDF.Obj ap = obj.FindObj("AP");
                            if (ap != null)
                            {
                                obj.Erase("AP");
                            }
                        }
                        break;
                }
            }
        }

        internal pdftron.PDF.Annot.BorderStyle LineStyle
        {
            get
            {
                return mAnnot.GetBorderStyle();
            }
            set
            {
                pdftron.PDF.Annot.BorderStyle bStyle = mAnnot.GetBorderStyle();
                bStyle.border_style = value.border_style;
                bStyle.dash = value.dash;             
                mAnnot.SetBorderStyle(bStyle);
            }
        }

        internal pdftron.PDF.Annots.Line.EndingStyle LineStartStyle
        {
            get
            {
                pdftron.PDF.Annots.Line line = new pdftron.PDF.Annots.Line(mAnnot);
                return line.GetStartStyle();
            }
            set 
            {
                pdftron.PDF.Annots.Line line = new pdftron.PDF.Annots.Line(mAnnot);
                line.SetStartStyle(value);
            }
        }

        internal pdftron.PDF.Annots.Line.EndingStyle LineEndStyle
        {
            get
            {
                pdftron.PDF.Annots.Line line = new pdftron.PDF.Annots.Line(mAnnot);
                return line.GetEndStyle();
            }
            set
            {
                pdftron.PDF.Annots.Line line = new pdftron.PDF.Annots.Line(mAnnot);
                line.SetEndStyle(value);
            }
        }

        internal double Opacity
        {
            get
            {
                return mMarkup.GetOpacity();
            }
            set
            {
                mMarkup.SetOpacity(value);
                switch (mMarkup.GetType())
                {
                    case Annot.Type.e_Line:
                    case Annot.Type.e_Square:
                    case Annot.Type.e_Circle:
                    case Annot.Type.e_Polyline:
                    case Annot.Type.e_Polygon:
                    case Annot.Type.e_Ink:
                        pdftron.PDF.Tools.Properties.Settings.Default.MarkupOpacity = value;
                        break;
                    case Annot.Type.e_Highlight:
                        pdftron.PDF.Tools.Properties.Settings.Default.HighlightOpacity = value;
                        break;
                    case Annot.Type.e_Underline:
                    case Annot.Type.e_StrikeOut:
                    case Annot.Type.e_Squiggly:
                        pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupOpacity = value;
                        break;
                    case Annot.Type.e_FreeText:
                        pdftron.PDF.Tools.Properties.Settings.Default.TextOpacity = value;
                        break;
                }
            }
        }

        internal double FontSize
        {
            get
            {
                return mFreeText.GetFontSize();
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.FontSize = value;
                mFreeText.SetFontSize(value);
            }
        }

        // not part of properties menu, but part of the context menu
        protected bool mNoteOption = false; 
        protected bool mCopyOption = false;
        protected bool mEditTextOption = false;
        protected Rectangle mHighlightRect;
       
        //////////////////////////////////////////////////////////////////////////
        // Colors
        protected const int e_stroke = 0;
        protected const int e_fill = 1;
        protected const int e_text_stroke = 2;
        protected const int e_text_fill = 3;
        protected const int e_highlight = 4;
        protected const int e_text_markup = 5;
        protected int mEffectiveBrush = -1;

        /// <summary>
        /// Minimal constructor, which creates a minimal object so that it can be compared against selectionHelpers in Containers.
        /// </summary>
        /// <param name="annot"></param>
        internal SelectionHelper(Annot annot)
        {
            mAnnot = annot;
        }

        /// <summary>
        /// Main Constructor, will create a full fledged object.
        /// </summary>
        /// <param name="view">the PDGFViewCtrl</param>
        /// <param name="annot">The Annotation</param>
        /// <param name="pageNumber">The Annotation's page number</param>
        internal SelectionHelper(PDFViewWPF view, Tool tool, Annot annot, int pageNumber)
        {
            mPDFView = view;
            mHandlingTool = tool;
            mAnnot = annot;
            mPageNumber = pageNumber;
            CalculateBoundingBox();
            PopulateMenuAndCalculateMovability();
            CreateAppearance();
            mPageCropBox = new PDFRect();
        }

        /// <summary>
        /// Returns true if the point (x, y) is inside the rectangles bounding box.
        /// </summary>
        /// <param name="x">the x coordinate (from canvas)</param>
        /// <param name="y">they y coordinate (from canvas)</param>
        /// <returns></returns>
        internal bool IsPointInRect(double x, double y)
        {
            return (x >= mBoundingBoxRect.x1 && x <= mBoundingBoxRect.x2
                && y >= mBoundingBoxRect.y1 && y <= mBoundingBoxRect.y2);
        }

        internal void ZoomChanged()
        {
            CalculateBoundingBox();

        }

        public override bool Equals(object obj)
        {
            if (obj is SelectionHelper)
            {
                SelectionHelper otherObj = obj as SelectionHelper;
                return (mAnnot == otherObj.mAnnot);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0x50;
            hash = 31 * hash + mAnnot.GetHashCode();
            return hash;
        }

        /// <summary>
        /// This function figures out what options should go into the context menu, and whether or not the annotation is movable.
        /// </summary>
        internal virtual void PopulateMenuAndCalculateMovability()
        {
            mMenuItems = new List<Control>();

            bool annotIsSticky = false;
            bool annotIsTextMarkup = false;
            try
            {
                //locks the document first as accessing annotation/doc information isn't thread safe.
                mPDFView.DocLockRead();
                Annot.Type aType = mAnnot.GetType();

                mCanBeCopied = true; // default

                if (aType == Annot.Type.e_Link)
                {
                    mHasLineColor = true;
                    mHasLineStyle = true;
                    mHasLineThickness = true;
                    mCanLineThicknessBeZero = true;
                    mIsMovable = false;
                    mIsScalable = false;
                    mCanBeCopied = false;
                }


                annotIsSticky = (aType == Annot.Type.e_Text);
                annotIsTextMarkup = (aType == Annot.Type.e_Highlight ||
                                        aType == Annot.Type.e_Underline ||
                                        aType == Annot.Type.e_StrikeOut ||
                                        aType == Annot.Type.e_Squiggly);

                if ((mAnnot.IsMarkup() && aType != Annot.Type.e_FreeText)
                    || aType == Annot.Type.e_Text)
                {
                    mNoteOption = true;
                }

                if (aType == Annot.Type.e_Line || aType == Annot.Type.e_Ink || aType == Annot.Type.e_Square
                    || aType == Annot.Type.e_Circle || aType == Annot.Type.e_Polygon
                    || aType == Annot.Type.e_Polyline)
                {
                    mHasLineColor = true;

                    if (aType != Annot.Type.e_Ink)
                    {
                        mHasFillColor = true;
                    }
                    mHasLineThickness = true;

                    mHasLineStyle = true;
                    if (aType == Annot.Type.e_Line || aType == Annot.Type.e_Polyline)
                    {
                        mHasLineStartStyle = true;
                        mHasLineEndStyle = true;
                    }
                    
                }

                if (aType == Annot.Type.e_Underline || aType == Annot.Type.e_Squiggly || aType == Annot.Type.e_StrikeOut
                    || aType == Annot.Type.e_Highlight)
                {
                    mHasLineColor = true;

                    if (aType != Annot.Type.e_Highlight && aType != Annot.Type.e_Text)
                    {
                        mHasLineThickness = true;
                    }
                }

                if (annotIsTextMarkup)
                {
                    mCopyOption = true;
                    mCanBeCopied = false;
                }

                if (aType == Annot.Type.e_FreeText)
                {
                    mEditTextOption = true;
                    mHasTextColor = true;
                    mHasFillColor = true;
                    mHasLineColor = true;
                    mHasFontSize = true;
                    mHasLineThickness = true;
                    mHasLineStyle = true;
                    mCanLineThicknessBeZero = true;
                    mCanBeCopied = true;
                    mFreeText = new pdftron.PDF.Annots.FreeText(mAnnot);
                }
                if (annotIsTextMarkup || aType == Annot.Type.e_Line || aType == Annot.Type.e_Ink
                    || aType == Annot.Type.e_Square || aType == Annot.Type.e_Circle || aType == Annot.Type.e_Polygon
                    || aType == Annot.Type.e_Polyline || aType == Annot.Type.e_FreeText)
                {
                    mHasOpacity = true;
                }

                if (annotIsSticky)
                {
                    mIsScalable = false;
                }
                if (annotIsTextMarkup)
                {
                    mIsScalable = false;
                    mIsMovable = false;
                }

                if (mAnnot.IsMarkup())
                {
                    mMarkup = new pdftron.PDF.Annots.Markup(mAnnot);
                }


            }
            catch (Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlockRead();
            }
        }



        #region Virtual Functions

        /// <summary>
        /// Gets the current control point from the annotation.
        /// </summary>
        /// <param name="point">A point in Canvas Space (i.e. screen space + scroll)</param>
        /// <returns></returns>
        internal virtual int GetControlPoint(UIPoint point)
        {
            mDownPoint = point;


            mEffectiveControlPoint = -1;

            if (mIsScalable)
            {
                double shortestDistance = GetControlPointDistanceThreshold();
                double dist;

                // Find the closest control point
                for (int i = 0; i < 8; i++)
                {
                    dist = ((mSelectionEllipseGeometries[i].Center.X - point.X) * (mSelectionEllipseGeometries[i].Center.X - point.X))
                        + ((mSelectionEllipseGeometries[i].Center.Y - point.Y) * (mSelectionEllipseGeometries[i].Center.Y - point.Y));
                    if (dist <= shortestDistance)
                    {
                        shortestDistance = dist;
                        mEffectiveControlPoint = i;
                    }
                }

                // Since E_LL = 8, and E_CENTER = 0, we want to change this
                if (mEffectiveControlPoint == E_CENTER)
                {
                    mEffectiveControlPoint = E_LL;
                }
            }


            if (mEffectiveControlPoint == -1 && mBoundingBoxRect.Contains(point.X, point.Y))
            {
                mEffectiveControlPoint = E_CENTER;
            }

            return mEffectiveControlPoint;
        }


        /// <summary>
        /// Tell the SelectionHelper that you are about to start moving it, so that it can prepare.
        /// For example, this is needed to calculate a bounding box for the move.
        /// </summary>
        /// <param name="point">The current mouse point in the viewers GetCanvas() space </param>
        /// <returns></returns>
        internal virtual void PrepareForMove(UIPoint point)
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
            }

            // Set the bounds for our movement, in case the control point doesn't affect it.
            mLeft = mBoundingBoxRect.x1;
            mTop = mBoundingBoxRect.y1;
            mRight = mBoundingBoxRect.x2;
            mBottom = mBoundingBoxRect.y2;
        }


        /// <summary>
        /// Will update the appearance of the selection tool when the control point is dragged.
        /// </summary>
        /// <param name="point">A point in Canvas Space (i.e. screen space + scroll)</param>
        internal virtual void Move(UIPoint point)
        {
            mIsMoved = true;
            mDragPoint = point;

            if (!mIsMovable)
            {
                return;
            }

            CheckBounds();

            switch (mEffectiveControlPoint)
            {
                case E_CENTER:
                    mLeft = mBoundingBoxRect.x1 + mDragPoint.X - mDownPoint.X;
                    mTop = mBoundingBoxRect.y1 + mDragPoint.Y - mDownPoint.Y;
                    mRight = mBoundingBoxRect.x2 + mDragPoint.X - mDownPoint.X;
                    mBottom = mBoundingBoxRect.y2 + mDragPoint.Y - mDownPoint.Y;
                    break;
                case E_LL:
                    mLeft = mDragPoint.X;
                    mBottom = mDragPoint.Y;
                    break;
                case E_LM:
                    mBottom = mDragPoint.Y;
                    break;
                case E_LR:
                    mRight = mDragPoint.X;
                    mBottom = mDragPoint.Y;
                    break;
                case E_MR:
                    mRight = mDragPoint.X;
                    break;
                case E_UR:
                    mRight = mDragPoint.X;
                    mTop = mDragPoint.Y;
                    break;
                case E_UM:
                    mTop = mDragPoint.Y;
                    break;
                case E_UL:
                    mLeft = mDragPoint.X;
                    mTop = mDragPoint.Y;
                    break;
                case E_ML:
                    mLeft = mDragPoint.X;
                    break;
            }

            
            mRectangleGeometry.Rect = new UIRect(Math.Min(mLeft, mRight), Math.Min(mTop, mBottom), Math.Abs(mRight - mLeft), Math.Abs(mBottom - mTop));

            if (mIsScalable)
            {
                mSelectionEllipseGeometries[0].Center = new UIPoint(mLeft, mBottom); // lower left (since LL is 8)
                mSelectionEllipseGeometries[E_LM].Center = new UIPoint((mLeft + mRight) / 2, mBottom);
                mSelectionEllipseGeometries[E_LR].Center = new UIPoint(mRight, mBottom);
                mSelectionEllipseGeometries[E_MR].Center = new UIPoint(mRight, (mTop + mBottom) / 2);
                mSelectionEllipseGeometries[E_UR].Center = new UIPoint(mRight, mTop);
                mSelectionEllipseGeometries[E_UM].Center = new UIPoint((mLeft + mRight) / 2, mTop);
                mSelectionEllipseGeometries[E_UL].Center = new UIPoint(mLeft, mTop);
                mSelectionEllipseGeometries[E_ML].Center = new UIPoint(mLeft, (mTop + mBottom) / 2);
            }

            List<UIPoint> targetPoints = new List<UIPoint>();
            targetPoints.Add(new UIPoint(mLeft, mTop));
            targetPoints.Add(new UIPoint(mLeft, mBottom));
            targetPoints.Add(new UIPoint(mRight, mTop));
            targetPoints.Add(new UIPoint(mRight, mBottom));
            mHandlingTool.ToolManager.NoteManager.AnnotationMoved(mMarkup, targetPoints);
        }

        /// <summary>
        /// Keeps the shape within the page
        /// </summary>
        internal virtual void CheckBounds()
        {
            if (mDragPoint.X < mMovementBoundingBox.x1)
            {
                mDragPoint.X = mMovementBoundingBox.x1;
            }
            if (mDragPoint.Y < mMovementBoundingBox.y1)
            {
                mDragPoint.Y = mMovementBoundingBox.y1;
            }
            if (mDragPoint.X > mMovementBoundingBox.x2)
            {
                mDragPoint.X = mMovementBoundingBox.x2;
            }
            if (mDragPoint.Y > mMovementBoundingBox.y2)
            {
                mDragPoint.Y = mMovementBoundingBox.y2;
            }

        }

        /// <summary>
        /// Will complete the manipulation and push back the annotation's new appearance
        /// </summary>
        internal virtual void Finished()
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

                double sx =  mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();

                double x1 = mLeft - sx;
                double y1 = mTop - sy;
                double x2 = mRight - sx;
                double y2 = mBottom - sy;

                mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mPageNumber);
                mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mPageNumber);

                Rect newAnnotRect = new Rect(x1, y1, x2, y2);
                newAnnotRect.Normalize();

                mAnnot.Resize(newAnnotRect);

                Annot.Type aType = mAnnot.GetType();

                // We only want to refresh appearance for these types because [TODO]
                if (aType == Annot.Type.e_Line || aType == Annot.Type.e_Circle || aType == Annot.Type.e_Square || aType == Annot.Type.e_Polyline
                    || aType == Annot.Type.e_Polygon || aType == Annot.Type.e_Ink || aType == Annot.Type.e_FreeText)
                {
                    mAnnot.RefreshAppearance();
                }

                UpdateDate();

                // We need to update the area that the old bounding box occupied before we update it.
                updateRect = new Rect(mBoundingBoxRect.x1 - sx, mBoundingBoxRect.y1 - sy,
                    mBoundingBoxRect.x2 - sx, mBoundingBoxRect.y2 - sy);
                updateRect.Inflate(20 * mPDFView.GetZoom());
                //updateRect.Inflate(100 * mPDFView.GetZoom());

                CalculateBoundingBox();
                ShowAppearance();
            }
            catch (System.Exception)
            {
            	
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
        internal virtual void SetCursor(UIPoint mousePoint)
        {
            int cp = GetControlPoint(mousePoint);
            switch (cp)
            {
                case E_LL:
                case E_UR:
                    mPDFView.Cursor = Cursors.SizeNESW;
                    break;
                case E_LR:
                case E_UL:
                    mPDFView.Cursor = Cursors.SizeNWSE;
                    break;
                case E_LM:
                case E_UM:
                    mPDFView.Cursor = Cursors.SizeNS;
                    break;
                case E_ML:
                case E_MR:
                    mPDFView.Cursor = Cursors.SizeWE;
                    break;
                default:
                    mPDFView.Cursor = Cursors.Arrow;
                    break;

            }
        }

        /// <summary>
        /// Makes the Selection draw it's appearance on the tool canvas
        /// 
        /// Note that the Selection expects the tool canvas to correspond to PDFViewCtrl's canvas
        /// </summary>
        /// <param name="toolCanvas"></param>
        internal virtual void ShowAppearance()
        {
            if (!mHandlingTool.Children.Contains(mSelectionRectangle))
            {
                mHandlingTool.Children.Add(mSelectionRectangle);
            }
            mRectangleGeometry.Rect = new UIRect(mBoundingBoxRect.x1, mBoundingBoxRect.y1,
                mBoundingBoxRect.x2 - mBoundingBoxRect.x1, mBoundingBoxRect.y2 - mBoundingBoxRect.y1);
            if (mIsScalable)
            {
                if (!mHandlingTool.Children.Contains(mSelectionEllipses[0]))
                {
                    foreach (Path p in mSelectionEllipses)
                    {
                        mHandlingTool.Children.Add(p);
                    }
                }

                mSelectionEllipseGeometries[0].Center = new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y2); // lower left (since LL is 8)
                mSelectionEllipseGeometries[E_LM].Center = new UIPoint((mBoundingBoxRect.x1 + mBoundingBoxRect.x2) / 2, mBoundingBoxRect.y2);
                mSelectionEllipseGeometries[E_LR].Center = new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y2);
                mSelectionEllipseGeometries[E_MR].Center = new UIPoint(mBoundingBoxRect.x2, (mBoundingBoxRect.y1 + mBoundingBoxRect.y2) / 2);
                mSelectionEllipseGeometries[E_UR].Center = new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y1);
                mSelectionEllipseGeometries[E_UM].Center = new UIPoint((mBoundingBoxRect.x1 + mBoundingBoxRect.x2) / 2, mBoundingBoxRect.y1);
                mSelectionEllipseGeometries[E_UL].Center = new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y1);
                mSelectionEllipseGeometries[E_ML].Center = new UIPoint(mBoundingBoxRect.x1, (mBoundingBoxRect.y1 + mBoundingBoxRect.y2) / 2);
            }
        }

        /// <summary>
        ///  Will remove all UI elements from toolCanvas
        /// </summary>
        /// <param name="toolCanvas"></param>
        internal virtual void HideAppearance()
        {
            if (mIsMovable && mHandlingTool.Children.Contains(mSelectionRectangle))
            {
                mHandlingTool.Children.Remove(mSelectionRectangle);
            }
            
            if (mIsScalable && mHandlingTool.Children.Contains(mSelectionEllipses[0]))
            {
                foreach (Path p in mSelectionEllipses)
                {
                    mHandlingTool.Children.Remove(p);
                }
            }
           
        }

        /// <summary>
        /// Instantiates the various shapes and lists needed to draw the appearance.
        /// </summary>
        internal virtual void CreateAppearance()
        {
            mSelectionRectangle = new Path();
            mRectangleGeometry = new RectangleGeometry();
            mSelectionRectangle.Data = mRectangleGeometry;
            mSelectionRectangle.Stroke = mSelectionWidgetBrush;
            mSelectionRectangle.StrokeThickness = STROKE_THICKNESS;
            DoubleCollection d = new DoubleCollection();
            d.Add(1);
            d.Add(1);
            mSelectionRectangle.StrokeDashArray = d;
            //}

            if (mIsScalable)
            {
                mSelectionEllipses = new List<Path>();
                mSelectionEllipseGeometries = new List<EllipseGeometry>();
                mSelectionEllipseCenters = new List<UIPoint>();

                for (int i = 0; i < 8; i++)
                {
                    Path ellipse = new Path();
                    EllipseGeometry geom = new EllipseGeometry();
                    ellipse.Data = geom;
                    geom.RadiusX = ELLIPSE_RADIUS;
                    geom.RadiusY = ELLIPSE_RADIUS;
                    ellipse.Fill = mSelectionWidgetBrush;

                    // This would add the same stroke dash array to the resize widgets.
                    //ellipse.Stroke = mSelectionWidgetBrush;
                    //ellipse.StrokeThickness = STROKE_THICKNESS;
                    //DoubleCollection dc = new DoubleCollection();
                    //dc.Add(1);
                    //dc.Add(1);
                    //ellipse.StrokeDashArray = dc;


                    mSelectionEllipses.Add(ellipse);
                    mSelectionEllipseGeometries.Add(geom);
                    mSelectionEllipseCenters.Add(new UIPoint());
                }
            }
        }


        public virtual void AddContextMenuItems(ContextMenu menu)
        {
            MenuItem m;
            if (mNoteOption)
            {
                m = new MenuItem();
                m.Header = "Note";
                m.Click += ContextMenu_Note;
                menu.Items.Add(m);
            }

            if (mEditTextOption)
            {
                m = new MenuItem();
                m.Header = "Edit Text";
                m.Click += ContextMenu_EditText;
                menu.Items.Add(m);
            }

            if (mCopyOption)
            {
                m = new MenuItem();
                m.Header = "Copy Text to Clipboard";
                m.Click += ContextMenu_CopyText;
                menu.Items.Add(m);
            }

            if (mHasLineColor || mHasTextColor || mHasFillColor || mHasLineThickness || mHasLineStyle
                || mHasLineStartStyle || mHasLineEndStyle || mHasOpacity || mHasFontSize)
            {
                m = new MenuItem();
                m.Header = "Properties";
                m.Click += ContextMenu_Properties;
                menu.Items.Add(m);
            }
        }


        internal virtual void HandleDoubleClick()
        {
            if (mNoteOption)
            {
                OpenPopupNote();
            }
            if (mEditTextOption)
            {
                CreateTextPopup();
            }
        }


        /// <summary>
        /// Calculates the bounding box in Canvas space
        /// </summary>
        internal virtual void CalculateBoundingBox()
        {
            try
            {
                mPDFView.DocLockRead();
                mBoundingBoxRect = mAnnot.GetRect();
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlockRead();
            }
            
            mHandlingTool.ConvertPageRectToCanvasRect(mBoundingBoxRect, mPageNumber);
        }





        /// <summary>
        /// Will move the annotation by distance. The page will not be refreshed
        /// NOTE: Must have a write lock before calling this function
        /// </summary>
        /// <param name="distance">a distance in page space</param>
        internal virtual void MoveAnnotation(UIPoint distance)
        {
            Rect oldAnnotRect = mAnnot.GetRect();

            Rect newAnnotRect = new Rect(oldAnnotRect.x1 + distance.X, oldAnnotRect.y1 + distance.Y,
                oldAnnotRect.x2 + distance.X, oldAnnotRect.y2 + distance.Y);
            newAnnotRect.Normalize();

            mAnnot.Resize(newAnnotRect);

            Annot.Type aType = mAnnot.GetType();

            // We only want to refresh appearance for these types because for others they may start looking different
            if (aType == Annot.Type.e_Line || aType == Annot.Type.e_Circle || aType == Annot.Type.e_Square || aType == Annot.Type.e_Polyline
                || aType == Annot.Type.e_Polygon || aType == Annot.Type.e_Ink || aType == Annot.Type.e_FreeText)
            {
                mAnnot.RefreshAppearance();
            }

            UpdateDate();

            CalculateBoundingBox();
        }


        internal virtual List<UIPoint> GetTargetPoints()
        {
            List<UIPoint> targetPoints = new List<UIPoint>();
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y1));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y2));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y1));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y2));
            return targetPoints;
        }

        #endregion Virtual Functions

        /// <summary>
        /// Call this when you are moving the selected annotation
        /// This will update any required state that is not directly part of the selection widget drawing.
        /// </summary>
        /// <param name="translation"></param>
        internal virtual void Translated(double translationX, double translationY)
        {
            List<UIPoint> targetPoints = GetTargetPoints();
            List<UIPoint> translatedTargetPoints = new List<UIPoint>();
            foreach (UIPoint point in targetPoints)
            {
                point.Offset(translationX, translationY);
                translatedTargetPoints.Add(point);
            }
            //List<UIPoint> targetPoints = new List<UIPoint>();

            //targetPoints.Add(new UIPoint(BoundingBoxRect.x1 + translationX, BoundingBoxRect.y1 + translationY));
            //targetPoints.Add(new UIPoint(BoundingBoxRect.x1 + translationX, BoundingBoxRect.y2 + translationY));
            //targetPoints.Add(new UIPoint(BoundingBoxRect.x2 + translationX, BoundingBoxRect.y2 + translationY));
            //targetPoints.Add(new UIPoint(BoundingBoxRect.x2 + translationX, BoundingBoxRect.y1 + translationY));
            mHandlingTool.ToolManager.NoteManager.AnnotationMoved(mMarkup, translatedTargetPoints);
        }

        /// <summary>
        /// Will graphically highlight the annotation, to distinguish it from others.
        /// For example, this is used when the properties menu is open, in case there are many selections
        /// so that it is possible to see which one the properties menu belongs to.
        /// 
        /// Note: This funtion should always be paired with RemoveSelectionHighlight()
        /// </summary>
        internal virtual void HighlightSelection()
        {
            // Add a rectangle to mark the active annotation
            PDFRect bBoxRect = new PDFRect();
            bBoxRect.Set(mBoundingBoxRect);
            bBoxRect.Inflate(5);
            mHighlightRect = new Rectangle();
            mHighlightRect.SetValue(Canvas.LeftProperty, bBoxRect.x1);
            mHighlightRect.SetValue(Canvas.TopProperty, bBoxRect.y1);
            mHighlightRect.Width = bBoxRect.Width();
            mHighlightRect.Height = bBoxRect.Height();
            mHighlightRect.Stroke = new SolidColorBrush(Color.FromArgb(100, 50, 50, 150));
            mHighlightRect.StrokeThickness = 3;
            mHighlightRect.Fill = new SolidColorBrush(Color.FromArgb(50, 100, 100, 255));
            mHandlingTool.Children.Add(mHighlightRect);
        }

        /// <summary>
        ///  Removes the highlighting added by HighlightSelection()
        /// </summary>
        internal virtual void RemoveSelectionHighlight()
        {
            mHandlingTool.Children.Remove(mHighlightRect);
        }

        #region Context Menu

        protected void ContextMenu_Note(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenPopupNote();
        }

        protected virtual void OpenPopupNote()
        {
            List<UIPoint> targetPoints = new List<UIPoint>();
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y1));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x1, mBoundingBoxRect.y2));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y1));
            targetPoints.Add(new UIPoint(mBoundingBoxRect.x2, mBoundingBoxRect.y2));
            mHandlingTool.ToolManager.NoteManager.OpenNote(mMarkup, mPageNumber, GetTargetPoints());
        }

        protected void ContextMenu_CopyText(object sender, System.Windows.RoutedEventArgs e)
        {
            pdftron.PDF.TextExtractor te = new pdftron.PDF.TextExtractor();
            try
            {
                mPDFView.DocLockRead();
                PDFPage page = mPDFView.GetDoc().GetPage(mPageNumber);
                if (page != null)
                {
                    te.Begin(page);
                    String text = te.GetTextUnderAnnot(mAnnot);

                    System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, text);
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

        protected void ContextMenu_EditText(object sender, System.Windows.RoutedEventArgs e)
        {
            CreateTextPopup();
        }

        protected void ContextMenu_Properties(object sender, System.Windows.RoutedEventArgs e)
        {
            AnnotationPropertiesPopup popup = null;
            try
            {
                mPDFView.DocLockRead();
                popup = new AnnotationPropertiesPopup(this);
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlockRead();
            }

            if (popup == null)
            {
                return;
            }

            popup.Owner = System.Windows.Window.GetWindow(mHandlingTool);

            // Add a rectangle to mark the active annotation
            //PDFRect bBoxRect = new PDFRect();
            //bBoxRect.Set(mBoundingBoxRect);
            //bBoxRect.Inflate(5);
            //Rectangle rect = new Rectangle();
            //rect.SetValue(Canvas.LeftProperty, bBoxRect.x1);
            //rect.SetValue(Canvas.TopProperty, bBoxRect.y1);
            //rect.Width = bBoxRect.Width();
            //rect.Height = bBoxRect.Height();
            //rect.Stroke = new SolidColorBrush(Color.FromArgb(100, 50, 50, 150));
            //rect.StrokeThickness = 3;
            //rect.Fill = new SolidColorBrush(Color.FromArgb(50, 100, 100, 255));
            //mHandlingTool.Children.Add(rect);
            HighlightSelection();

            Nullable<bool> result = null;

            try
            {
                mPDFView.DocLock(true);
                result = popup.ShowDialog();

                

                if (result != null && result == true)
                {
                    mAnnot.RefreshAppearance();
                    UpdateDate();
                }
            }
            catch (System.Exception)
            { }
            finally
            {
                mPDFView.DocUnlock();
                RemoveSelectionHighlight();
            }

            if (result != null && result == true)
            {
                mPDFView.Update(mAnnot, mPageNumber);
                mHandlingTool.ToolManager.RaiseAnnotationEditedEvent(mAnnot);
                pdftron.PDF.Tools.Properties.Settings.Default.Save();
            }
        }

        #endregion Context Menu


        #region Utility Functions


        protected double GetControlPointDistanceThreshold()
        {
            double distance = ELLIPSE_RADIUS * ELLIPSE_RADIUS;
            if (IsUsingTouch)
            {
                distance *= 4;
            }
            return distance;
        }


        protected void UpdateDate()
        {
            DateTime now = DateTime.Now;
            Date date = new Date((short)now.Year, (byte)now.Month, (byte)now.Day, (byte)now.Hour, (byte)now.Minute, (byte)now.Second);
            mAnnot.SetDate(date);
        }
        
        protected SolidColorBrush GetColorFromAnnot(int effectiveBrush)
        {
            ColorPt color;
            switch (mAnnot.GetType())
            {
                case Annot.Type.e_Circle:
                case Annot.Type.e_Square:
                case Annot.Type.e_Line:
                case Annot.Type.e_Ink:
                case Annot.Type.e_Polyline:
                case Annot.Type.e_Polygon:
                case Annot.Type.e_Highlight:
                case Annot.Type.e_Underline:
                case Annot.Type.e_Squiggly:
                case Annot.Type.e_StrikeOut:
                    if (effectiveBrush == e_stroke)
                    {
                        if (mAnnot.GetColorCompNum() == 0)
                        {
                            return new SolidColorBrush(Colors.Transparent);
                        }
                        color = mAnnot.GetColorAsRGB();
                        return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    }
                    else if (effectiveBrush == e_fill)
                    {
                        if (mMarkup.GetInteriorColorCompNum() == 0)
                        {
                            return new SolidColorBrush(Colors.Transparent);
                        }
                        color = mMarkup.GetInteriorColor();
                        return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    }
                    break;
                case Annot.Type.e_FreeText:
                    if (effectiveBrush == e_text_stroke)
                    {
                        color = mFreeText.GetTextColor();
                        return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    }
                    if (effectiveBrush == e_fill)
                    {
                        if (mAnnot.GetColorCompNum() == 0)
                        {
                            return new SolidColorBrush(Colors.Transparent);
                        }
                        color = mAnnot.GetColorAsRGB();
                        return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    }
                    if (effectiveBrush == e_stroke)
                    {
                        if (mFreeText.GetLineColorCompNum() == 0)
                        {
                            return new SolidColorBrush(Colors.Transparent);
                        }
                        color = mFreeText.GetLineColor();
                        return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
                    }
                    break;
                case Annot.Type.e_Link:
                    if (mAnnot.GetColorCompNum() == 0)
                    {
                        return new SolidColorBrush(Colors.Transparent);
                    }
                    color = mAnnot.GetColorAsRGB();
                    return new SolidColorBrush(Color.FromArgb(255, (byte)(color.Get(0) * 255 + 0.5), (byte)(color.Get(1) * 255 + 0.5), (byte)(color.Get(2) * 255 + 0.5)));
            }

            return null;
        }

        protected void SetColorOfAnnot(SolidColorBrush brush, int effectiveBrush)
        {
            int colorCompNum = 3;
            ColorPt color = new ColorPt(brush.Color.R / 255.0, brush.Color.G / 255.0, brush.Color.B / 255.0);

            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor settingsColor = new pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor();
            settingsColor.R = brush.Color.R;
            settingsColor.G = brush.Color.G;
            settingsColor.B = brush.Color.B;
            settingsColor.Use = true;

            if (brush.Color.A == 0)
            {
                colorCompNum = 0;
                color = new ColorPt(0, 0, 0, 0);
                settingsColor.Use = false;
            }

            Annot.Type aType = mAnnot.GetType();

            switch (aType)
            {
                case Annot.Type.e_Circle:
                case Annot.Type.e_Square:
                case Annot.Type.e_Line:
                case Annot.Type.e_Ink:
                case Annot.Type.e_Polyline:
                case Annot.Type.e_Polygon:
                    //pdftron.PDF.Annots.Markup markup = new pdftron.PDF.Annots.Markup(mAnnot.GetSDFObj());
                    if (effectiveBrush == e_stroke)
                    {
                        mMarkup.SetColor(color,colorCompNum);
                        pdftron.PDF.Tools.Utilities.ColorSettings.StrokeColor = settingsColor;
                    }
                    else if (effectiveBrush == e_fill)
                    {
                        mMarkup.SetInteriorColor(color, colorCompNum);
                        pdftron.PDF.Tools.Utilities.ColorSettings.FillColor = settingsColor;
                    }
                    break;
                case Annot.Type.e_Highlight:
                case Annot.Type.e_Underline:
                case Annot.Type.e_Squiggly:
                case Annot.Type.e_StrikeOut:
                    mMarkup.SetColor(color, colorCompNum);
                    if (aType == Annot.Type.e_Highlight)
                    {
                        pdftron.PDF.Tools.Utilities.ColorSettings.HighlightColor = settingsColor;
                    }
                    else
                    {
                        pdftron.PDF.Tools.Utilities.ColorSettings.TextMarkupColor = settingsColor;
                    }
                    break;
                case Annot.Type.e_FreeText:
                    if (effectiveBrush == e_text_stroke)
                    {
                        mFreeText.SetTextColor(color, colorCompNum);
                        pdftron.PDF.Tools.Utilities.ColorSettings.TextColor = settingsColor;
                    }
                    if (effectiveBrush == e_fill)
                    {
                        mFreeText.SetColor(color, colorCompNum);
                        pdftron.PDF.Tools.Utilities.ColorSettings.TextFillColor = settingsColor;
                    }
                    if (effectiveBrush == e_stroke)
                    {
                        mFreeText.SetLineColor(color, colorCompNum);
                        pdftron.PDF.Tools.Utilities.ColorSettings.TextLineColor = settingsColor;
                    }
                    break;
                case Annot.Type.e_Link:
                    mAnnot.SetColor(color, colorCompNum);
                    break;
            }
        }

        protected bool CanColorBeEmpty(int effectiveBrush)
        {
            switch (mAnnot.GetType())
            {
                case Annot.Type.e_Circle:
                case Annot.Type.e_Square:
                case Annot.Type.e_Polygon:
                    return true;
                case Annot.Type.e_Line:
                case Annot.Type.e_Ink:
                case Annot.Type.e_Polyline:
                    if (effectiveBrush == e_stroke)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                case Annot.Type.e_Highlight:
                case Annot.Type.e_Underline:
                case Annot.Type.e_Squiggly:
                case Annot.Type.e_StrikeOut:
                    return false;
                case Annot.Type.e_FreeText:
                    if (effectiveBrush == e_text_stroke)
                    {
                        return false;
                    }
                    if (effectiveBrush == e_fill)
                    {
                        return true;
                    }
                    if (effectiveBrush == e_stroke)
                    {
                        return false;
                    }
                    break;
            }

            return false;
        }

        #endregion Utility Functions


        #region Editing Free Text Annotation

        protected void CreateTextPopup()
        {
            mHandlingTool.ShouldHandleKeyEvents = false;

            HideAppearance();
            mSaveTextWhenExiting = true;

            // create the control to host the text canvas
            mTextPopupBorder = new Border();
            mTextPopupBorder.Width = mBoundingBoxRect.Width();
            mTextPopupBorder.Height = mBoundingBoxRect.Height();

            pdftron.PDF.Annot.BorderStyle bs = mAnnot.GetBorderStyle();
            

            mTextPopupBorder.BorderThickness = new System.Windows.Thickness(bs.width * mPDFView.GetZoom());
            mTextPopupBorder.BorderBrush = LineColor;

            // create text box
            mTextBox = new TextBox();
            mTextBox.TextWrapping = System.Windows.TextWrapping.Wrap;
            mTextBox.AcceptsReturn = true;
            mTextBox.AcceptsTab = true;
            mTextBox.AutoWordSelection = true;

            mTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(TextBoxScrollChanged), true);

            mTextBox.Foreground = TextColor;

            SolidColorBrush backgrondBrush = FillColor;
            if (backgrondBrush.Color.A == 0)
            {
                mTextBox.Background = Brushes.White;
            }
            else
            {
                mTextBox.Background = backgrondBrush;
                mTextBox.CaretBrush = mTextBox.Foreground;
            }
            double fontSize = FontSize;
            if (fontSize == 0)
            {
                fontSize = 12;
            }

            mTextBox.FontSize = fontSize * mPDFView.GetZoom();
            mTextBox.FontFamily = new FontFamily("Arial");
            mTextBox.Loaded += mTextBox_Loaded;
            mTextBox.KeyDown += TextBox_KeyDown;
            mTextBox.Unloaded += TextBox_Unloaded;
            mTextBox.TextChanged += mTextBox_TextChanged;

            mOldText = mFreeText.GetContents();
            mTextBox.Text = mOldText;

            mTextPopupBorder.SetValue(Canvas.LeftProperty, mBoundingBoxRect.x1);
            mTextPopupBorder.SetValue(Canvas.TopProperty, mBoundingBoxRect.y1);

            mTextPopupBorder.Child = mTextBox;
            mHandlingTool.Children.Add(mTextPopupBorder);
            mIsTextPopupOpen = true;
        }

        void mTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mIsFreeTextEdited = true;
            bool locked = false;
            try
            {
                locked = mPDFView.DocTryLock();
                if (locked)
                {
                    mFreeText.SetContents(mTextBox.Text);
                    mFreeText.RefreshAppearance();
                    mIsContentUpToDate = true;
                }
                else
                {
                    mIsContentUpToDate = false;
                }
            }
            catch (System.Exception)
            {

            }
            finally
            {
                if (locked)
                {
                    mPDFView.DocUnlock();
                }
            }
        }

        private void TextBoxScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true; // this will prevent the entire view from scrolling to accommodate the text box
        }

        void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseTextPopup();
            }
        }

        void mTextBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            mTextBox.Focus();
            mTextBox.SelectionStart = mTextBox.Text.Length;
        }

        void TextBox_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!mSaveTextWhenExiting)
            {
                try
                {
                    mPDFView.DocLock(true);
                    mFreeText.SetContents(mOldText);
                    mFreeText.RefreshAppearance();
                }
                catch (System.Exception)
                {

                }
                finally
                {
                    mPDFView.DocUnlock();
                }
            }


            if (mIsFreeTextEdited)
            {
                try
                {
                    mPDFView.DocLock(true);
                    if (!mIsContentUpToDate)
                    {
                        mFreeText.SetContents(mTextBox.Text);
                        mFreeText.RefreshAppearance();
                    }
                    UpdateDate();
                }
                catch (System.Exception)
                {

                }
                finally
                {
                    mPDFView.DocUnlock();
                }
                mHandlingTool.ToolManager.RaiseAnnotationEditedEvent(mAnnot);
            }

            mPDFView.Update(mFreeText, mPageNumber);
            mHandlingTool.ShouldHandleKeyEvents = true;
        }


        protected void CloseTextPopup()
        {
            mSaveTextWhenExiting = false;
            if (mHandlingTool.Children.Contains(mTextPopupBorder))
            {
                mHandlingTool.Children.Remove(mTextPopupBorder);
            }
            ShowAppearance();
        }

        #endregion Editing Free Text Annotation
    }
}