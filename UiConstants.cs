using System.Windows;
using System.Windows.Media;
using CryptoPnLWidget.Services;

namespace CryptoPnLWidget
{
    public static class UiConstants
    {
        private static ThemeManager? _themeManager;

        // Font sizes
        public const double FontSizeSmall = 12;
        public const double FontSizeMedium = 14;
        public const double FontSizeLarge = 20;

        // Font weights
        public static readonly FontWeight FontWeightNormal = FontWeights.Normal;
        public static readonly FontWeight FontWeightBold = FontWeights.Bold;

        // Margins, Thickness, etc.
        public static readonly Thickness PositionGridMargin = new Thickness(0, 3, 0, 3);
        public static readonly CornerRadius WidgetCornerRadius = new CornerRadius(10);
        public static readonly Thickness WidgetBorderThickness = new Thickness(1);

        public static void Initialize(ThemeManager themeManager)
        {
            _themeManager = themeManager;
        }

        // Colors - теперь динамические
        public static Brush FontColor => _themeManager?.GetFontColor() ?? Brushes.White;
        public static Brush ForegroundGreen => _themeManager?.GetGreenColor() ?? Brushes.Green;
        public static Brush ForegroundRed => _themeManager?.GetRedColor() ?? Brushes.Red;
        public static Brush BackgroundTransparent => Brushes.Transparent;
        public static Brush BorderBrushDarkGray => _themeManager?.GetBorderColor() ?? Brushes.DarkGray;
        public static Brush BackgroundWidget => _themeManager?.GetBackgroundColor() ?? new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
    }
} 