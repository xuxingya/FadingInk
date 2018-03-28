using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

using pdftron.PDF;

using UIPoint = System.Windows.Point;

using PDFRect = pdftron.PDF.Rect;


namespace pdftron.PDF.Tools
{
    public class TextStrikeoutCreate: TextMarkupCreate
    {
       
        public TextStrikeoutCreate(PDFViewWPF ctrl, ToolManager tMan)
            : base(ctrl, tMan)
        {
            mNextToolMode = ToolManager.ToolType.e_text_strikeout;
            mToolMode = ToolManager.ToolType.e_text_strikeout;

            mAnnotType = Annot.Type.e_StrikeOut;
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.TextMarkupColor;
            mTextMarkupOpacity = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupOpacity;
            mTextMarkupThickness = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupThickness;
            mTextMarkupBrush = new SolidColorBrush(Color.FromArgb((byte)(mTextMarkupOpacity * 255), col.R, col.G, col.B));
        }

        internal override void Draw(PDFRect drawRect)
        {
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_90 || mPageRotation == pdftron.PDF.Page.Rotate.e_270)
            {
                // Vertical strikeout
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, ((drawRect.x1 + drawRect.x2) / 2) - (mDrawThickness / 2));
                rect.SetValue(Canvas.TopProperty, drawRect.y1);
                rect.Width = mDrawThickness;
                rect.Height = drawRect.y2 - drawRect.y1;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
            else
            {
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, drawRect.x1);
                rect.SetValue(Canvas.TopProperty, ((drawRect.y1 + drawRect.y2) / 2) - (mDrawThickness / 2));
                rect.Width = drawRect.x2 - drawRect.x1;
                rect.Height = mDrawThickness;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
        }
    }
}