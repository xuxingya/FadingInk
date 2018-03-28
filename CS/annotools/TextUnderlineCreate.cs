using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

using pdftron.PDF;

using UIPoint = System.Windows.Point;

using PDFRect = pdftron.PDF.Rect;


namespace pdftron.PDF.Tools
{
    public class TextUnderlineCreate: TextMarkupCreate
    {
       
        public TextUnderlineCreate(PDFViewWPF ctrl, ToolManager tMan)
            : base(ctrl, tMan)
        {
            mNextToolMode = ToolManager.ToolType.e_text_underline;
            mToolMode = ToolManager.ToolType.e_text_underline;

            mAnnotType = Annot.Type.e_Underline;
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.TextMarkupColor;
            mTextMarkupOpacity = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupOpacity;
            mTextMarkupThickness = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupThickness;
            mTextMarkupBrush = new SolidColorBrush(Color.FromArgb((byte)(mTextMarkupOpacity * 255), col.R, col.G, col.B));
        }

        internal override void Draw(PDFRect drawRect)
        {
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_0)
            {
                // underline at bottom
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, drawRect.x1);

                rect.SetValue(Canvas.TopProperty, drawRect.y2 - (mDrawThickness / 2));
                rect.Width = drawRect.x2 - drawRect.x1;
                rect.Height = mDrawThickness;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_90)
            {
                // underline to the left
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, drawRect.x1);
                rect.SetValue(Canvas.TopProperty, drawRect.y1);
                rect.Width = mDrawThickness;
                rect.Height = drawRect.y2 - drawRect.y1;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_180)
            {
                // underline at the top
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, drawRect.x1);
                rect.SetValue(Canvas.TopProperty, drawRect.y1);
                rect.Width = drawRect.x2 - drawRect.x1;
                rect.Height = mDrawThickness;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_270)
            {
                // underlien to the right
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, drawRect.x2 - mDrawThickness);
                rect.SetValue(Canvas.TopProperty, drawRect.y1);
                rect.Width = mDrawThickness;
                rect.Height = drawRect.y2 - drawRect.y1;
                rect.Fill = mTextMarkupBrush;
                this.Children.Add(rect);
            }
        }
    }
}