using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

using pdftron.PDF;

using UIPoint = System.Windows.Point;

using PDFRect = pdftron.PDF.Rect;



namespace pdftron.PDF.Tools
{
    public class TextHightlightCreate: TextMarkupCreate
    {
       
        public TextHightlightCreate(PDFViewWPF ctrl, ToolManager tMan)
            : base(ctrl, tMan)
        {
            mNextToolMode = ToolManager.ToolType.e_text_highlight;
            mToolMode = ToolManager.ToolType.e_text_highlight;

            mAnnotType = Annot.Type.e_Highlight;
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.HighlightColor;
            mTextMarkupOpacity = pdftron.PDF.Tools.Properties.Settings.Default.HighlightOpacity;
            mTextMarkupBrush = new SolidColorBrush(Color.FromArgb((byte)(mTextMarkupOpacity * 125), col.R, col.G, col.B));
        }

        internal override void Draw(PDFRect drawRect)
        {
            // In version 6.0, the PDFViewWPF highlights selected text automatically (while it is selected).
            // In the future, this will be disabled. At that point, this code will draw highlights using the 
            // current highlight color, so that it matches the annotation that we will push back when done.

            Rectangle rect = new Rectangle();
            rect.IsHitTestVisible = false;
            rect.SetValue(Canvas.LeftProperty, drawRect.x1);
            rect.SetValue(Canvas.TopProperty, drawRect.y1);
            rect.Width = drawRect.x2 - drawRect.x1;
            rect.Height = drawRect.y2 - drawRect.y1;
            rect.Fill = mTextMarkupBrush;
            this.Children.Add(rect);
        }
    }
}
