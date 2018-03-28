using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pdftron.Common;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

using UIPoint = System.Windows.Point;
using UIImage = System.Windows.Controls.Image;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;





namespace pdftron.PDF.Tools
{

    public class StickyNoteCreate : Tool
    {
        protected const int PAGE_SPACE_ICON_WIDTH = 20;
        protected const int PAGE_SPACE_ICON_HEIGHT = 20;



        public StickyNoteCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mToolMode = ToolManager.ToolType.e_sticky_note_create;
            mNextToolMode = ToolManager.ToolType.e_sticky_note_create;
            DisallowTextSelection();
        }


        internal override void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ProcessInputDown(e);
        }

        public override void TouchDownHandler(object sender, System.Windows.Input.TouchEventArgs e)
        {
            base.TouchDownHandler(sender, e);
            ProcessInputDown(e);
            e.Handled = true;
        }

        private void ProcessInputDown(System.Windows.Input.InputEventArgs e)
        {
            UIPoint point = GetPosition(e, mPDFView);

            int pageNumber = mPDFView.GetPageNumberFromScreenPt(point.X, point.Y);


            if (pageNumber > 0)
            {
                double x = point.X;
                double y = point.Y;

                mPDFView.ConvScreenPtToPagePt(ref x, ref y, pageNumber);

                try
                {
                    mPDFView.DocLock(true);

                    PDFRect rect = new PDFRect(x, y, x + PAGE_SPACE_ICON_WIDTH, y + PAGE_SPACE_ICON_HEIGHT);

                    pdftron.PDF.Annots.Text text = pdftron.PDF.Annots.Text.Create(mPDFView.GetDoc().GetSDFDoc(), rect);
                    text.SetIcon(pdftron.PDF.Annots.Text.Icon.e_Comment);
                    text.SetColor(new ColorPt(255, 255, 0), 3);

                    pdftron.PDF.Annots.Popup pop = pdftron.PDF.Annots.Popup.Create(mPDFView.GetDoc().GetSDFDoc(), rect);
                    pop.SetParent(text);
                    text.SetPopup(pop);

                    text.RefreshAppearance();
                    PDFPage page = mPDFView.GetDoc().GetPage(pageNumber);


                    
                    page.AnnotPushBack(text);
                    page.AnnotPushBack(pop);

                    // required to make it appear upright in rotated documents
                    text.RefreshAppearance();


                    mPDFView.Update(text, pageNumber);

                    ConvertPageRectToCanvasRect(rect, pageNumber);

                    List<UIPoint> targetPoints = new List<UIPoint>();
                    targetPoints.Add(new UIPoint(rect.x1, rect.y1));
                    targetPoints.Add(new UIPoint(rect.x1, rect.y2));
                    targetPoints.Add(new UIPoint(rect.x2, rect.y1));
                    targetPoints.Add(new UIPoint(rect.x2, rect.y2));

                    mToolManager.NoteManager.OpenNote(new pdftron.PDF.Annots.Markup(text), pageNumber, targetPoints);

                    mAnnot = text;
                    mAnnotPageNum = pageNumber;
                }
                catch (System.Exception)
                { }
                finally
                {
                    mPDFView.DocUnlock();
                }

                mToolManager.RaiseAnnotationAddedEvent(mAnnot);
                mToolManager.DelayRemoveTimers.Add(new PDFViewWPFToolsCS2013.Utilities.DelayRemoveTimer(mViewerCanvas, this, this, pageNumber));
            }
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }
    }
}