
using pdftron.PDF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;



namespace pdftron.PDF.Tools
{
    public class LineThicknessValidationRule : ValidationRule
    {
        double mMinThickness = 0.5;
        double mMaxThickness = 15;

        public double MinThickness
        {
            get { return this.mMinThickness; }
            set { this.mMinThickness = value; }
        }

        public double MaxThickness
        {
            get { return this.mMaxThickness; }
            set { this.mMaxThickness = value; }
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            double thickness;

            // Is a number? 
            if (!double.TryParse((string)value, out thickness))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range? 
            if ((thickness < this.mMinThickness) || (thickness > this.mMaxThickness))
            {
                string msg = string.Format("Margin must be between {0} and {1}.", this.mMinThickness, this.mMaxThickness);
                return new ValidationResult(false, msg);
            }

            // Number is valid 
            return new ValidationResult(true, null);
        }
    }


    /// <summary>
    /// This class is the base class for wrapping annotations into selection objects.
    /// This class will use the annotations bounding box and gives the user 8 control points to move around.
    /// 
    /// Create a subclass of this to make a more specific tool for other annotations.
    /// </summary>
    public partial class AnnotationPropertiesPopup : Window
    {
        protected bool mHasLineColorChanged = false;
        protected bool mHasFillColorChanged = false;
        protected bool mHasTextColorChanged = false;
        protected bool mHasLineThicknessChanged = false;
        protected bool mHasLineStyleChanged = false;
        protected bool mHasLineStartStyleChanged = false;
        protected bool mHasLineEndStyleChanged = false;
        protected bool mHasOpacityChanged = false;
        protected bool mHasFontSizeChanged = false;


        internal SelectionHelper mSelectionHelper;

        public double ThicknessPathSource { get; set; }
        int[] fontSizes = { 9, 11, 14, 18, 24, 36 };

        internal AnnotationPropertiesPopup(SelectionHelper selection)
            : base()
        {
            InitializeComponent();
            this.DataContext = this;
            mSelectionHelper = selection;

            if (mSelectionHelper.HasLineColor)
            {
                LineColorOption.Visibility = Visibility.Visible;
                LineColorRect.Fill = mSelectionHelper.LineColor;
            }
            if (mSelectionHelper.HasFillColor)
            {
                FillColorOption.Visibility = Visibility.Visible;
                FillColorRect.Fill = mSelectionHelper.FillColor;
            }
            if (mSelectionHelper.HasTextColor)
            {
                TextColorOption.Visibility = Visibility.Visible;
                TextColorRect.Fill = mSelectionHelper.TextColor;
            }
            if (mSelectionHelper.HasLineThickness)
            {
                LineThicknessOption.Visibility = Visibility.Visible;
                LineThicknessBox.Text = "" + mSelectionHelper.LineThickness;
                if (mSelectionHelper.CanLineThicknessBeZero) 
                {
                    LineThicknessRule.MinThickness = 0;
                }
            }
            if (mSelectionHelper.HasLineStyle)
            {
                LineStyleOption.Visibility = Visibility.Visible;
                LineStyleComboBox.SelectedIndex = GetIndexFromLineStyle(mSelectionHelper.LineStyle);
            }
            if (mSelectionHelper.HasLineStartStyle)
            {
                LineStartStyleOption.Visibility = Visibility.Visible;
                LineStartStyleComboBox.SelectedIndex = GetIndexFromEndingStyle(mSelectionHelper.LineStartStyle);
            }
            if (mSelectionHelper.HasLineEndStyle)
            {
                LineEndStyleOption.Visibility = Visibility.Visible;
                LineEndStyleComboBox.SelectedIndex = GetIndexFromEndingStyle(mSelectionHelper.LineEndStyle);
            }
            if (mSelectionHelper.HasOpacity)
            {
                OpacityOption.Visibility = Visibility.Visible;
                OpacitySlider.Value = (int)(mSelectionHelper.Opacity * 100);
            }
            if (mSelectionHelper.HasFontSize)
            {
                FontSizeOption.Visibility = Visibility.Visible;
                FontSizeComboBox.SelectedIndex = GetIndexFromFontSize(mSelectionHelper.FontSize);
            }
            mHasLineColorChanged = false;
            mHasFillColorChanged = false;
            mHasTextColorChanged = false;
            mHasLineThicknessChanged = false;
            mHasLineStyleChanged = false;
            mHasLineStartStyleChanged = false;
            mHasLineEndStyleChanged = false;
            mHasOpacityChanged = false;
            mHasFontSizeChanged = false;
        }

