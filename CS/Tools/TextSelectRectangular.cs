using pdftron.PDF;

namespace pdftron.PDF.Tools
{

    public class TextSelectRectangular : TextSelectStructural
    {
        public TextSelectRectangular(PDFViewWPF view, ToolManager manager)
            : base(view, manager)
        {
            mPDFView.SetTextSelectionMode(PDFViewWPF.TextSelectionMode.e_rectangular);
            mToolMode = ToolManager.ToolType.e_text_select_rectangular;
            mNextToolMode = ToolManager.ToolType.e_text_select_rectangular;
        }

        internal override void MouseDoubleClickHandler(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.MouseDoubleClickHandler(sender, e);
            mPDFView.SetTextSelectionMode(PDFViewWPF.TextSelectionMode.e_rectangular);
        }
    }
}