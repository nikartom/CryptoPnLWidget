using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace CryptoPnLWidget.Services
{
    public enum Theme
    {
        Dark,
        Light
    }

    public class ThemeSettings
    {
        public double BalanceFontSize { get; set; } = 20;
        public double ContentFontSize { get; set; } = 12;
        public double HeaderFontSize { get; set; } = 14;
        
        public Color ProfitColor { get; set; } = Colors.LightGreen;
        public Color LossColor { get; set; } = Colors.LightCoral;
        
        public Color DarkBackgroundColor { get; set; } = Color.FromArgb(128, 0, 0, 0);
        public Color DarkFontColor { get; set; } = Colors.White;
        public Color DarkBorderColor { get; set; } = Colors.DarkGray;
        
        public Color LightBackgroundColor { get; set; } = Color.FromArgb(200, 255, 255, 255);
        public Color LightFontColor { get; set; } = Colors.Black;
        public Color LightBorderColor { get; set; } = Colors.LightGray;
    }

    public class ThemeManager
    {
        private Theme _currentTheme = Theme.Dark;
        private readonly string _settingsFilePath;
        private ThemeSettings _settings = new ThemeSettings();
        
        // Настраиваемые параметры
        public double BalanceFontSize { get => _settings.BalanceFontSize; set => _settings.BalanceFontSize = value; }
        public double ContentFontSize { get => _settings.ContentFontSize; set => _settings.ContentFontSize = value; }
        public double HeaderFontSize { get => _settings.HeaderFontSize; set => _settings.HeaderFontSize = value; }
        
        // Цвета для PnL (единые для обеих тем)
        public Color ProfitColor { get => _settings.ProfitColor; set => _settings.ProfitColor = value; }
        public Color LossColor { get => _settings.LossColor; set => _settings.LossColor = value; }
        
        // Настройки для темной темы
        public Color DarkBackgroundColor { get => _settings.DarkBackgroundColor; set => _settings.DarkBackgroundColor = value; }
        public Color DarkFontColor { get => _settings.DarkFontColor; set => _settings.DarkFontColor = value; }
        public Color DarkBorderColor { get => _settings.DarkBorderColor; set => _settings.DarkBorderColor = value; }
        
        // Настройки для светлой темы
        public Color LightBackgroundColor { get => _settings.LightBackgroundColor; set => _settings.LightBackgroundColor = value; }
        public Color LightFontColor { get => _settings.LightFontColor; set => _settings.LightFontColor = value; }
        public Color LightBorderColor { get => _settings.LightBorderColor; set => _settings.LightBorderColor = value; }
        
        public Theme CurrentTheme => _currentTheme;
        
        public event Action<Theme>? ThemeChanged;

        public ThemeManager()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificFolder = Path.Combine(appDataFolder, "CryptoPnLWidget");
            Directory.CreateDirectory(appSpecificFolder);
            _settingsFilePath = Path.Combine(appSpecificFolder, "theme_settings.json");
            
            LoadSettings();
        }

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
                ? new SolidColorBrush(_settings.DarkBackgroundColor)
                : new SolidColorBrush(_settings.LightBackgroundColor);
        }

        public Brush GetBorderColor()
        {
            return _currentTheme == Theme.Dark 
                ? new SolidColorBrush(_settings.DarkBorderColor)
                : new SolidColorBrush(_settings.LightBorderColor);
        }

        public Brush GetFontColor()
        {
            return _currentTheme == Theme.Dark 
                ? new SolidColorBrush(_settings.DarkFontColor)
                : new SolidColorBrush(_settings.LightFontColor);
        }

        public Brush GetGreenColor()
        {
            return new SolidColorBrush(_settings.ProfitColor);
        }

        public Brush GetRedColor()
        {
            return new SolidColorBrush(_settings.LossColor);
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

        // Методы для получения размеров шрифтов
        public double GetBalanceFontSize() => _settings.BalanceFontSize;
        public double GetContentFontSize() => _settings.ContentFontSize;
        public double GetHeaderFontSize() => _settings.HeaderFontSize;

        // Методы для установки размеров шрифтов
        public void SetBalanceFontSize(double size)
        {
            _settings.BalanceFontSize = size;
            SaveSettings();
            ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetContentFontSize(double size)
        {
            _settings.ContentFontSize = size;
            SaveSettings();
            ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetHeaderFontSize(double size)
        {
            _settings.HeaderFontSize = size;
            SaveSettings();
            ThemeChanged?.Invoke(_currentTheme);
        }

        // Методы для установки цветов PnL (единые)
        public void SetProfitColor(Color color)
        {
            _settings.ProfitColor = color;
            SaveSettings();
            ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetLossColor(Color color)
        {
            _settings.LossColor = color;
            SaveSettings();
            ThemeChanged?.Invoke(_currentTheme);
        }

        // Методы для установки цветов темы
        public void SetDarkBackgroundColor(Color color)
        {
            _settings.DarkBackgroundColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Dark)
                ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetDarkFontColor(Color color)
        {
            _settings.DarkFontColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Dark)
                ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetDarkBorderColor(Color color)
        {
            _settings.DarkBorderColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Dark)
                ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetLightBackgroundColor(Color color)
        {
            _settings.LightBackgroundColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Light)
                ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetLightFontColor(Color color)
        {
            _settings.LightFontColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Light)
                ThemeChanged?.Invoke(_currentTheme);
        }

        public void SetLightBorderColor(Color color)
        {
            _settings.LightBorderColor = color;
            SaveSettings();
            if (_currentTheme == Theme.Light)
                ThemeChanged?.Invoke(_currentTheme);
        }

        // Сохранение и загрузка настроек
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                CryptoPnLWidget.Services.UIManager.RaiseGlobalError($"Ошибка при сохранении настроек темы: {ex.Message}");
            }
        }

        public void ForceThemeUpdate()
        {
            ThemeChanged?.Invoke(_currentTheme);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonSerializer.Deserialize<ThemeSettings>(json) ?? new ThemeSettings();
                }
                else
                {
                    _settings = new ThemeSettings();
                }
            }
            catch (Exception ex)
            {
                CryptoPnLWidget.Services.UIManager.RaiseGlobalError($"Ошибка при загрузке настроек темы: {ex.Message}");
                _settings = new ThemeSettings();
            }
        }
    }
} 