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




namespace pdftron.PDF.Tools
{
    public class AnnotEdit : Tool
    {
        internal struct AnnotWithPageNum
        {
            internal Annot Annotation;
            internal int PageNumber;

            internal AnnotWithPageNum(Annot a, int p)
            {
                Annotation = a;
                PageNumber = p;
            }
        }

        protected enum EditState
        {
            e_nothing,
            e_select,
            e_add_selection,
            e_manipulate_single,
            e_manipulate_many,
        }

        /// <summary>
        /// Returns the currently selected annotations.
        /// </summary>
        public List<Annot> SelectedAnnotations
        {
            get
            {
                List<Annot> annots = new List<Annot>();
                if (mSelectedAnnotation != null)
                {
                    annots.Add(mSelectedAnnotation.Annot);
                }
                else
                {
                    foreach (SelectionHelper selection in mSelectedAnnotations)
                    {
                        annots.Add(selection.Annot);
                    }
                }
                return annots;
            }
        }


        // these 2 should be mutually exclusive, either we have a selected annotation or we have many.
        internal List<SelectionHelper> mSelectedAnnotations; // if we have multiple annots selected
        internal SelectionHelper mSelectedAnnotation; // if we only have 1 annotation selected.
        internal List<AnnotWithPageNum> mNewSelection; // The annotations that were hit by a click and drag.
        protected int[] mVisiblePagesOnScreen;

        protected bool mMultipleAnnotationSelected = false;
        protected bool mMultiPageSelection = false;
        protected int mSelectedAnnotationPageNumber;
        protected EditState mEditingState;

        internal List<SelectionHelper> mPreviousSelection;

        // Manipulation
        protected int mEffectiveCtrlPoint = -1; // 8 for translate, -1 for none.
        protected bool mIsMoving = false;
        protected PDFRect mPageCropOnClient;
        protected UIPoint mDownPoint;
        protected UIPoint mDragPoint;
        protected bool mAnnotIsScalable = true;
        protected bool mAreAnnotsMovable = true;

        protected PDFRect mSelectionArea; // This rectangle is the minimum bounding box that contains all Annots
        protected PDFRect mMovementBoundingBox;
        protected TranslateTransform mRenderTransform;

        protected const int START_MOVING_THRESHOLD = 5; // The distance we need to drag before the annot starts moving

        protected bool mIsDoubleClicking = false;
        protected UIPoint mDoubleClickDownPoint;

        // Graphics state
        protected List<Path> mAnnotationSelectionRectangles; // This is the list of currently selected Annotations.
        protected List<RectangleGeometry> mAnnotationSelectionRectangleGeometries; // This is their shape.
        
        // Dashed selection box
        protected System.Windows.Shapes.Path mSelectionWidget;
        protected RectangleGeometry mSelectionWidgetGeometry;

        //////////////////////////////////////////////////////////////////////////
        // Context Menu
        protected int mRightClickPageNumber;


        public AnnotEdit(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_annot_edit;
            mNextToolMode = ToolManager.ToolType.e_annot_edit;
            DisallowTextSelection();
        }


        internal override void OnCreate()
        {
            if (mAnnot != null)
            {
                mSelectedAnnotation = new SelectionHelper(mPDFView, this, mAnnot, mAnnot.GetPage().GetIndex());
            }
            
            // Add the annot edit tool to the Viewer Canvas immediately.
            mViewerCanvas = mToolManager.AnnotationCanvas;
            mViewerCanvas.Children.Insert(0, this);
            CreateSelectionWidget();

            mSelectedAnnotations = new List<SelectionHelper>();
            mAnnotationSelectionRectangles = new List<Path>();
            mAnnotationSelectionRectangleGeometries = new List<RectangleGeometry>();
            mNewSelection = new List<AnnotWithPageNum>();

            mPreviousSelection = new List<SelectionHelper>();

            this.Width = mViewerCanvas.ActualWidth;
            this.Height = mViewerCanvas.ActualHeight;
        }

        internal override void OnClose()
        {
            mViewerCanvas.Children.Remove(this);
            mSelectedAnnotation = null;
            mSelectedAnnotations.Clear();
            HandleSelectionChangedEvent();
        }

        internal override void ZoomChangedHandler(object sender, RoutedEventArgs e)
        {
            HandleLayoutChanges();
        }

        internal override void LayoutChangedHandler(object sender, RoutedEventArgs e)
        {
            HandleLayoutChanges();
        }

        protected void HandleLayoutChanges()
        {
            if (mSelectedAnnotation != null)
            {
                mSelectedAnnotation.ZoomChanged();
            }
            else
            {
                foreach (SelectionHelper selection in mSelectedAnnotations)
                {
                    selection.ZoomChanged();
                }
            }
            ResolveSelectionAndDrawAppearnce();
        }

