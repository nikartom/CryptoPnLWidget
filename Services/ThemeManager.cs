using System;
using System.Windows;
using System.Windows.Media;

namespace CryptoPnLWidget.Services
{
    public enum Theme
    {
        Dark,
        Light
    }

    public class ThemeManager
    {
        private Theme _currentTheme = Theme.Dark;
        
        public Theme CurrentTheme => _currentTheme;
        
        public event Action<Theme>? ThemeChanged;

        public void ToggleTheme()
        {
            _currentTheme = _currentTheme == Theme.Dark ? Theme.Light : Theme.Dark;
            ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetTheme(Theme theme)
        {
            if (_currentTheme != theme)
            {
                _currentTheme = theme;
                ThemeChanged?.Invoke(_currentTheme);
            }
        }

        public Brush GetBackgroundColor()
        {
            return _currentTheme == Theme.Dark 
                ? new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)) // Полупрозрачный черный
                : new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)); // Полупрозрачный белый
        }

        public Brush GetBorderColor()
        {
            return _currentTheme == Theme.Dark 
                ? Brushes.DarkGray 
                : Brushes.LightGray;
        }

        public Brush GetFontColor()
        {
            return _currentTheme == Theme.Dark 
                ? Brushes.White 
                : Brushes.Black;
        }

        public Brush GetGreenColor()
        {
            return _currentTheme == Theme.Dark 
                ? Brushes.LightGreen 
                : Brushes.DarkGreen;
        }

        public Brush GetRedColor()
        {
            return _currentTheme == Theme.Dark 
                ? Brushes.LightCoral 
                : Brushes.DarkRed;
        }

        public Brush GetThemeToggleColor()
        {
            return _currentTheme == Theme.Dark 
                ? Brushes.Yellow 
                : Brushes.Orange;
        }

        public Brush GetErrorColor()
        {
            return _currentTheme == Theme.Dark
                ? Brushes.Red
                : Brushes.DarkRed;
        }
    }
} 