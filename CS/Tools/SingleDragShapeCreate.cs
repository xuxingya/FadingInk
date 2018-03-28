using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using UIPoint = System.Windows.Point;
using PDFPoint = pdftron.PDF.Point;
using PDFRect = pdftron.PDF.Rect;
using PDFPage = pdftron.PDF.Page;
using pdftron.PDF;
using pdftron.Common;

namespace pdftron.PDF.Tools
{

    public class SingleDragShapeCreate : SimpleShapeCreate
    {

        public SingleDragShapeCreate(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
        }

        internal override void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            base.MouseLeftButtonDownHandler(sender, e);
            ProcessInputDown(e);
        }

        public override void TouchDownHandler(object sender, TouchEventArgs e)
        {
            base.TouchDownHandler(sender, e);
            ProcessInputDown(e);
        }

        private void ProcessInputDown(InputEventArgs e)
        {
            if (mDownPageNumber < 0)
            {
                EndCurrentTool(ToolManager.ToolType.e_pan);
            }
        }

        internal override void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
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
            if (mIsDrawing)
            {
                Create();
                mToolManager.DelayRemoveTimers.Add(new PDFViewWPFToolsCS2013.Utilities.DelayRemoveTimer(mViewerCanvas, this, this, mDownPageNumber));
            }
            else
            {
                mViewerCanvas.Children.Remove(this);
            }
            EndCurrentTool(ToolManager.ToolType.e_pan);
        }

    }

}