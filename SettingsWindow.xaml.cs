using System.Windows;
using System.Windows.Controls;
using CryptoPnLWidget.Services;

namespace CryptoPnLWidget
{
    public partial class SettingsWindow : Window
    {
        private readonly ThemeManager _themeManager;

        public SettingsWindow()
        {
            InitializeComponent();
            _themeManager = new ThemeManager();
            this.Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowApiSettings();
        }

        private void SettingsMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsMenu.SelectedIndex == 0)
            {
                ShowApiSettings();
            }
            else if (SettingsMenu.SelectedIndex == 1)
            {
                ShowInterfaceSettings();
            }
        }

        private void ShowApiSettings()
        {
            if (SettingsContent == null) return;
            
            // Можно реализовать UserControl для API настроек, пока просто текст
            var text = new TextBlock { Text = "Здесь будут настройки API (управление ключами)", FontSize = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            SettingsContent.Content = text;
        }

        private void ShowInterfaceSettings()
        {
            if (SettingsContent == null) return;
            
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Настройки интерфейса", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,10) });
            var themeLabel = new TextBlock { Text = "Тема:", Margin = new Thickness(0,10,0,5) };
            panel.Children.Add(themeLabel);
            var themeCombo = new ComboBox { Width = 150 };
            themeCombo.Items.Add("Тёмная");
            themeCombo.Items.Add("Светлая");
            themeCombo.SelectedIndex = _themeManager.CurrentTheme == Theme.Dark ? 0 : 1;
            themeCombo.SelectionChanged += (s, e) =>
            {
                _themeManager.SetTheme(themeCombo.SelectedIndex == 0 ? Theme.Dark : Theme.Light);
            };
            panel.Children.Add(themeCombo);
            SettingsContent.Content = panel;
        }
    }
} 