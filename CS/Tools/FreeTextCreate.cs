using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using UIPoint = System.Windows.Point;
using UIRect = System.Windows.Rect;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;
using pdftron.SDF;




namespace pdftron.PDF.Tools
{
    public class FreeTextCreate : SimpleShapeCreate
    {
        protected System.Windows.Shapes.Path mShape;
        protected RectangleGeometry mRectangleGeometry;
        protected SolidColorBrush mTextBrush;
        protected double mFontSize;

        protected Border mTextPopupBorder;
        protected TextBox mTextBox;
        protected PDFRect mTextRect;
        protected bool mIsTextPopupOpen = false;
        protected bool mIsContentUpToDate = true;
        protected bool mCancelTextAnnot = false;

        protected bool mIsTextEdited = false;

        public FreeTextCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mNextToolMode = ToolManager.ToolType.e_text_annot_create;
            mToolMode = ToolManager.ToolType.e_text_annot_create;
            mViewerCanvas = mToolManager.AnnotationCanvas;
            START_DRAWING_THRESHOLD = 20;
        }

        internal override void OnCreate()
        {
            base.OnCreate();
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.TextLineColor;
            mUseStroke = col.Use;
            mStrokeBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

            col = pdftron.PDF.Tools.Utilities.ColorSettings.TextFillColor;
            mUseFill = col.Use;
            mFillBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

            col = pdftron.PDF.Tools.Utilities.ColorSettings.TextColor;
            mTextBrush = new SolidColorBrush(Color.FromArgb(255, col.R, col.G, col.B));

            mStrokeThickness = pdftron.PDF.Tools.Properties.Settings.Default.TextLineThickness;
            mOpacity = pdftron.PDF.Tools.Properties.Settings.Default.TextOpacity;
            mFontSize = pdftron.PDF.Tools.Properties.Settings.Default.FontSize;
            if (mFontSize <= 0)
            {
                mFontSize = 12;
            }
        }

        internal override void OnClose()
        {
            if (mIsTextPopupOpen)
            {
                //UpdateDoc();
                //mPDFView.Update(mAnnot, mDownPageNumber);
                CloseTextPopup();

            }
            base.OnClose();
        }

