using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;

using pdftron.PDF;

using UIPoint = System.Windows.Point;

using PDFRect = pdftron.PDF.Rect;


namespace pdftron.PDF.Tools
{
    public class TextSquigglyCreate: TextMarkupCreate
    {
       
        public TextSquigglyCreate(PDFViewWPF ctrl, ToolManager tMan)
            : base(ctrl, tMan)
        {
            mNextToolMode = ToolManager.ToolType.e_text_squiggly;
            mToolMode = ToolManager.ToolType.e_text_squiggly;

            mAnnotType = Annot.Type.e_Squiggly;
            pdftron.PDF.Tools.Utilities.ColorSettings.ToolColor col = pdftron.PDF.Tools.Utilities.ColorSettings.TextMarkupColor;
            mTextMarkupOpacity = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupOpacity;
            mTextMarkupThickness = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupThickness;
            mTextMarkupBrush = new SolidColorBrush(Color.FromArgb((byte)(mTextMarkupOpacity * 255), col.R, col.G, col.B));
        }

        internal override void Draw(PDFRect drawRect)
        {

            Path squigglyShape = new System.Windows.Shapes.Path();
            PathGeometry pathGeometry = new PathGeometry();
            squigglyShape.StrokeLineJoin = PenLineJoin.Round;
            
            squigglyShape.StrokeThickness = mDrawThickness;
            squigglyShape.Stroke = mTextMarkupBrush;
            this.Children.Add(squigglyShape);

            double stepLength = mPDFView.GetZoom() * 2;
            double wiggleHeight = mPDFView.GetZoom() * 2;
            int numSteps = 0;

            double xStart = 0;
            double yStart = 0;
            double xStep = 0;
            double yStep = 0;
            int xAlternate = 1;
            int yAlternate = 1;

            UIPoint pathPoint = new UIPoint();

            // set up start condition, steps, and so on.

            if (mPageRotation == pdftron.PDF.Page.Rotate.e_0)
            {
                // Squiggly at bottom
                xStart = drawRect.x1;
                xStep = stepLength;
                yStep = wiggleHeight;
                yStart = drawRect.y2 - ((wiggleHeight) / 2);
                xAlternate = 1;
                yAlternate = -1;
                numSteps = (int)((drawRect.x2 - drawRect.x1) / stepLength);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_90)
            {
                // Squiggly to the left
                xStart = drawRect.x1 + ((wiggleHeight) / 2);
                xStep = -wiggleHeight;
                yStep = stepLength; 
                yStart = drawRect.y1;
                xAlternate = -1;
                yAlternate = 1;
                numSteps = (int)((drawRect.y2 - drawRect.y1) / stepLength);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_180)
            {
                // Squiggly at the top
                xStart = drawRect.x2;
                xStep = -stepLength;
                yStep = -wiggleHeight;
                yStart = drawRect.y1 + ((wiggleHeight) / 2);
                xAlternate = 1;
                yAlternate = -1;
                numSteps = (int)((drawRect.x2 - drawRect.x1) / stepLength);
            }
            if (mPageRotation == pdftron.PDF.Page.Rotate.e_270)
            {
                // Squiggly to the right
                xStart = drawRect.x2 - ((wiggleHeight) / 2);
                xStep = wiggleHeight;
                yStep = -stepLength;
                yStart = drawRect.y2;
                xAlternate = -1;
                yAlternate = 1;
                numSteps = (int)((drawRect.y2 - drawRect.y1) / stepLength);
            }

            // create a path based on above data.
            pathPoint.X = xStart;
            pathPoint.Y = yStart;
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = pathPoint;

            for (int i = 0; i < numSteps; i++)
            {
                pathPoint.X += xStep;
                pathPoint.Y += yStep;
                xStep *= xAlternate;
                yStep *= yAlternate;
                pathFigure.Segments.Add(new LineSegment() { Point = pathPoint });
            }
            pathGeometry.Figures.Add(pathFigure);
            squigglyShape.Data = pathGeometry;
        }
    }
}
