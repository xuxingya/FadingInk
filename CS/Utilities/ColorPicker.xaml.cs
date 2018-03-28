
using pdftron.PDF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;



namespace pdftron.PDF.Tools
{
    public partial class ColorPicker : Window
    {

        private Rectangle mSelectedColorRectangle;
        private bool mCanChooseEmpty;


        internal ColorPicker(Rectangle selectedColor, bool canChooseEmpty)
            : base()
        {
            InitializeComponent();
            this.DataContext = this;

            mCanChooseEmpty = canChooseEmpty;
            if (!mCanChooseEmpty)
            {
                NoColorButton.Visibility = Visibility.Collapsed;
            }
            mSelectedColorRectangle = selectedColor;
            ColorRectangle.Fill = selectedColor.Fill as SolidColorBrush;
            ColorRectangle.Visibility = Visibility.Visible;
            FillInWithColor();
        }

        void FillInWithColor()
        {
            Color[] colors = { Colors.White, Colors.Black, Colors.LightGray, Colors.DarkGray,
                                 Colors.Red, Colors.Orange, Colors.Yellow, Colors.YellowGreen, 
                                 Colors.Green, Colors.Cyan, Colors.Blue, Colors.Violet, 
                                 Colors.Pink, Colors.Brown, Colors.Magenta, Colors.Tan};

            foreach (Color c in colors)
            {
                Button colorButton = new Button();
                Rectangle FillRect = new Rectangle();
                FillRect.Fill = new SolidColorBrush(c);
                colorButton.Content = FillRect;
                colorButton.Click += colorButton_Click;
                PresetColorGrid.Children.Add(colorButton);
            }
        }

        #region Event Handling

        void colorButton_Click(object sender, RoutedEventArgs e)
        {
            Button but = sender as Button;
            Rectangle rect = but.Content as Rectangle;
            ColorRectangle.Fill = rect.Fill;
            ColorRectangle.Visibility = Visibility.Visible;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ColorRectangle.Visibility == Visibility.Collapsed)
            {
                mSelectedColorRectangle.Fill = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                mSelectedColorRectangle.Fill = ColorRectangle.Fill;
            }
            this.DialogResult = true;
        }

        private void NoColor_Click(object sender, RoutedEventArgs e)
        {
            ColorRectangle.Visibility = Visibility.Collapsed;
        }

        #endregion Event Handling


       
    }
}