        internal override void CurrentPageNumberChangedHandler(PDFViewWPF viewer, int currentPage, int totalPages)
        {
            pdftron.PDF.PDFViewWPF.PagePresentationMode mode = mPDFView.GetPagePresentationMode();
            if (mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_single_page ||
                mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing ||
                mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing_cover)
            {
                ResolveSelectionAndDrawAppearnce();
            }
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
            ProcessInputDown(e);
            mViewerCanvas.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            if (mIsContextMenuOpen)
            {
                return;
            }
            GetModifierKeyStates();

            mDownPoint = GetPosition(e, mViewerCanvas);
            mVisiblePagesOnScreen = mPDFView.GetVisiblePages();

            // We need to determine what we are supposed to do here.
            if (mIsCtrlDown)
            {
                // we have exactly one annotation
                if (mSelectedAnnotation != null)
                {
                    mEffectiveCtrlPoint = mSelectedAnnotation.GetControlPoint(mDownPoint);
                    // We hit an annotation, so we want to deselect it.
                    if (mVisiblePagesOnScreen.Contains(mSelectedAnnotation.PageNumber) && mEffectiveCtrlPoint >= 0)
                    {

                        mEditingState = EditState.e_nothing; // mouse move should do nothing
                        mSelectedAnnotation.HideAppearance();
                        mSelectedAnnotation = null;
                    }
                    // We didn't hit any annotation
                    else
                    {
                        if (AddAnnotationToSelectionIfPresent())
                        {
                            // We hit an annotation, add it, then do nothing for the drag.
                            ResolveSelectionAndDrawAppearnce();
                            mEditingState = EditState.e_nothing;
                        }
                        // We didn't hit an annotation, so we add to selection
                        else
                        {
                            mEditingState = EditState.e_add_selection;
                        }
                    }
                }

                // We have multiple selections
                else if (mSelectedAnnotations.Count > 1)
                {
                    SelectionHelper selection = IsPointInAnnotation(mDownPoint);
                    // We hit one of our selected annotations, deselect it and then do nothing
                    if (selection != null)
                    {
                        mSelectedAnnotations.Remove(selection);
                        ResolveSelectionAndDrawAppearnce();
                        mEditingState = EditState.e_nothing; // mouse move should do nothing
                    }
                    // We didn't hit one of our selected annotation
                    else
                    {
                        if (AddAnnotationToSelectionIfPresent())
                        {
                            // We hit an unselected annotation, add it, then do nothing for the drag.
                            ResolveSelectionAndDrawAppearnce();
                            mEditingState = EditState.e_nothing;
                        }
                        // We didn't hit an annotation, so we add to selection
                        else
                        {
                            mEditingState = EditState.e_add_selection;
                        }
                    }
                }

                // We have no annotations selected
                else
                {

                    if (AddAnnotationToSelectionIfPresent())
                    {
                        // We hit an annotation, make it our selected annotation 
                        ResolveSelectionAndDrawAppearnce();
                        mEditingState = EditState.e_nothing;
                    }
                    // We hit nothing, so just select
                    else
                    {
                        mEditingState = EditState.e_select;
                    }
                }
            }
            else // ctrl is not down
            {
                // We have 1 selected annotation
                if (mSelectedAnnotation != null)
                {
                    if (e is TouchEventArgs)
                    {
                        mSelectedAnnotation.IsUsingTouch = true;
                    }
                    else
                    {
                        mSelectedAnnotation.IsUsingTouch = false;
                    }
                    mEffectiveCtrlPoint = mSelectedAnnotation.GetControlPoint(mDownPoint);
                    if (!mVisiblePagesOnScreen.Contains(mSelectedAnnotation.PageNumber))
                    {
                        mEffectiveCtrlPoint = -1;
                    }

                    if (mEffectiveCtrlPoint >= 0)
                    {
                        mEditingState = EditState.e_manipulate_single;
                        mSelectedAnnotation.PrepareForMove(mDownPoint);
                    }
                    else
                    {
                        if (AddAnnotationToSelectionIfPresent())
                        {
                            // We hit an annotation, make it our selected annotation and then do nothing
                            mSelectedAnnotation.HideAppearance();
                            mSelectedAnnotation = null;
                            ResolveSelectionAndDrawAppearnce();
                            mEditingState = EditState.e_nothing;

                        }
                        // We hit nothing, so just select
                        else
                        {
                            mSelectedAnnotation.HideAppearance();
                            mSelectedAnnotation = null;
                            mEditingState = EditState.e_select;
                        }
                    }
                }

                // We have multiple selected annotations
                else if (mSelectedAnnotations.Count() > 1)
                {
                    // The point is inside an annotation, so we start moving them
                    if (IsPointInAnnotation(mDownPoint) != null)
                    {
                        if (mMultiPageSelection) // We don't allow manipulation when we have selected annotations on multiple pages.
                        {
                            mEditingState = EditState.e_nothing;
                        }
                        else
                        {
                            if (!mAreAnnotsMovable)
                            {
                                mEditingState = EditState.e_nothing;
                            }
                            else
                            {
                                PrepareMultiAnnotationMove();
                                mEditingState = EditState.e_manipulate_many;
                            }
                        }
                    }
                    // We hit nothing, start selecting from scratch
                    else
                    {
                        if (AddAnnotationToSelectionIfPresent())
                        {
                            mSelectedAnnotations.Clear();
                            ResolveSelectionAndDrawAppearnce();
                            mEditingState = EditState.e_nothing;
                        }
                        // We hit nothing, so just select
                        else
                        {
                            mSelectedAnnotations.Clear();
                            this.Children.Clear();
                            mEditingState = EditState.e_select;
                        }
                    }
                }
                // We have no annotation selected
                else
                {
                    if (AddAnnotationToSelectionIfPresent())
                    {
                        // We hit an annotation, make it our selected annotation 
                        ResolveSelectionAndDrawAppearnce();
                        mEditingState = EditState.e_nothing;
                    }
                    // We hit nothing, so just select
                    else
                    {
                        mEditingState = EditState.e_select;
                    }
                }
            }
            mIsMoving = false;
        }



        internal override void MouseMovedHandler(object sender, System.Windows.Input.MouseEventArgs e)
        {
            base.MouseMovedHandler(sender, e);

            if (!mIsDragging)
            {
                if (mSelectedAnnotation != null)
                {
                    mSelectedAnnotation.SetCursor(GetPosition(e, mViewerCanvas));
                }
                return;
            }

            ProcessInputMove(e);
        }

        public override void TouchMoveHandler(object sender, TouchEventArgs e)
        {
            base.TouchMoveHandler(sender, e);
            ProcessInputMove(e);
        }