        #region Event Handling

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (mHasLineColorChanged)
            {
                mSelectionHelper.LineColor = LineColorRect.Fill as SolidColorBrush;
            }
            if (mHasFillColorChanged)
            {
                mSelectionHelper.FillColor = FillColorRect.Fill as SolidColorBrush;
            }
            if (mHasTextColorChanged)
            {
                mSelectionHelper.TextColor = TextColorRect.Fill as SolidColorBrush;
            }
            if (mHasLineThicknessChanged && !Validation.GetHasError(LineThicknessBox))
            {
                mSelectionHelper.LineThickness = ThicknessPathSource;
            }
            if (mHasLineStyleChanged)
            {
                Annot.BorderStyle lineStyle = GetLineStyleFromIndex(LineStyleComboBox.SelectedIndex);
                if (lineStyle != null)
                {
                    mSelectionHelper.LineStyle = lineStyle;
                }
            }
            if (mHasLineStartStyleChanged)
            {
                mSelectionHelper.LineStartStyle = GetEndingStyleFromIndex(LineStartStyleComboBox.SelectedIndex);
            }
            if (mHasLineEndStyleChanged)
            {
                mSelectionHelper.LineEndStyle = GetEndingStyleFromIndex(LineEndStyleComboBox.SelectedIndex);
            }
            if (mHasOpacityChanged)
            {
                mSelectionHelper.Opacity = OpacitySlider.Value / 100;
            }
            if (mHasFontSizeChanged)
            {
                mSelectionHelper.FontSize = GetFontSizeFromIndex(FontSizeComboBox.SelectedIndex);
            }
            this.DialogResult = true;
        }


        private void LineColor_Click(object sender, RoutedEventArgs e)
        {
            Button but = sender as Button;
            Rectangle rect = but.Content as Rectangle;
            ColorPicker colorPicker = new ColorPicker(rect, mSelectionHelper.CanLineColorBeEmpty);
            colorPicker.Owner = this;

            System.Nullable<bool> result = colorPicker.ShowDialog();
            if (result != null && result == true)
            {
                mHasLineColorChanged = true;
            }
        }

        private void TextColor_Click(object sender, RoutedEventArgs e)
        {
            Button but = sender as Button;
            Rectangle rect = but.Content as Rectangle;
            ColorPicker colorPicker = new ColorPicker(rect, mSelectionHelper.CanTextColorBeEmpty);
            colorPicker.Owner = this;

            System.Nullable<bool> result = colorPicker.ShowDialog();
            if (result != null && result == true)
            {
                mHasTextColorChanged = true;
            }
        }

        private void FillColor_Click(object sender, RoutedEventArgs e)
        {
            Button but = sender as Button;
            Rectangle rect = but.Content as Rectangle;
            ColorPicker colorPicker = new ColorPicker(rect, mSelectionHelper.CanFillColorBeEmpty);
            colorPicker.Owner = this;

            System.Nullable<bool> result = colorPicker.ShowDialog();
            if (result != null && result == true)
            {
                mHasFillColorChanged = true;
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mHasOpacityChanged = true;
        }

        private void LineEndStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mHasLineEndStyleChanged = true;
        }

