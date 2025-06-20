using System.Windows;
using System.Windows.Media;

namespace CryptoPnLWidget
{
    public static class UiConstants
    {
        // Font sizes
        public const double FontSizeSmall = 12;
        public const double FontSizeMedium = 14;
        public const double FontSizeLarge = 20;

        // Font weights
        public static readonly FontWeight FontWeightNormal = FontWeights.Normal;
        public static readonly FontWeight FontWeightBold = FontWeights.Bold;

        // Colors
        public static readonly Brush FontColor = Brushes.White;
        public static readonly Brush ForegroundGreen = Brushes.Green;
        public static readonly Brush ForegroundRed = Brushes.Red;
        public static readonly Brush BackgroundTransparent = Brushes.Transparent;
        public static readonly Brush BorderBrushDarkGray = Brushes.DarkGray;
        public static readonly Brush BackgroundWidget = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)); // #80000000

        // Margins, Thickness, etc.
        public static readonly Thickness PositionGridMargin = new Thickness(0, 3, 0, 3);
        public static readonly CornerRadius WidgetCornerRadius = new CornerRadius(10);
        public static readonly Thickness WidgetBorderThickness = new Thickness(1);
    }
} 