        private void ProcessInputMove(InputEventArgs e)
        {
            // Makes sure we have moved enough to make it worthwhile
            mDragPoint = GetPosition(e, mViewerCanvas);
            if (!mIsMoving)
            {
                if (GetDistance(mDragPoint, mDownPoint) > START_MOVING_THRESHOLD)
                {
                    mIsMoving = true;
                    if (mEditingState == EditState.e_add_selection || mEditingState == EditState.e_select)
                    {
                        this.Children.Add(mSelectionWidget);
                    }
                }
                else return;
            }

            switch (mEditingState)
            {
                case EditState.e_nothing:
                    return;

                case EditState.e_select:
                case EditState.e_add_selection:
                    DrawSelectionWidget();
                    break;

                case EditState.e_manipulate_many:
                    CheckMultiMoveBounds();
                    mRenderTransform.X = mDragPoint.X - mDownPoint.X;
                    mRenderTransform.Y = mDragPoint.Y - mDownPoint.Y;
                    foreach (SelectionHelper selection in mSelectedAnnotations)
                    {
                        selection.Translated(mRenderTransform.X, mRenderTransform.Y);
                    }
                    break;

                case EditState.e_manipulate_single:
                    mSelectedAnnotation.Move(mDragPoint);

                    break;
            }
        }

        internal override void MouseLeftButtonUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            mViewerCanvas.ReleaseMouseCapture();
            ProcessInputUp(e);
        }

        public override void TouchUpHandler(object sender, TouchEventArgs e)
        {
            base.TouchUpHandler(sender, e);
            mViewerCanvas.ReleaseTouchCapture(e.TouchDevice);
            ProcessInputUp(e);
        }

        private void ProcessInputUp(InputEventArgs e)
        {
            if (this.Children.Contains(mSelectionWidget))
            {
                this.Children.Remove(mSelectionWidget);
            }

            bool wasDoubleClicking = false;
            if (mIsDoubleClicking)
            {
                mIsDoubleClicking = false;
                UIPoint upPoint = GetPosition(e, mPDFView);

                if (GetDistance(upPoint, mDoubleClickDownPoint) < START_MOVING_THRESHOLD)
                {
                    wasDoubleClicking = true;
                    UIPoint doubleClickPoint = GetPosition(e, mViewerCanvas);

                    if (mSelectedAnnotation != null)
                    {
                        if (mSelectedAnnotation.BoundingBoxRect.Contains(doubleClickPoint.X, doubleClickPoint.Y))
                        {
                            mSelectedAnnotation.HandleDoubleClick();
                        }
                    }
                    else
                    {
                        SelectionHelper selection = IsPointInAnnotation(doubleClickPoint);
                        if (selection != null)
                        {
                            selection.HandleDoubleClick();
                        }

                    }
                }
            }

            if (!wasDoubleClicking)// Not double clicking
            {
                switch (mEditingState)
                {
                    case EditState.e_nothing:
                        return;

                    case EditState.e_select:
                    case EditState.e_add_selection:
                        if (mIsMoving)
                        {
                            GetAnnotationsInSelection();
                        }
                        else
                        {
                            pdftron.SDF.Obj annotObj = mPDFView.GetAnnotationAt(mDownPoint.X - mPDFView.GetHScrollPos(), mDownPoint.Y - mPDFView.GetVScrollPos());

                            if (annotObj != null)
                            {
                                Annot annot = new Annot(annotObj);

                                if (CanAnnotationBeEdited(annot))
                                {
                                    mNewSelection.Add(new AnnotWithPageNum(new Annot(annotObj),
                                        mPDFView.GetPageNumberFromScreenPt(mDownPoint.X - mPDFView.GetHScrollPos(), mDownPoint.Y - mPDFView.GetVScrollPos())));
                                }
                            }
                        }
                        ResolveSelectionAndDrawAppearnce();
                        break;

                    case EditState.e_manipulate_many:
                        // Need to move annotations
                        mRenderTransform.X = 0;
                        mRenderTransform.Y = 0;
                        UpdateSelectedAnnotations();

                        break;

                    case EditState.e_manipulate_single:
                        if (mIsMoving)
                        {

                            mSelectedAnnotation.Finished();
                        }

                        break;

                }
            }
            mEditingState = EditState.e_nothing;
        }



        internal override void PreviewMouseWheelHandler(object sender, System.Windows.Input.MouseWheelEventArgs e)
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


