using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pdftron.PDF;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

using UIPoint = System.Windows.Point;
using PDFRect = pdftron.PDF.Rect;

using PDFAction = pdftron.PDF.Action;

namespace pdftron.PDF.Tools
{
    class LinkAction : Tool
    {
        private pdftron.PDF.Annots.Link mLink;
        private PDFRect mPageCropOnClient;
        private List<PDFRect> mLinkRectangles = new List<PDFRect>();
        private bool mInsideRect = false;
        private double mMouseOverOpacity = 0.7;
        private double mMouseAwayOpacity = 0.3;



        public LinkAction(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mNextToolMode = ToolManager.ToolType.e_link_action;
            mToolMode = ToolManager.ToolType.e_link_action;
        }

        internal override void OnCreate()
        {
            mLink = new pdftron.PDF.Annots.Link(mAnnot);
        }

        internal override void MouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseLeftButtonDownHandler(sender, e);
            mViewerCanvas = mToolManager.AnnotationCanvas;
            mViewerCanvas.CaptureMouse();

            DrawRectangles();
            mInsideRect = true;
            this.Opacity = mMouseOverOpacity;
        }

        internal override void MouseMovedHandler(object sender, MouseEventArgs e)
        {
            UIPoint pointInsideTool = e.GetPosition(this);
            mInsideRect = false;
            foreach (PDFRect rect in mLinkRectangles)
            {
                if (rect.Contains(pointInsideTool.X, pointInsideTool.Y))
                {
                    mInsideRect = true;
                }
            }

            if (mInsideRect)
            {
                this.Opacity = mMouseOverOpacity;
            }
            else
            {
                this.Opacity = mMouseAwayOpacity;
            }
        }

        internal override void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            base.MouseLeftButtonUpHandler(sender, e);
            if (mInsideRect)
            {
                JumpToLink();
            }
            mNextToolMode = ToolManager.ToolType.e_pan;
        }

        private void JumpToLink()
        {
            if (mLink != null)
            {
                PDFAction a;
                try
                {
                    mPDFView.DocLockRead();
                    a = mLink.GetAction();
                    if (a != null)
                    {
                        PDFAction.Type at = a.GetType();
                        if (at == PDFAction.Type.e_URI)
                        {
                            pdftron.SDF.Obj o = a.GetSDFObj();
                            o = o.FindObj("URI");
                            if (o != null)
                            {
                                String uristring = o.GetAsPDFText();
                                if (uristring != null)
                                {
                                    System.Text.RegularExpressions.Regex emailExpression = new System.Text.RegularExpressions.Regex("^([a-zA-Z0-9_\\-\\.]+)@((\\[[0-9]{1,3}" + "\\.[0-9]{1,3}\\.[0-9]{1,3}\\.)|(([a-zA-Z0-9\\-]+\\" + ".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\\]?)$");
                                    if (emailExpression.IsMatch(uristring))
                                    {
                                        if (!uristring.StartsWith("mailto:"))
                                        {
                                            uristring = uristring.Insert(0, "mailto:");
                                        }
                                    }
                                    System.Diagnostics.Process.Start(uristring);
                                }
                            }
                        }
                        else if (at == PDFAction.Type.e_GoTo)
                        {
                            mPDFView.ExecuteAction(a);
                        }
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
        }

        /// <summary>
        /// Draws a rectangle for each quad
        /// </summary>
        protected void DrawRectangles()
        {
            try
            {
                // place canvas on page
                mPageCropOnClient = BuildPageBoundBoxOnClient(mAnnotPageNum);
                this.SetValue(Canvas.LeftProperty, mPageCropOnClient.x1);
                this.SetValue(Canvas.TopProperty, mPageCropOnClient.y1);
                this.Width = Math.Abs(mPageCropOnClient.Width());
                this.Height = Math.Abs(mPageCropOnClient.Height());

                mViewerCanvas.Children.Add(this);

                int qn = mLink.GetQuadPointCount();
                double sx = mPDFView.GetHScrollPos();
                double sy = mPDFView.GetVScrollPos();
                for (int i = 0; i < qn; ++i)
                {
                    // convert quad point to screen space
                    QuadPoint qp = mLink.GetQuadPoint(i);
                    PDFRect quadRect = new PDFRect();

                    double x1 = Math.Min(Math.Min(Math.Min(qp.p1.x, qp.p2.x), qp.p3.x), qp.p4.x);
                    double y2 = Math.Min(Math.Min(Math.Min(qp.p1.y, qp.p2.y), qp.p3.y), qp.p4.y);
                    double x2 = Math.Max(Math.Max(Math.Max(qp.p1.x, qp.p2.x), qp.p3.x), qp.p4.x);
                    double y1 = Math.Max(Math.Max(Math.Max(qp.p1.y, qp.p2.y), qp.p3.y), qp.p4.y);
                    mPDFView.ConvPagePtToScreenPt(ref x1, ref y1, mAnnotPageNum);
                    quadRect.x1 = x1 + sx - mPageCropOnClient.x1;
                    quadRect.y1 = y1 + sy - mPageCropOnClient.y1;
                    mPDFView.ConvPagePtToScreenPt(ref x2, ref y2, mAnnotPageNum);
                    quadRect.x2 = x2 + sx - mPageCropOnClient.x1;
                    quadRect.y2 = y2 + sy - mPageCropOnClient.y1;
                    quadRect.Normalize();

                    Rectangle drawRect = new Rectangle();
                    drawRect.Fill = new SolidColorBrush(Color.FromArgb(255, 130, 90, 220));
                    drawRect.SetValue(Canvas.LeftProperty, quadRect.x1);
                    drawRect.SetValue(Canvas.TopProperty, quadRect.y1);
                    drawRect.Width = quadRect.x2 - quadRect.x1;
                    drawRect.Height = quadRect.y2 - quadRect.y1;
                    this.Children.Add(drawRect);

                    mLinkRectangles.Add(quadRect);

                }
            }
            catch (Exception) { }
        }
    }
}
