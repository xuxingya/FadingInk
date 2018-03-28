using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pdftron.PDF.Tools.Utilities
{
    class ColorSettings
    {

        public struct ToolColor
        {
            public byte R;
            public byte G;
            public byte B;
            public bool Use; // For when you select an empty color.
        }

        public static ToolColor StrokeColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.StrokeColorUse = value.Use;
            }
        }

        public static ToolColor FillColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.FillColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.FillColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.FillColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.FillColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.FillColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.FillColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.FillColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.FillColorUse = value.Use;
            }
        }

        public static ToolColor HighlightColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.HighlightColorUse = value.Use;
            }
        }

        public static ToolColor TextMarkupColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.TextMarkupColorUse = value.Use;
            }
        }

        public static ToolColor TextColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.TextColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.TextColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.TextColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.TextColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.TextColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.TextColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.TextColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.TextColorUse = value.Use;
            }
        }

        public static ToolColor TextFillColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.TextFillColorUse = value.Use;
            }
        }

        public static ToolColor TextLineColor
        {
            get
            {
                ToolColor color = new ToolColor();
                color.R = pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorR;
                color.G = pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorG;
                color.B = pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorB;
                color.Use = pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorUse;
                return color;
            }
            set
            {
                pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorR = value.R;
                pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorG = value.G;
                pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorB = value.B;
                pdftron.PDF.Tools.Properties.Settings.Default.TextLineColorUse = value.Use;
            }
        }
    }
}