        internal override void KeyDownAction(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!mShouldHandleKeyEvents)
            {
                return;
            }
            if (mIsCtrlDown)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.C:
                        CopyAnnotations();
                        break;
                    case System.Windows.Input.Key.V:
                        mRightClickPageNumber = mPDFView.GetCurrentPage();
                        PasteAnnotations();
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Delete:
                        DeleteAnnotations();
                        break;
                }
            }
            base.KeyDownAction(sender, e);
        }


        internal override void MouseDoubleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseDoubleClickHandler(sender, e);
            mDoubleClickDownPoint = e.GetPosition(mPDFView);
            mIsDoubleClicking = true;
        }


        #region Multiple Selection


        /// <summary>
        /// This draws the simple manipulation Rectangle for the annotation.
        /// This should be used when multiple annotations are selected.
        /// </summary>
        /// <param name="annot">The annotation to create a rectangle around</param>
        internal void DrawManipulationRectangle(SelectionHelper selection)
        {
            PDFRect rect = selection.BoundingBoxRect;

            Path rPath = new Path();
            RectangleGeometry rGeom = new RectangleGeometry();
            rPath.Data = rGeom;
            rGeom.Rect = new UIRect(rect.x1, rect.y1, rect.Width(), rect.Height());
            rPath.Stroke = new SolidColorBrush(Colors.Gray);
            rPath.StrokeThickness = 3;
            DoubleCollection d = new DoubleCollection();
            d.Add(2);
            d.Add(2);
            rPath.StrokeDashArray = d;

            mAnnotationSelectionRectangles.Add(rPath);
            mAnnotationSelectionRectangleGeometries.Add(rGeom);
            this.Children.Add(rPath);
            
        }


        /// <summary>
        /// This will do a union of the Annots in mSelectedAnnotations or mSelectedAnnotation with annotsAndPageNums.
        /// </summary>
        /// <param name="annotsAndPageNums"></param>
        internal void ResolveSelection(List<AnnotWithPageNum> annotsAndPageNums) // TODO: FIX WIDGETS
        {
            // this can happen if we just removed one annotation from here.
            if (mSelectedAnnotations.Count() == 1)
            {
                mSelectedAnnotation = mSelectedAnnotations[0];
                mSelectedAnnotations.Clear();
            }

            // if we got no new annotations
            if (annotsAndPageNums == null || annotsAndPageNums.Count() == 0 )
            {
                return;
            }

            mMultipleAnnotationSelected = false;
            mMultiPageSelection = false;
            // if we only have 1 annot selected, and they're the same.
            if (annotsAndPageNums.Count() == 1 && mSelectedAnnotation != null && annotsAndPageNums[0].Annotation == mSelectedAnnotation.Annot)
            {
                return;
            }

            // if we have 0 selected before, and only 1 now.
            if (mSelectedAnnotation == null && mSelectedAnnotations.Count() == 0 && annotsAndPageNums.Count() == 1)
            {
                if (annotsAndPageNums[0].Annotation.GetType() == Annot.Type.e_Line)
                {
                    mSelectedAnnotation = new LineSelection(mPDFView, this, annotsAndPageNums[0].Annotation, annotsAndPageNums[0].PageNumber);
                }
                else
                {
                    mSelectedAnnotation = new SelectionHelper(mPDFView, this, annotsAndPageNums[0].Annotation, annotsAndPageNums[0].PageNumber);
                }
                return;
            }

            // At this point, we have at least 1 old annotation, and at least 1 new annotation
            mMultipleAnnotationSelected = true;

            // Move mSelectedAnnotation into the list if necessary
            if (mSelectedAnnotation != null)
            {
                mSelectedAnnotations.Add(mSelectedAnnotation);
                mSelectedAnnotation = null;
            }

            // merge the lists
            foreach (AnnotWithPageNum annot in annotsAndPageNums)
            {

                SelectionHelper sel;
                if (annot.Annotation.GetType() == Annot.Type.e_Line)
                {
                    sel = new LineSelection(mPDFView, this, annot.Annotation, annot.PageNumber);
                }
                else
                {
                    sel = new SelectionHelper(mPDFView, this, annot.Annotation, annot.PageNumber);
                }
                if (!mSelectedAnnotations.Contains(sel))
                {
                    mSelectedAnnotations.Add(sel);
                }
            }
        }


        /// <summary>
        /// Returns a list of all the annotations in the selection rectangle.
        /// </summary>
        /// <returns></returns>
        protected void GetAnnotationsInSelection()
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            PDFRect selectionRect = new PDFRect(mDownPoint.X - sx, mDownPoint.Y - sy, mDragPoint.X - sx, mDragPoint.Y - sy);
            
            List<int> pages = GetPagesInRect(selectionRect);
            if (pages.Count() == 0)
            {
                return;
            }

            try
            {
                mPDFView.DocLockRead();

                foreach (int pageNum in pages)
                {
                    double x1 = selectionRect.x1;
                    double y1 = selectionRect.y1;
                    double x2 = selectionRect.x2;
                    double y2 = selectionRect.y2;

                    mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, pageNum);
                    mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, pageNum);

                    PDFRect pageSpaceRect = new PDFRect(x1, y1, x2, y2);
                    pageSpaceRect.Normalize();

                    PDFPage page = mPDFView.GetDoc().GetPage(pageNum);

                    int annotNum = page.GetNumAnnots();
                    for (int i = 0; i < annotNum; i++)
                    {
                        Annot annot = page.GetAnnot(i);

                        // make sure it's a type we can handle.
                        if (!CanAnnotationBeEdited(annot))
                        {
                            continue;
                        }

                        PDFRect annotRect = annot.GetRect();
                        if (annotRect.IntersectRect(pageSpaceRect, annotRect))
                        {
                            mNewSelection.Add(new AnnotWithPageNum(annot, pageNum));
                        }
                        else
                        {
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

        /// <summary>
        /// Sets up the Annot edit tool for manipulation of multiple elements.
        /// </summary>
        protected void PrepareMultiAnnotationMove()
        {
            // At this point, we can expect at least 2 annotations in mSelectedAnnotations
            // They're all on the same page, and they're all movable.
            mSelectedAnnotationPageNumber = mSelectedAnnotations[0].PageNumber;

            mPageCropOnClient = BuildPageBoundBoxOnClient(mSelectedAnnotationPageNumber);

            mMovementBoundingBox = new PDFRect(mPageCropOnClient.x1 + mDownPoint.X - mSelectionArea.x1,
                mPageCropOnClient.y1 + mDownPoint.Y - mSelectionArea.y1,
                mPageCropOnClient.x2 + mDownPoint.X - mSelectionArea.x2,
                mPageCropOnClient.y2 + mDownPoint.Y - mSelectionArea.y2);

            mRenderTransform = new TranslateTransform();
            this.RenderTransform = mRenderTransform;
        }

        /// <summary>
        /// Checks the bounds for moving multiple annotations
        /// </summary>
        protected void CheckMultiMoveBounds()
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

        protected void UpdateSelectedAnnotations()
        {
            // calculate distance moved in page space.
            double x1 = mDownPoint.X;
            double y1 = mDownPoint.Y;
            double x2 = mDragPoint.X;
            double y2 = mDragPoint.Y;

            mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mSelectedAnnotationPageNumber);
            mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mSelectedAnnotationPageNumber);

            UIPoint movement = new UIPoint(x2 - x1, y2 - y1);

            try
            {
                mPDFView.DocLock(true);
                foreach (SelectionHelper selection in mSelectedAnnotations)
                {
                    selection.MoveAnnotation(movement);
                }
            }
            catch (System.Exception) { }
            finally
            {
                mPDFView.DocUnlock();
            }

            foreach (SelectionHelper selection in mSelectedAnnotations)
            {
                mToolManager.RaiseAnnotationEditedEvent(selection.Annot);
            }

            // now, calculate old and new update rect for the annotations.
            movement.X = mDragPoint.X - mDownPoint.X;
            movement.Y = mDragPoint.Y - mDownPoint.Y;

            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            PDFRect oldRect = new PDFRect(mSelectionArea.x1 - sx, mSelectionArea.y1 - sy, mSelectionArea.x2 - sx, mSelectionArea.y2 - sy);
            PDFRect newRect = new PDFRect(mSelectionArea.x1 - sx + movement.X, mSelectionArea.y1 - sy + movement.Y, 
                mSelectionArea.x2 - sx + movement.X, mSelectionArea.y2 - sy + movement.Y);

            mPDFView.Update(oldRect);
            mPDFView.Update(newRect);

            mSelectionArea.Set(mSelectionArea.x1 + movement.X, mSelectionArea.y1 + movement.Y,
                mSelectionArea.x2 + movement.X, mSelectionArea.y2 + movement.Y);

            ResolveSelectionAndDrawAppearnce();
        }

        /// <summary>
        /// This function draws the dashed box that shows where we're currently selecting.
        /// </summary>
        protected void DrawSelectionWidget()
        {
            double minX = Math.Min(mDownPoint.X, mDragPoint.X);
            double minY = Math.Min(mDownPoint.Y, mDragPoint.Y);
            double distX = Math.Abs(mDownPoint.X - mDragPoint.X);
            double distY = Math.Abs(mDownPoint.Y - mDragPoint.Y);

            mSelectionWidgetGeometry.Rect = new UIRect(minX, minY, distX, distY);
        }

        /// <summary>
        /// Creates the selection Widget for DrawSelection
        /// </summary>
        protected void CreateSelectionWidget()
        {
            mSelectionWidgetGeometry = new RectangleGeometry();
            mSelectionWidget = new Path();
            mSelectionWidget.Data = mSelectionWidgetGeometry;
            mSelectionWidget.Stroke = new SolidColorBrush(Colors.Black);

            DoubleCollection d = new DoubleCollection();
            d.Add(2.5);
            d.Add(2.5);
            mSelectionWidget.StrokeDashArray = d;
        }

        #endregion MultipleSelection





        #region Utility Functions
        /// <summary>
        /// Returns the selection which contains point.
        /// </summary>
        /// <param name="point">A point in the viewers GetCanvas() space</param>
        /// <returns></returns>
        internal SelectionHelper IsPointInAnnotation(UIPoint point)
        {
            mVisiblePagesOnScreen = mPDFView.GetVisiblePages();
            foreach (SelectionHelper selection in mSelectedAnnotations)
            {
                if (mVisiblePagesOnScreen.Contains(selection.PageNumber) && selection.IsPointInRect(point.X, point.Y))
                {
                    return selection;
                }
            }
            return null;
        }


        /// <summary>
        /// This will update and draw the new selection.
        /// </summary>
        /// <param name="annots">The annotations to (potentially) add to the selection</param>
        protected void ResolveSelectionAndDrawAppearnce()
        {
            ResolveSelection(mNewSelection);
            mNewSelection.Clear();

            mAnnotationSelectionRectangles.Clear();
            this.Children.Clear();

            mMultiPageSelection = false;
            mAreAnnotsMovable = true;
            mSelectionArea = new PDFRect();

            // determine which pages to draw annots on, leave page1 as 0 if continuous mode, so we draw everything
            int page1 = 0; 
            int page2 = 0;
            pdftron.PDF.PDFViewWPF.PagePresentationMode mode = mPDFView.GetPagePresentationMode();
            if (mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_single_page)
            {
                page1 = mPDFView.GetCurrentPage();
            }
            else if (mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing)
            {
                page1 = mPDFView.GetCurrentPage();
                page2 = page1 % 2 == 0 ? page1 - 1 : page1 + 1;
            }
            else if (mode == pdftron.PDF.PDFViewWPF.PagePresentationMode.e_facing_cover)
            {
                page1 = mPDFView.GetCurrentPage();
                page2 = page1 % 2 == 0 ? page1 + 1 : page1 - 1;
            }

            if (mSelectedAnnotations.Count() > 1)
            {
                int pageNum = mSelectedAnnotations[0].PageNumber;
                mSelectionArea.Set(mSelectedAnnotations[0].BoundingBoxRect);
                foreach (SelectionHelper selection in mSelectedAnnotations)
                {
                    if (page1 == 0 || selection.PageNumber == page1 || selection.PageNumber == page2)
                    {
                        DrawManipulationRectangle(selection);
                        if (selection.PageNumber != pageNum)
                        {
                            mMultiPageSelection = true;
                        }
                        mAreAnnotsMovable &= selection.IsMovable;
                        PDFRect compareRect = selection.BoundingBoxRect;
                        mSelectionArea.x1 = Math.Min(mSelectionArea.x1, compareRect.x1);
                        mSelectionArea.y1 = Math.Min(mSelectionArea.y1, compareRect.y1);
                        mSelectionArea.x2 = Math.Max(mSelectionArea.x2, compareRect.x2);
                        mSelectionArea.y2 = Math.Max(mSelectionArea.y2, compareRect.y2);
                    }
                }
            }

            else if (mSelectedAnnotation != null)
            {
                if (page1 == 0 || mSelectedAnnotation.PageNumber == page1 || mSelectedAnnotation.PageNumber == page2)
                {
                    mSelectedAnnotation.ShowAppearance();
                }
            }
            HandleSelectionChangedEvent();
        }

        /// <summary>
        /// If there's an annotation under mDownPoint, it will be added to the mNewSelection list, and this function will return true,
        /// otherwise, it returns false;
        /// </summary>
        /// <returns></returns>
        protected bool AddAnnotationToSelectionIfPresent()
        {
            // if we have an annotation, add it to the new selection
            pdftron.SDF.Obj annotObj = mPDFView.GetAnnotationAt(mDownPoint.X - mPDFView.GetHScrollPos(), mDownPoint.Y - mPDFView.GetVScrollPos());
            if (annotObj != null)
            {
                Annot annot = new Annot(annotObj);

                if (CanAnnotationBeEdited(annot))
                {
                    mNewSelection.Add(new AnnotWithPageNum(new Annot(annotObj),
                        mPDFView.GetPageNumberFromScreenPt(mDownPoint.X - mPDFView.GetHScrollPos(), mDownPoint.Y - mPDFView.GetVScrollPos())));
                    return true;
                }
            }

            // We didn't hit an annotation, so we add to selection
            return false;
        }


        private void PasteAnnotations()
        {
            if (mToolManager.CurrentCopiedAnnotations.Count() == 0)
            {
                return;
            }

            bool success = false;
            PDFRect totalBoundingBox = null;
            try
            {
                mPDFView.DocLock(true);
                List<Annot> newAnnots = new List<Annot>();

                // Calculate cumulative bounding box.
                foreach (Annot annot in mToolManager.CurrentCopiedAnnotations)
                {
                    if (totalBoundingBox == null)
                    {
                        totalBoundingBox = annot.GetRect();
                        totalBoundingBox.Normalize();
                    }
                    else
                    {
                        PDFRect rect = annot.GetRect();
                        rect.Normalize();
                        totalBoundingBox.x1 = Math.Min(totalBoundingBox.x1, rect.x1);
                        totalBoundingBox.y1 = Math.Min(totalBoundingBox.y1, rect.y1);
                        totalBoundingBox.x2 = Math.Max(totalBoundingBox.x2, rect.x2);
                        totalBoundingBox.y2 = Math.Max(totalBoundingBox.y2, rect.y2);
                    }

                    pdftron.SDF.Obj srcAnnot = annot.GetSDFObj();
                    pdftron.SDF.Obj p = srcAnnot.FindObj("P");
                    srcAnnot.Erase("P");

                    pdftron.SDF.Obj destAnnot = mPDFView.GetDoc().GetSDFDoc().ImportObj(srcAnnot, true);
                    newAnnots.Add(new Annot(destAnnot));
                    if (p != null)
                    {
                        srcAnnot.Put("P", p);
                    }
                }


                // clear up old annotations
                if (newAnnots.Count() > 0)
                {
                    mSelectedAnnotations.Clear();
                    mSelectedAnnotation = null;

                    PDFPage page = mPDFView.GetDoc().GetPage(mRightClickPageNumber);


                    PDFRect cropBox = page.GetCropBox();

                    // how far should each annot be pushed to end up top-left?
                    double pushX = -totalBoundingBox.x1 + cropBox.x1;
                    double pushY = cropBox.y2 - totalBoundingBox.y2;

                    // since PDF pages start bottom left, but we want top left
                    totalBoundingBox.x2 -= totalBoundingBox.x1;
                    totalBoundingBox.x1 = 0;
                    totalBoundingBox.y1 += page.GetCropBox().y2 - totalBoundingBox.y2;
                    totalBoundingBox.y2 = page.GetCropBox().y2;


                    foreach (Annot annot in newAnnots)
                    {
                        page.AnnotPushBack(annot);

                        PDFRect rect = annot.GetRect();

                        rect.x1 += pushX;
                        rect.y1 += pushY;
                        rect.x2 += pushX;
                        rect.y2 += pushY;
                        annot.Resize(rect);
                        annot.RefreshAppearance();

                        mNewSelection.Add(new AnnotWithPageNum(annot, mRightClickPageNumber));
                    }
                    success = true;
                }
            }
            catch (System.Exception)
            {

            }
            finally
            {
                mPDFView.DocUnlock();
            }

            if (success)
            {
                ConvertPageRectToScreenRect(totalBoundingBox, mRightClickPageNumber);

                mPDFView.Update(totalBoundingBox);

                ResolveSelectionAndDrawAppearnce();

                foreach (SelectionHelper selection in mSelectedAnnotations)
                {
                    mToolManager.RaiseAnnotationAddedEvent(selection.Annot);
                }
            }
        }

        private void CopyAnnotations()
        {
            // Add selected annotation(s) to the list of copied annotation
            mToolManager.CurrentCopiedAnnotations.Clear();
            if (mSelectedAnnotation != null)
            {
                if (mSelectedAnnotation.CanBeCopied)
                {
                    mToolManager.CurrentCopiedAnnotations.Add(mSelectedAnnotation.Annot);
                }
            }

            foreach (SelectionHelper selection in mSelectedAnnotations)
            {
                if (selection.CanBeCopied)
                {
                    mToolManager.CurrentCopiedAnnotations.Add(selection.Annot);
                }
            }

        }

        private void DeleteAnnotations()
        {
            PDFRect updateRect = null;
            List<Annot> deletedAnnotations = new List<Annot>();
            try
            {
                mPDFView.DocLock(true);
                if (mSelectedAnnotation != null)
                {
                    deletedAnnotations.Add(mSelectedAnnotation.Annot);
                    PDFPage page = mPDFView.GetDoc().GetPage(mSelectedAnnotation.PageNumber);
                    page.AnnotRemove(mSelectedAnnotation.Annot);
                    updateRect = mSelectedAnnotation.BoundingBoxRect;
                    if (mSelectedAnnotation.Annot.IsMarkup())
                    {
                        mToolManager.NoteManager.CloseNote(new pdftron.PDF.Annots.Markup(mSelectedAnnotation.Annot));
                    }
                    mSelectedAnnotation = null;
                }
                else
                {
                    updateRect = mSelectionArea;
                    foreach (SelectionHelper selection in mSelectedAnnotations)
                    {
                        deletedAnnotations.Add(selection.Annot);
                        PDFPage page = mPDFView.GetDoc().GetPage(selection.PageNumber);
                        page.AnnotRemove(selection.Annot);
                        if (selection.Annot.IsMarkup())
                        {
                            mToolManager.NoteManager.CloseNote(new pdftron.PDF.Annots.Markup(selection.Annot));
                        }
                    }
                    mSelectedAnnotations.Clear();
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                mPDFView.DocUnlock();
            }
            if (updateRect != null)
            {
                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();
                updateRect.x1 -= sx;
                updateRect.y1 -= sy;
                updateRect.x2 -= sx;
                updateRect.y2 -= sy;
                mPDFView.Update(updateRect);

            }
            ResolveSelectionAndDrawAppearnce();
            foreach (Annot annot in deletedAnnotations)
            {
                mToolManager.RaiseAnnotationRemovedEvent(annot);
            }
        }

        protected bool CanAnnotationBeEdited(Annot annot)
        {
            return (annot.IsValid() && !annot.GetFlag(Annot.Flag.e_hidden) &&
                (annot.GetType() == Annot.Type.e_Link || annot.IsMarkup()));
        }

        /// <summary>
        /// returns true if there's at least 1 annotation in the selection that can be copied.
        /// </summary>
        /// <param name="downPoint">The point in the viewers GetCanvas() space where the mouse is</param>
        /// <returns></returns>
        private bool CanAnnotationsBeCopied(UIPoint downPoint)
        {
            if (mSelectedAnnotation != null)
            {
                if (mSelectedAnnotation.GetControlPoint(downPoint) >= 0
                    && mSelectedAnnotation.CanBeCopied)
                {
                    return true;
                }
            }
            else
            {
                SelectionHelper selection = IsPointInAnnotation(downPoint);
                if (selection != null)
                {
                    foreach (SelectionHelper sel in mSelectedAnnotations)
                    {
                        if (sel.CanBeCopied)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// returns true if there's at least 1 annotation in the selection that can be copied.
        /// </summary>
        /// <param name="downPoint">The position of the mouse in the viewers GetCanvas() space</param>
        /// <returns></returns>
        private bool CanAnnotationsBeDeleted(UIPoint downPoint)
        {
            if (mSelectedAnnotation != null)
            {
                if (mSelectedAnnotation.GetControlPoint(downPoint) >= 0)
                {
                    return true;
                }
            }
            else
            {
                SelectionHelper selection = IsPointInAnnotation(downPoint);
                if (selection != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Will check if the current selection is different from the previous one and will raise an event if this is the case.
        /// </summary>
        protected void HandleSelectionChangedEvent()
        {
            // create a list of current selections.
            List<SelectionHelper> newSelection = new List<SelectionHelper>();
            if (mSelectedAnnotation != null)
            {
                newSelection.Add(mSelectedAnnotation);
            }
            else
            {
                foreach (SelectionHelper selection in mSelectedAnnotations)
                {
                    newSelection.Add(new SelectionHelper(selection.Annot));
                }
            }


            bool selectionChanged = false;
            // Compare it to the old list.
            if (newSelection.Count != mPreviousSelection.Count)
            {
                selectionChanged = true;
            }
            if (newSelection.Count > 0 && mPreviousSelection.Count > 0)
            {
                // At this point, the count is the same, so we want to compare items.
                foreach (SelectionHelper selection in newSelection)
                {
                    if (!mPreviousSelection.Contains(selection))
                    {
                        selectionChanged = true;
                    }
                }
            }


            mPreviousSelection = newSelection;
            if (selectionChanged)
            {
                mToolManager.RaiseSelectedAnnotationsChangedEvent();
            }

        }

        #endregion Utility Functions




        #region Command Menu

        /// <summary>
        /// Adds a collection of Context Menu items to the context menu
        /// </summary>
        /// <param name="menu">The Context Menu</param>
        /// <param name="x">The x position relative to the PDFViewCtrl</param>
        /// <param name="y">The y position relative to the PDFViewCtrl</param>
        public override void AddContextMenuItems(ContextMenu menu, double x, double y)
        {
            mRightClickPageNumber = -1;
            try
            {
                mPDFView.DocLockRead();

                mRightClickPageNumber = mPDFView.GetPageNumberFromScreenPt(x, y);

                base.AddContextMenuItems(menu);
                MenuItem m;
                Separator sep = new Separator();
                menu.Items.Add(sep);

                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();

                UIPoint mousePoint = new UIPoint(x + sx, y + sy);

                bool canCopy = CanAnnotationsBeCopied(mousePoint);
                bool canDelete = CanAnnotationsBeDeleted(mousePoint);



                if (mRightClickPageNumber > 0)
                {
                    m = new MenuItem();
                    m.Header = "Select All";
                    m.Click += ContextMenu_SelectAllAnnots;
                    menu.Items.Add(m);
                }


                m = new MenuItem();
                m.Header = "Deselect All";
                m.Click += ContextMenu_DeselectAll;
                menu.Items.Add(m);

                sep = new Separator();
                menu.Items.Add(sep);

                if (canCopy && !mMultiPageSelection)
                {
                    m = new MenuItem();
                    m.Header = "Copy";
                    m.Click += ContextMenu_Copy;
                    menu.Items.Add(m);
                }

                if (!canCopy && mToolManager.CurrentCopiedAnnotations.Count() > 0)
                {
                    m = new MenuItem();
                    m.Header = "Paste";
                    m.Click += ContextMenu_Paste;
                    menu.Items.Add(m);
                }

                if (canDelete)
                {
                    m = new MenuItem();
                    m.Header = "Delete";
                    m.Click += ContextMenu_Delete;
                    menu.Items.Add(m);
                }




                if (mSelectedAnnotation != null)
                {
                    if (mSelectedAnnotation.GetControlPoint(mousePoint) >= 0)
                    {
                        sep = new Separator();
                        menu.Items.Add(sep);
                        mSelectedAnnotation.AddContextMenuItems(menu);
                    }
                }
                else
                {
                    SelectionHelper selection = IsPointInAnnotation(mousePoint);
                    if (selection != null)
                    {
                        sep = new Separator();
                        menu.Items.Add(sep);
                        selection.AddContextMenuItems(menu);

                        // To get a maximal set of shared properties for multi selection, you could create a
                        // selection helper derived class here and make it handle that.
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


        protected void ContextMenu_Paste(object sender, RoutedEventArgs e)
        {
            PasteAnnotations();
        }

        protected void ContextMenu_Copy(object sender, RoutedEventArgs e)
        {
            CopyAnnotations();
        }

        protected void ContextMenu_SelectAllAnnots(object sender, RoutedEventArgs e)
        {
            SelectAll();
        }

        protected void ContextMenu_DeselectAll(object sender, RoutedEventArgs e)
        {
            DeselectAll();
        }

        private void ContextMenu_Delete(object sender, RoutedEventArgs e)
        {
            DeleteAnnotations();
        }

        #endregion CommandMenu

        public override void SelectAll()
        {
            // clear current selection
            mSelectedAnnotations.Clear();

            // find the page number
            int pageNumber = mRightClickPageNumber;
            if (pageNumber <= 0)
            {
                pageNumber = mPDFView.GetCurrentPage();
            }

            // add every annot to the selection
            try
            {
                mPDFView.DocLockRead();
                PDFPage page = mPDFView.GetDoc().GetPage(pageNumber);

                int annotNum = page.GetNumAnnots();
                for (int i = 0; i < annotNum; i++)
                {
                    Annot annot = page.GetAnnot(i);

                    // make sure it's a type we can handle.
                    if (!annot.IsValid() || annot.GetFlag(Annot.Flag.e_hidden) ||
                        (annot.GetType() != Annot.Type.e_Link && annot.GetType() != Annot.Type.e_Widget && !annot.IsMarkup()))
                    {
                        continue;
                    }
                    else
                    {
                        mSelectedAnnotations.Add(new SelectionHelper(mPDFView, this, annot, pageNumber));
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
            ResolveSelectionAndDrawAppearnce();
        }

        public override void DeselectAll()
        {
            mSelectedAnnotation = null;
            mSelectedAnnotations.Clear();
            ResolveSelectionAndDrawAppearnce();
        }

        /// <summary>
        /// Will select the annotation and get ready to draw it.
        /// </summary>
        /// <param name="annotation"></param>
        /// <param name="pageNumber"></param>
        public void SelectAnnotation(Annot annotation, int pageNumber)
        {
            DeselectAll();
            AddSelection(annotation, pageNumber);
            ResolveSelectionAndDrawAppearnce();
            mEditingState = EditState.e_nothing;
        }

        public void SelectAnnotations(List<Tuple<Annot, int>> annotsWithPageNums)
        {
            DeselectAll();
            foreach (Tuple<Annot, int> annotWithPageNum in annotsWithPageNums)
            {
                AddIndividualAnnotToSelection(annotWithPageNum.Item1, annotWithPageNum.Item2);
            }
            ResolveSelectionAndDrawAppearnce();
            mEditingState = EditState.e_nothing;
        }

        private void AddIndividualAnnotToSelection(Annot annotation, int pageNumber)
        {
            try
            {
                mPDFView.DocLockRead();
                PDFDoc doc = mPDFView.GetDoc();

                if (doc != null)
                {
                    Page page = doc.GetPage(pageNumber);
                    int numAnnots = page.GetNumAnnots();
                    for (int i = 0; i < numAnnots; i++)
                    {
                        Annot annot = page.GetAnnot(i);
                        if (annot.GetSDFObj().IsEqual(annotation.GetSDFObj()))
                        {
                            mNewSelection.Add(new AnnotWithPageNum(annot, pageNumber));
                            return;
                        }
                    }
                }
            }
            catch (PDFNetException)
            {
            }
            finally
            {
                mPDFView.DocUnlockRead();
            }
        }

        public void AddSelection(Annot annotation, int pageNumber)
        {
            AddIndividualAnnotToSelection(annotation, pageNumber);
            ResolveSelectionAndDrawAppearnce();
        }

        public void RemoveSelection(Annot annot)
        {
            RemoveIndividualAnnotFromSelection(annot);
            ResolveSelectionAndDrawAppearnce();
        }

        private void RemoveIndividualAnnotFromSelection(Annot annot)
        {
            if (mSelectedAnnotation != null)
            {
                if (mSelectedAnnotation.Annot.GetSDFObj().IsEqual(annot.GetSDFObj()))
                {
                    mSelectedAnnotation = null;
                }
            }
            else if (mSelectedAnnotations.Count > 1)
            {
                int annotCount = mSelectedAnnotations.Count;
                for (int i = 0; i < annotCount; i++)
                {
                    if (mSelectedAnnotations[i].Annot.GetSDFObj().IsEqual(annot.GetSDFObj()))
                    {
                        mSelectedAnnotations.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public void DeleteSelectedAnnotations()
        {
            DeleteAnnotations();
        }
    }
}
    