        private void LineStartStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mHasLineStartStyleChanged = true;
        }

        private void LineStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mHasLineStyleChanged = true;
        }

        private void LineThicknessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mHasLineThicknessChanged = true;
        }

        private void FontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mHasFontSizeChanged = true;
        }

        #endregion Event Handling

        #region Utility Functions

        private int GetIndexFromLineStyle(Annot.BorderStyle borderStyle)
        {
            if (borderStyle.border_style == Annot.BorderStyle.Style.e_solid)
            {
                return 0;
            }
            else if (borderStyle.border_style == Annot.BorderStyle.Style.e_dashed && borderStyle.dash != null)
            {
                if (borderStyle.dash.Length == 2)
                {
                    if (borderStyle.dash[0] == 1 && borderStyle.dash[1] == 1)
                    {
                        return 1;
                    }
                    if (borderStyle.dash[0] == 3 && borderStyle.dash[1] == 1)
                    {
                        return 2;
                    }
                    if (borderStyle.dash[0] == 5 && borderStyle.dash[1] == 2)
                    {
                        return 3;
                    }
                }
            }
            return 4;
        }

        private Annot.BorderStyle GetLineStyleFromIndex(int index)
        {
            Annot.BorderStyle borderStyle = new Annot.BorderStyle(Annot.BorderStyle.Style.e_dashed, 1);
            switch (index)
            {
                case 0:
                    borderStyle.border_style = Annot.BorderStyle.Style.e_solid;
                    break;
                case 1:
                    borderStyle.dash = new double[2] { 1, 1 };
                    break;
                case 2:
                    borderStyle.dash = new double[2] { 3, 1 };
                    break;
                case 3:
                    borderStyle.dash = new double[2] { 5 , 2 };
                    break;
                case 4:
                    borderStyle = null;
                    break;
            }
            return borderStyle;
        }

        private int GetIndexFromEndingStyle(pdftron.PDF.Annots.Line.EndingStyle style)
        {
            switch (style)
            {
                case pdftron.PDF.Annots.Line.EndingStyle.e_None:
                    return 0;
                case pdftron.PDF.Annots.Line.EndingStyle.e_Circle:
                    return 1;
                case pdftron.PDF.Annots.Line.EndingStyle.e_Square:
                    return 2;
                case pdftron.PDF.Annots.Line.EndingStyle.e_Diamond:
                    return 3;
                case pdftron.PDF.Annots.Line.EndingStyle.e_Butt:
                    return 4;
                case pdftron.PDF.Annots.Line.EndingStyle.e_Slash:
                    return 5;
                case pdftron.PDF.Annots.Line.EndingStyle.e_OpenArrow:
                    return 6;
                case pdftron.PDF.Annots.Line.EndingStyle.e_ClosedArrow:
                    return 7;
                case pdftron.PDF.Annots.Line.EndingStyle.e_ROpenArrow:
                    return 8;
                case pdftron.PDF.Annots.Line.EndingStyle.e_RClosedArrow:
                    return 9;
            }
            return 10; // unknown
        }

        private pdftron.PDF.Annots.Line.EndingStyle GetEndingStyleFromIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_None;
                case 1:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_Circle;
                case 2:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_Square;
                case 3:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_Diamond;
                case 4:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_Butt;
                case 5:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_Slash;
                case 6:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_OpenArrow;
                case 7:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_ClosedArrow;
                case 8:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_ROpenArrow;
                case 9:
                    return pdftron.PDF.Annots.Line.EndingStyle.e_RClosedArrow;
            }
            return pdftron.PDF.Annots.Line.EndingStyle.e_Unknown;
        }

        private int GetIndexFromFontSize(double fontSize)
        {
            ComboBoxItem item;
            int currentIndex = fontSizes.Length;
            FontSizeComboBox.Items.Clear();
            for (int i = 0; i < fontSizes.Length; i++)
            {
                item = new ComboBoxItem();
                item.Content = "" + fontSizes[i];
                FontSizeComboBox.Items.Add(item);
                if (fontSize >= fontSizes[i])
                {
                    currentIndex = i;
                }
            }

            item = new ComboBoxItem();
            item.Content = "Auto";
            FontSizeComboBox.Items.Add(item);

            return currentIndex;

        }

        private double GetFontSizeFromIndex(int index)
        {
            if (index < fontSizes.Length)
            {
                return fontSizes[index];
            }
            return 0;
        }

        #endregion Utility Functions



    }
}

