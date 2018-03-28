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
using pdftron.PDF.Annots;


namespace pdftron.PDF.Tools
{

    public class TextMarkupCreate : Tool
    {
        protected UIPoint mDownPoint;
        protected UIPoint mDragPoint;
        protected int mDownPageNumber = -1;

        protected Annot.Type mAnnotType;
        protected pdftron.PDF.Page.Rotate mPageRotation;
        protected SolidColorBrush mTextMarkupBrush = new SolidColorBrush(Color.FromArgb(100, 80, 110, 200));
        protected double mTextMarkupOpacity = 1;
        protected double mTextMarkupThickness = 1;
        protected double mDrawThickness = 1;

        protected bool mTouchScrolling = false;

        public TextMarkupCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mViewerCanvas = mToolManager.AnnotationCanvas;
            mPDFView.SetTextSelectionMode(PDFViewWPF.TextSelectionMode.e_structural);
            mViewerCanvas.Children.Add(this);
            mPDFView.Cursor = System.Windows.Input.Cursors.IBeam;
            DisallowTextSelection();
        }

        internal override void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonDownHandler(sender, e);
            ProcessInputDown(e);
            mViewerCanvas.CaptureMouse();
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            base.TouchDownHandler(sender, e);
            if (mToolManager.TouchIDs.Count > 1 && !mTouchScrolling)
            {
                EndCurrentTool(ToolManager.ToolType.e_pan);
                e.Handled = true;
            }
            ProcessInputDown(e);
            mTouchScrolling = false;
            UIPoint screenPoint = GetPosition(e, mPDFView);
            if (mPDFView.GetPageNumberFromScreenPt(screenPoint.X, screenPoint.Y) == -1)
            {
                mTouchScrolling = true;
            }
            else
            {
                e.Handled = true;
            }
            mViewerCanvas.CaptureTouch(e.TouchDevice);
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            mDownPoint = GetPosition(e, mViewerCanvas);
        }

        internal override void MouseMovedHandler(object sender, System.Windows.Input.MouseEventArgs e)
        {
            base.MouseMovedHandler(sender, e);
            if (!mIsDragging)
            {
                return;
            }
            ProcessInputMove(e);
        }

        public override void TouchMoveHandler(object sender, TouchEventArgs e)
        {
            base.TouchMoveHandler(sender, e);
            if (!mTouchScrolling)
            {
                ProcessInputMove(e);
            }
        }

        private void ProcessInputMove(InputEventArgs e)
        {
            mDragPoint = GetPosition(e, mViewerCanvas);

            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();
            mPDFView.ClearSelection();
            mPDFView.Select(mDownPoint.X - sx, mDownPoint.Y - sy, mDragPoint.X - sx, mDragPoint.Y - sy);
            DrawSelection();
        }

        internal override void MouseLeftButtonUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            ProcessInputUp(e);
            mViewerCanvas.ReleaseMouseCapture();
        }

        public override void TouchUpHandler(object sender, TouchEventArgs e)
        {
            base.TouchUpHandler(sender, e);
            if (!mTouchScrolling)
            {
                ProcessInputUp(e);
            }
            mViewerCanvas.ReleaseTouchCapture(e.TouchDevice);
        }

        private void ProcessInputUp(InputEventArgs e)
        {
            CreateTextMarkup();
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }

        internal override void PreviewMouseWheelHandler(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (mIsDragging && mIsCtrlDown)
            {
                e.Handled = true;
            }
            else
            {
                base.PreviewMouseWheelHandler(sender, e);
            }
        }

        protected void DrawSelection()
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();
            mSelectionStartPage = mPDFView.GetSelectionBeginPage();
            mSelectionEndPage = mPDFView.GetSelectionEndPage();
            this.Children.Clear();

            mDrawThickness = mTextMarkupThickness * mPDFView.GetZoom();

            pdftron.PDF.Page.Rotate viewerRotation = mPDFView.GetRotation();

            for (int pgnm = mSelectionStartPage; pgnm <= mSelectionEndPage; pgnm++)
            {
                PDFViewWPF.Selection sel = mPDFView.GetSelection(pgnm);

                // figure out how much the page is rotated (viewer + page rotation)
                if (!mPDFView.HasSelectionOnPage(pgnm))
                {
                    continue;
                }

                try
                {
                    mPageRotation = pdftron.PDF.Page.Rotate.e_0;
                    mPDFView.DocLockRead();
                    mPageRotation = mPDFView.GetDoc().GetPage(pgnm).GetRotation();
                }
                catch (System.Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlockRead();
                }
                mPageRotation = (pdftron.PDF.Page.Rotate)(((int)viewerRotation + (int)mPageRotation) % 4);                

                double[] quads = sel.GetQuads();
                int sz = quads.Length / 8;

                int k = 0;
                PDFRect drawRect;
                double xpt = 0; // for translating coordinates
                double ypt = 0;

                // each quad consists of 8 consecutive points
                for (int i = 0; i < sz; ++i, k += 8)
                {
                    drawRect = new PDFRect();

                    // Get first corner of selection quad
                    xpt = quads[k];
                    ypt = quads[k + 1];
                    mPDFView.ConvPagePtToScreenPt(ref xpt, ref ypt, pgnm);
                    drawRect.x1 = xpt + sx;
                    drawRect.y1 = ypt + sy;

                    // Get opposite corner of selection quad
                    xpt = quads[k + 4];
                    ypt = quads[k + 5];
                    mPDFView.ConvPagePtToScreenPt(ref xpt, ref ypt, pgnm);
                    drawRect.x2 = xpt + sx;
                    drawRect.y2 = ypt + sy;

                    drawRect.Normalize();

                    Draw(drawRect);
                }
            }
        }

        /// <summary>
        /// Derived classes should override this if they want a more interesting shape than a blue rectangle
        /// </summary>
        /// <param name="drawRect"></param>
        internal virtual void Draw(PDFRect drawRect)
        {
            Rectangle rect = new Rectangle();
            rect.SetValue(Canvas.LeftProperty, drawRect.x1);
            rect.SetValue(Canvas.TopProperty, drawRect.y1);
            rect.Width = drawRect.x2 - drawRect.x1;
            rect.Height = drawRect.y2 - drawRect.y1;
            rect.Fill = new SolidColorBrush(Color.FromArgb(100, 80, 110, 200));
            this.Children.Add(rect);
        }

        private void CreateTextMarkup()
        {
            // Store created markups by page, so that we can request all updates after we have created them all
            Dictionary<int, TextMarkup> textMarkupsToUpdate = new Dictionary<int, TextMarkup>();
            try
            {
                PDFDoc doc = mPDFView.GetDoc();
                mPDFView.DocLock(true);

                mSelectionStartPage = mPDFView.GetSelectionBeginPage();
                mSelectionEndPage = mPDFView.GetSelectionEndPage();

                

                for (int pgnm = mSelectionStartPage; pgnm <= mSelectionEndPage; pgnm++)
                {
                    if (!mPDFView.HasSelectionOnPage(pgnm))
                    {
                        continue;
                    }

                    double[] quads = mPDFView.GetSelection(pgnm).GetQuads();
                    int sz = quads.Length / 8;
                    if (sz == 0)
                    {
                        continue;
                    }

                    // for translating points
                    PDFPoint p1 = new PDFPoint();
                    PDFPoint p2 = new PDFPoint();
                    PDFPoint p3 = new PDFPoint();
                    PDFPoint p4 = new PDFPoint();


                    QuadPoint qp = new QuadPoint(p1, p2, p3, p4);
                    PDFRect bbox = new PDFRect(quads[0], quads[1], quads[4], quads[5]); //just use the first quad to temporarily populate the bbox
                    TextMarkup tm;

                    if (mAnnotType == Annot.Type.e_Highlight)
                    {
                        tm = Highlight.Create(doc.GetSDFDoc(), bbox);
                    }
                    else // Underline, Strikeout, and Squiggly share color and opacity settings
                    {
                        // figure out markup type
                        if (mAnnotType == Annot.Type.e_Underline)
                        {
                            tm = Underline.Create(doc.GetSDFDoc(), bbox);
                        }
                        else if (mAnnotType == Annot.Type.e_StrikeOut)
                        {
                            tm = StrikeOut.Create(doc.GetSDFDoc(), bbox);
                        }
                        else // squiggly
                        {
                            tm = Squiggly.Create(doc.GetSDFDoc(), bbox);
                        }
                    }

                    // Add the quads
                    int k = 0;
                    for (int i = 0; i < sz; ++i, k += 8)
                    {
                        p1.x = quads[k];
                        p1.y = quads[k + 1];

                        p2.x = quads[k + 2];
                        p2.y = quads[k + 3];

                        p3.x = quads[k + 4];
                        p3.y = quads[k + 5];

                        p4.x = quads[k + 6];
                        p4.y = quads[k + 7];

                        qp.p1 = p1;
                        qp.p2 = p2;
                        qp.p3 = p3;
                        qp.p4 = p4;

                        tm.SetQuadPoint(i, qp);
                    }

                    // set color and opacity
                    ColorPt color = new ColorPt(mTextMarkupBrush.Color.R / 255.0, mTextMarkupBrush.Color.G / 255.0, mTextMarkupBrush.Color.B / 255.0);
                    tm.SetColor(color, 3);
                    tm.SetOpacity(mTextMarkupOpacity);
                    pdftron.PDF.Annot.BorderStyle bStyle = tm.GetBorderStyle();
                    bStyle.width = mTextMarkupThickness;
                    tm.SetBorderStyle(bStyle);
                    tm.RefreshAppearance();

                    PDFPage page = doc.GetPage(pgnm);
                    page.AnnotPushBack(tm);

                    // add markup to dictionary for later update
                    textMarkupsToUpdate[pgnm] = tm;

                }

                // clear selection
                mPDFView.ClearSelection();
                //DrawSelection();
            }
            catch (System.Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }



            // Now update the PDFViewCtrl to display our new text markup
            foreach (int pgnm in textMarkupsToUpdate.Keys)
            {
                mPDFView.Update(textMarkupsToUpdate[pgnm], pgnm);
                mToolManager.RaiseAnnotationAddedEvent(textMarkupsToUpdate[pgnm]);
                mToolManager.DelayRemoveTimers.Add(new PDFViewWPFToolsCS2013.Utilities.DelayRemoveTimer(mViewerCanvas, this, this, pgnm));
            }
        }

    }
}