        internal override void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessInputDown(e);
            if (mNextToolMode == ToolManager.ToolType.e_text_annot_create)
            {
                base.MouseLeftButtonDownHandler(sender, e);
            }
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            Console.WriteLine("free text touch down");
            ProcessInputDown(e);
            if (mNextToolMode == ToolManager.ToolType.e_text_annot_create)
            {
                base.TouchDownHandler(sender, e);
            }
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            if (mIsTextPopupOpen)
            {
                mIsTextPopupOpen = false;
                //UpdateDoc();
                //mPDFView.Update(mAnnot, mDownPageNumber);
                CloseTextPopup();
                if (mUseSameToolWhenDone)
                {
                    Reset();
                }
                else
                {
                    mNextToolMode = ToolManager.ToolType.e_pan;
                }
            }
        }


        internal override void MouseLeftButtonUpHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            ProcessInputUp(e);
        }

        public override void TouchUpHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            base.TouchUpHandler(sender, e);
            ProcessInputUp(e);
        }
        
        private void ProcessInputUp(InputEventArgs e)
        {
            this.Children.Clear();

            if (mIsDrawing)
            {
                CreateTextPopup();
                CreateTextAnnot();
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

        protected override void Draw()
        {
            if (!mShapeHasBeenCreated)
            {
                mShapeHasBeenCreated = true;

                mRectangleGeometry = new RectangleGeometry();
                mShape = new Path();
                mShape.Data = mRectangleGeometry;
                if (mStrokeThickness > 0)
                {
                    mShape.Stroke = mStrokeBrush;
                }
                else
                {
                    mDrawThickness = 1;
                    mShape.Stroke = new SolidColorBrush(Colors.Black);
                    mShape.StrokeDashArray.Add(2);
                    mShape.StrokeDashArray.Add(2);
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

        protected void CreateTextPopup()
        {
            ShouldHandleKeyEvents = false;

            // rectangle in canvas space
            mTextRect = new PDFRect(mDownPoint.X, mDownPoint.Y, mDragPoint.X, mDragPoint.Y);
            mTextRect.Normalize();

            if (2 * mTextRect.Width() <= mDrawThickness || 2 * mTextRect.Height() <= mDrawThickness)
            {
                return;
            }

            // create the control to host the text canvas
            mTextPopupBorder = new Border();
            mTextPopupBorder.Width = mTextRect.Width();
            mTextPopupBorder.Height = mTextRect.Height();
            mTextPopupBorder.Opacity = mOpacity;
            mTextPopupBorder.BorderThickness = new System.Windows.Thickness(mDrawThickness);
            mTextPopupBorder.BorderBrush = mStrokeBrush;

            // create text box
            mTextBox = new TextBox();
            mTextBox.TextWrapping = System.Windows.TextWrapping.Wrap;
            mTextBox.AcceptsReturn = true;
            mTextBox.AcceptsTab = true;
            mTextBox.AutoWordSelection = true;

            mTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(TextBoxScrollChanged), true);

            mTextBox.Foreground = mTextBrush;
            if (mUseFill)
            {
                mTextBox.Background = mFillBrush;
            }
            else
            {
                mTextBox.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                mTextBox.CaretBrush = mTextBrush;
            }
            mTextBox.FontSize = mFontSize * mPDFView.GetZoom();
            mTextBox.FontFamily = new FontFamily("Arial");
            mTextBox.Text = "";
            mTextBox.Loaded += mTextBox_Loaded;
            mTextBox.KeyDown += mTextBox_KeyDown;

            mTextPopupBorder.SetValue(Canvas.LeftProperty, mTextRect.x1 - mPageCropOnClient.x1);
            mTextPopupBorder.SetValue(Canvas.TopProperty, mTextRect.y1 - mPageCropOnClient.y1);

            mTextPopupBorder.Child = mTextBox;
            this.Children.Add(mTextPopupBorder);
            mIsTextPopupOpen = true;
        }

        private void TextBoxScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true; // this will prevent the entire view from scrolling to accommodate the text box
        }

        void mTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                mCancelTextAnnot = true;
                CloseTextPopup();
            }
        }

        void mTextBox_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            mTextBox.Focus();
            mTextBox.TextChanged += mTextBox_TextChanged;
        }

        void mTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool locked = false;
            try
            {
                locked = mPDFView.DocTryLock();
                if (locked)
                {
                    mAnnot.SetContents(mTextBox.Text);
                    mAnnot.RefreshAppearance();
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

        private void CloseTextPopup()
        {
            ShouldHandleKeyEvents = true;
            if (mCancelTextAnnot)
            {
                PDFRect updateRect = new PDFRect();
                try
                {
                    mPDFView.DocLock(true);
                    updateRect = mAnnot.GetRect();
                    ConvertPageRectToScreenRect(updateRect, mDownPageNumber);
                    PDFPage page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                    page.AnnotRemove(mAnnot);
                }
                catch (System.Exception)
                {
                }
                finally
                {
                    mPDFView.DocUnlock();
                }
                mPDFView.Update(updateRect);
            }
            else if (mTextBox != null && mTextBox.Text.Trim().Length > 0)
            {

                try
                {
                    mPDFView.DocLock(true);
                    DateTime now = DateTime.Now;
                    Date date = new Date((short)now.Year, (byte)now.Month, (byte)now.Day, (byte)now.Hour, (byte)now.Minute, (byte)now.Second);
                    mAnnot.SetDate(date);
                    if (!mIsContentUpToDate)
                    {
                        mAnnot.SetContents(mTextBox.Text);
                        mAnnot.RefreshAppearance();
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

                mPDFView.Update(mAnnot, mDownPageNumber);
            }
            if (mCancelTextAnnot)
            {
                if (mTextPopupBorder != null)
                {
                    if (this.Children.Contains(mTextPopupBorder))
                    {
                        this.Children.Remove(mTextPopupBorder);
                    }
                    mTextPopupBorder = null;
                    mTextBox = null;
                }
            }
            else
            {
                mToolManager.DelayRemoveTimers.Add(new PDFViewWPFToolsCS2013.Utilities.DelayRemoveTimer(mViewerCanvas, this, this, mDownPageNumber));
            }
        }

        /// <summary>
        /// Update the document with the new text
        /// </summary>
        protected void UpdateDoc()
        {
            if (mTextBox == null || mTextBox.Text.Trim().Length == 0)
            {
                return;
            }


            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            double xPos = mTextRect.x1 - sx;
            double yPos = mTextRect.y1 - sy;

            double x1 = mTextRect.x1 - sx;
            double y1 = mTextRect.y1 - sy;
            double x2 = mTextRect.x2 - sx;
            double y2 = mTextRect.y2 - sy;

            // we need to give the text box a minimum size, to make sure that at least some text fits in it.
            double marg = 50;
            if (x2 - x1 < marg)
            {
                x2 = x1 + marg;
            }
            if (y2 - y1 < marg)
            {
                y2 = y1 + marg;
            }


            mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mDownPageNumber);
            mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mDownPageNumber);

            try
            {
                mPDFView.DocLock(true);
                pdftron.PDF.Rect rect;
                pdftron.PDF.Page.Rotate pr = mPDFView.GetDoc().GetPage(mDownPageNumber).GetRotation();
                double xDist, yDist;
                if (pr == pdftron.PDF.Page.Rotate.e_90 || pr == pdftron.PDF.Page.Rotate.e_270)
                {
                    xDist = Math.Abs(y1 - y2);
                    yDist = Math.Abs(x1 - x2);
                }
                else
                {
                    xDist = Math.Abs(x1 - x2);
                    yDist = Math.Abs(y1 - y2);
                }
                //rect = new pdftron.PDF.Rect(x1, y1, x1 + xDist, y1 + yDist);
                rect = new pdftron.PDF.Rect(x1, y1, x2, y2);
                pdftron.PDF.Page page4rect = mPDFView.GetDoc().GetPage(mDownPageNumber);

                pdftron.PDF.Annots.FreeText textAnnot = pdftron.PDF.Annots.FreeText.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                // set text color
                double red = mTextBrush.Color.R / 255.0;
                double green = mTextBrush.Color.G / 255.0;
                double blue = mTextBrush.Color.B / 255.0;
                ColorPt color = new ColorPt(red, green, blue);
                textAnnot.SetTextColor(color, 3);

                // Set background color if necessary
                if (mUseFill)
                {
                    red = mFillBrush.Color.R;
                    green = mFillBrush.Color.G;
                    blue = mFillBrush.Color.B;
                    color = new ColorPt(red / 255.0, green / 255.0, blue / 255.0);
                    textAnnot.SetColor(color, 3);
                }

                Annot.BorderStyle bs = textAnnot.GetBorderStyle();
                bs.width = mStrokeThickness;
                textAnnot.SetBorderStyle(bs);
                red = mStrokeBrush.Color.R;
                green = mStrokeBrush.Color.G;
                blue = mStrokeBrush.Color.B;
                color = new ColorPt(red / 255.0, green / 255.0, blue / 255.0);
                textAnnot.SetLineColor(color, 3);

                // set appearance and contents
                textAnnot.SetOpacity(mOpacity);
                textAnnot.SetFontSize(mFontSize);
                string outString = mTextBox.Text;

                //mTextBox.Document.GetText(Windows.UI.Text.TextGetOptions.UseCrlf, out outString);
                textAnnot.SetContents(outString);

                textAnnot.RefreshAppearance(); // else we can't get content stream


                // TODO: Get rid of this once confirmed to work well!
                // Get the annotation's content stream 
                Obj firstObj = textAnnot.GetSDFObj();
                Obj secondObj = firstObj.FindObj("AP");
                Obj contentStream = secondObj.FindObj("N");

                // use element reader to iterate through elements and union their bounding boxes
                ElementReader er = new ElementReader();
                er.Begin(contentStream);
                Rect unionRect = new Rect();
                unionRect.Set(rect);

                Rect r = new Rect();
                Element element = er.Next();
                while (element != null)
                {
                    if (element.GetBBox(r))
                    {
                        if (element.GetType() == Element.Type.e_text)
                        {
                            if (unionRect == null)
                            {
                                unionRect = r;
                            }
                            unionRect = GetRectUnion(r, unionRect);
                        }
                    }

                    element = er.Next();
                }
                unionRect.y1 -= 25;
                unionRect.x2 += 25;

                // Move annotation back into position
                x1 = unionRect.x1;
                y1 = unionRect.y1;
                x2 = unionRect.x2;
                y2 = unionRect.y2;
                mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, mDownPageNumber);
                mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, mDownPageNumber);

                pr = mPDFView.GetDoc().GetPage(mDownPageNumber).GetRotation();
                if (pr == pdftron.PDF.Page.Rotate.e_90 || pr == pdftron.PDF.Page.Rotate.e_270)
                {
                    xDist = Math.Abs(y1 - y2);
                    yDist = Math.Abs(x1 - x2);
                }
                else
                {
                    xDist = Math.Abs(x1 - x2);
                    yDist = Math.Abs(y1 - y2);
                }
                x1 = xPos;
                y1 = yPos;
                x2 = xPos + xDist;
                y2 = yPos + yDist;
                mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mDownPageNumber);
                mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mDownPageNumber);
                rect = new pdftron.PDF.Rect(x1, y1, x2, y2);

                //textAnnot.Resize(rect);
                //textAnnot.RefreshAppearance();
                pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                page.AnnotPushBack(textAnnot);
                textAnnot.RefreshAppearance();

                mAnnot = textAnnot;

                mPDFView.Update(textAnnot, mDownPageNumber);
            }
            catch (Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }
        }

        protected void CreateTextAnnot()
        {
            double sx = mPDFView.GetHScrollPos();
            double sy = mPDFView.GetVScrollPos();

            double xPos = mTextRect.x1 - sx;
            double yPos = mTextRect.y1 - sy;

            double x1 = mTextRect.x1 - sx;
            double y1 = mTextRect.y1 - sy;
            double x2 = mTextRect.x2 - sx;
            double y2 = mTextRect.y2 - sy;

            // we need to give the text box a minimum size, to make sure that at least some text fits in it.
            double marg = 50;
            if (x2 - x1 < marg)
            {
                x2 = x1 + marg;
            }
            if (y2 - y1 < marg)
            {
                y2 = y1 + marg;
            }

            mPDFView.ConvScreenPtToPagePt(ref x1, ref y1, mDownPageNumber);
            mPDFView.ConvScreenPtToPagePt(ref x2, ref y2, mDownPageNumber);

            try
            {
                mPDFView.DocLock(true);
                pdftron.PDF.Rect rect;
                pdftron.PDF.Page.Rotate pr = mPDFView.GetDoc().GetPage(mDownPageNumber).GetRotation();
                double xDist, yDist;
                if (pr == pdftron.PDF.Page.Rotate.e_90 || pr == pdftron.PDF.Page.Rotate.e_270)
                {
                    xDist = Math.Abs(y1 - y2);
                    yDist = Math.Abs(x1 - x2);
                }
                else
                {
                    xDist = Math.Abs(x1 - x2);
                    yDist = Math.Abs(y1 - y2);
                }
                //rect = new pdftron.PDF.Rect(x1, y1, x1 + xDist, y1 + yDist);
                rect = new pdftron.PDF.Rect(x1, y1, x2, y2);
                pdftron.PDF.Page page4rect = mPDFView.GetDoc().GetPage(mDownPageNumber);

                pdftron.PDF.Annots.FreeText textAnnot = pdftron.PDF.Annots.FreeText.Create(mPDFView.GetDoc().GetSDFDoc(), rect);

                // set text color
                double red = mTextBrush.Color.R / 255.0;
                double green = mTextBrush.Color.G / 255.0;
                double blue = mTextBrush.Color.B / 255.0;
                ColorPt color = new ColorPt(red, green, blue);
                textAnnot.SetTextColor(color, 3);

                // Set background color if necessary
                if (mUseFill)
                {
                    red = mFillBrush.Color.R;
                    green = mFillBrush.Color.G;
                    blue = mFillBrush.Color.B;
                    color = new ColorPt(red / 255.0, green / 255.0, blue / 255.0);
                    textAnnot.SetColor(color, 3);
                }

                Annot.BorderStyle bs = textAnnot.GetBorderStyle();
                bs.width = mStrokeThickness;
                textAnnot.SetBorderStyle(bs);
                red = mStrokeBrush.Color.R;
                green = mStrokeBrush.Color.G;
                blue = mStrokeBrush.Color.B;
                color = new ColorPt(red / 255.0, green / 255.0, blue / 255.0);
                textAnnot.SetLineColor(color, 3);

                // set appearance and contents
                textAnnot.SetOpacity(mOpacity);
                textAnnot.SetFontSize(mFontSize);

                pdftron.PDF.Page page = mPDFView.GetDoc().GetPage(mDownPageNumber);
                page.AnnotPushBack(textAnnot);
                textAnnot.RefreshAppearance();

                mAnnot = textAnnot;
            }
            catch (Exception)
            {
            }
            finally
            {
                mPDFView.DocUnlock();
            }
        }

        protected Rect GetRectUnion(Rect r1, Rect r2)
        {
            Rect rectUnion = new Rect();
            rectUnion.x1 = Math.Min(r1.x1, r2.x1);
            rectUnion.y1 = Math.Min(r1.y1, r2.y1);
            rectUnion.x2 = Math.Max(r1.x2, r2.x2);
            rectUnion.y2 = Math.Max(r1.y2, r2.y2);
            return rectUnion;
        }

        protected void Reset()
        {
            mIsDrawing = false;
            mShapeHasBeenCreated = false;
            mCancelTextAnnot = false;
            mViewerCanvas.Children.Remove(this);
            CloseTextPopup();
        }

    }
}