using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;
using System.Windows.Media;

namespace CryptoPnLWidget
{
    public partial class SettingsWindow : Window
    {
        private readonly ThemeManager _themeManager;
        private readonly ExchangeKeysManager _exchangeKeysManager;

        public SettingsWindow(CryptoPnLWidget.Services.ThemeManager themeManager)
        {
            InitializeComponent();
            _themeManager = themeManager;
            _exchangeKeysManager = new ExchangeKeysManager();
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
            else if (SettingsMenu.SelectedIndex == 2)
            {
                ShowSupportSection();
            }
        }

        private void ShowApiSettings()
        {
            if (SettingsContent == null) return;
            
            var panel = new StackPanel { Margin = new Thickness(20) };

            // Создаем сворачиваемую секцию для Bybit
            var bybitExpander = CreateExchangeExpander("Bybit");
            panel.Children.Add(bybitExpander);

            // Кнопка "Добавить API" ниже списка бирж
            var addApiButton = new Button { 
                Content = "Добавить API",
                Width = 150,
                Height = 30,
                Margin = new Thickness(0, 10, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            addApiButton.Click += (s, e) => ManageExchangeKeys("Bybit");
            panel.Children.Add(addApiButton);

            // Информация о том, как получить ключи
            var infoPanel = new StackPanel { Margin = new Thickness(0,20,0,0) };
            infoPanel.Children.Add(new TextBlock { 
                Text = "Как получить API ключи Bybit:", 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0,0,0,10) 
            });
            
            var instructions = new TextBlock { 
                Text = "1. Войдите в аккаунт Bybit\n" +
                      "2. Перейдите в API Management\n" +
                      "3. Создайте новый API ключ\n" +
                      "4. Включите права на чтение\n" +
                      "5. Скопируйте API Key и Secret",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0,0,0,10) 
            };
            infoPanel.Children.Add(instructions);

            panel.Children.Add(infoPanel);
            
            SettingsContent.Content = panel;
        }

        private Expander CreateExchangeExpander(string exchangeName)
        {
            bool hasKeys = _exchangeKeysManager.HasKeysForExchange(exchangeName);
            
            // Создаем заголовок с названием биржи
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var exchangeNameBlock = new TextBlock { 
                Text = exchangeName, 
                FontSize = 14, 
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(exchangeNameBlock);
            
            // Добавляем статус (если есть ключи)
            if (hasKeys)
            {
                var statusBlock = new TextBlock { 
                    Text = " (настроено)", 
                    Foreground = System.Windows.Media.Brushes.Green,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5,0,0,0)
                };
                headerPanel.Children.Add(statusBlock);
            }

            // Создаем Expander
            var expander = new Expander
            {
                Header = headerPanel,
                IsExpanded = false, // По умолчанию свернут
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Создаем содержимое Expander
            var contentPanel = new StackPanel { Margin = new Thickness(20, 10, 0, 10) };

            // Поля для ввода API ключей
            var apiKeyLabel = new TextBlock { 
                Text = "API Key:", 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0,0,0,5) 
            };
            contentPanel.Children.Add(apiKeyLabel);
            
            var apiKeyTextBox = new TextBox { 
                Height = 25,
                Margin = new Thickness(0,0,0,10),
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            contentPanel.Children.Add(apiKeyTextBox);
            
            var apiSecretLabel = new TextBlock { 
                Text = "API Secret:", 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0,0,0,5) 
            };
            contentPanel.Children.Add(apiSecretLabel);
            
            var apiSecretPasswordBox = new PasswordBox { 
                Height = 25,
                Margin = new Thickness(0,0,0,15),
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            contentPanel.Children.Add(apiSecretPasswordBox);

            // Заполняем поля текущими значениями
            if (hasKeys)
            {
                var keys = _exchangeKeysManager.GetKeysForExchange(exchangeName);
                if (keys != null)
                {
                    apiKeyTextBox.Text = keys.ApiKey;
                    apiSecretPasswordBox.Password = keys.ApiSecret;
                }
            }

            // Кнопка "Сохранить"
            var saveButton = new Button { 
                Content = "Сохранить",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0,0,0,10)
            };
            saveButton.Click += (s, e) => SaveExchangeKeys(exchangeName, apiKeyTextBox.Text, apiSecretPasswordBox.Password, expander);
            contentPanel.Children.Add(saveButton);

            // Информация о последнем обновлении (если есть ключи)
            if (hasKeys)
            {
                var keys = _exchangeKeysManager.GetKeysForExchange(exchangeName);
                if (keys != null)
                {
                    var lastUpdated = new TextBlock { 
                        Text = $"Обновлено: {keys.LastUpdated:dd.MM.yyyy HH:mm}", 
                        FontSize = 11,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        Margin = new Thickness(0,0,0,10) 
                    };
                    contentPanel.Children.Add(lastUpdated);
                }
            }

            expander.Content = contentPanel;
            return expander;
        }

        private void SaveExchangeKeys(string exchangeName, string apiKey, string apiSecret, Expander expander)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                MessageBox.Show("Пожалуйста, введите оба ключа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _exchangeKeysManager.SaveKeysForExchange(exchangeName, apiKey, apiSecret);
                MessageBox.Show("API ключи успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Обновляем заголовок Expander
                UpdateExpanderHeader(expander, exchangeName);
            }
            catch (System.Exception ex)
            {
                CryptoPnLWidget.Services.UIManager.RaiseGlobalError($"Ошибка при сохранении ключей: {ex.Message}");
            }
        }

        private void UpdateExpanderHeader(Expander expander, string exchangeName)
        {
            bool hasKeys = _exchangeKeysManager.HasKeysForExchange(exchangeName);
            
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var exchangeNameBlock = new TextBlock { 
                Text = exchangeName, 
                FontSize = 14, 
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerPanel.Children.Add(exchangeNameBlock);
            
            if (hasKeys)
            {
                var statusBlock = new TextBlock { 
                    Text = " (настроено)", 
                    Foreground = System.Windows.Media.Brushes.Green,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5,0,0,0)
                };
                headerPanel.Children.Add(statusBlock);
            }

            expander.Header = headerPanel;
        }

        private void ManageExchangeKeys(string exchangeName)
        {
            var apiSettingsWindow = new API.ApiSettingsWindow(_exchangeKeysManager);
            apiSettingsWindow.Owner = this;
            bool? result = apiSettingsWindow.ShowDialog();
            
            if (result == true)
            {
                // Обновляем отображение после изменения ключей
                ShowApiSettings();
            }
        }

        private void ShowInterfaceSettings()
        {
            if (SettingsContent == null) return;
            
            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            // Общие настройки сверху (без заголовка)
            var generalPanel = CreateGeneralSettingsPanel();
            mainPanel.Children.Add(generalPanel);

            // Разделитель
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 20, 0, 20) });

            // Трехколоночная панель для настроек тем
            var themesGrid = new Grid();
            themesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Подписи
            themesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Светлая тема
            themesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Темная тема

            // Колонка с подписями
            var labelsPanel = CreateThemeLabelsPanel();
            Grid.SetColumn(labelsPanel, 0);
            themesGrid.Children.Add(labelsPanel);

            // Настройки светлой темы (средняя колонка)
            var lightThemePanel = CreateThemeSettingsPanel("Светлая тема", Theme.Light);
            Grid.SetColumn(lightThemePanel, 1);
            themesGrid.Children.Add(lightThemePanel);

            // Настройки темной темы (правая колонка)
            var darkThemePanel = CreateThemeSettingsPanel("Тёмная тема", Theme.Dark);
            Grid.SetColumn(darkThemePanel, 2);
            themesGrid.Children.Add(darkThemePanel);

            mainPanel.Children.Add(themesGrid);

            // Оборачиваем в ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            SettingsContent.Content = scrollViewer;
        }

        private StackPanel CreateThemeLabelsPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 15, 0) };

            // Заголовок колонки
            panel.Children.Add(new TextBlock { 
                Text = "Параметры", 
                FontSize = 14, 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Подписи для цветов
            panel.Children.Add(new TextBlock { 
                Text = "Цвет фона:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 25
            });

            panel.Children.Add(new TextBlock { 
                Text = "Цвет шрифта:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 25
            });

            panel.Children.Add(new TextBlock { 
                Text = "Цвет границы:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 25
            });

            return panel;
        }

        private StackPanel CreateThemeSettingsPanel(string themeName, Theme theme)
        {
            var panel = new StackPanel { Margin = new Thickness(10, 0, 10, 0) };

            // Заголовок темы
            panel.Children.Add(new TextBlock { 
                Text = themeName, 
                FontSize = 14, 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Цвет фона
            var backgroundColor = theme == Theme.Dark ? _themeManager.DarkBackgroundColor : _themeManager.LightBackgroundColor;
            var backgroundColorButton = CreateColorButton(backgroundColor, 
                (color) => 
                {
                    if (theme == Theme.Dark)
                        _themeManager.SetDarkBackgroundColor(color);
                    else
                        _themeManager.SetLightBackgroundColor(color);
                    // Принудительно вызываем обновление UI
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(backgroundColorButton);

            // Цвет шрифта
            var fontColor = theme == Theme.Dark ? _themeManager.DarkFontColor : _themeManager.LightFontColor;
            var fontColorButton = CreateColorButton(fontColor, 
                (color) => 
                {
                    if (theme == Theme.Dark)
                        _themeManager.SetDarkFontColor(color);
                    else
                        _themeManager.SetLightFontColor(color);
                    // Принудительно вызываем обновление UI
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(fontColorButton);

            // Цвет границы
            var borderColor = theme == Theme.Dark ? _themeManager.DarkBorderColor : _themeManager.LightBorderColor;
            var borderColorButton = CreateColorButton(borderColor, 
                (color) => 
                {
                    if (theme == Theme.Dark)
                        _themeManager.SetDarkBorderColor(color);
                    else
                        _themeManager.SetLightBorderColor(color);
                    // Принудительно вызываем обновление UI
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(borderColorButton);

            return panel;
        }

        private Button CreateColorButton(System.Windows.Media.Color currentColor, Action<System.Windows.Media.Color> onColorChanged)
        {
            var colorButton = new Button
            {
                Width = 50,
                Height = 25,
                Background = new SolidColorBrush(currentColor),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            colorButton.Click += (s, e) =>
            {
                var colorDialog = new System.Windows.Forms.ColorDialog
                {
                    Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B),
                    FullOpen = true
                };

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newColor = System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B
                    );
                    
                    colorButton.Background = new SolidColorBrush(newColor);
                    onColorChanged(newColor);
                }
            };

            return colorButton;
        }

        private StackPanel CreateGeneralSettingsPanel()
        {
            var panel = new StackPanel();

            // Размеры шрифтов
            panel.Children.Add(new TextBlock { 
                Text = "Размеры шрифтов:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 10) 
            });

            // Размер шрифта баланса
            var balanceFontPanel = CreateFontSizePanel("Размер шрифта баланса:", _themeManager.BalanceFontSize, 
                (value) => 
                {
                    _themeManager.SetBalanceFontSize(value);
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(balanceFontPanel);

            // Размер шрифта контента
            var contentFontPanel = CreateFontSizePanel("Размер шрифта контента:", _themeManager.ContentFontSize, 
                (value) => 
                {
                    _themeManager.SetContentFontSize(value);
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(contentFontPanel);

            // Размер шрифта заголовков
            var headerFontPanel = CreateFontSizePanel("Размер шрифта заголовков:", _themeManager.HeaderFontSize, 
                (value) => 
                {
                    _themeManager.SetHeaderFontSize(value);
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(headerFontPanel);

            // Цвета PnL (единые)
            panel.Children.Add(new TextBlock { 
                Text = "Цвета PnL:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 20, 0, 10) 
            });

            // Цвет прибыли (единый)
            var profitColorPanel = CreateColorPanel("Цвет прибыли:", _themeManager.ProfitColor, 
                (color) => 
                {
                    _themeManager.SetProfitColor(color);
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(profitColorPanel);

            // Цвет убытка (единый)
            var lossColorPanel = CreateColorPanel("Цвет убытка:", _themeManager.LossColor, 
                (color) => 
                {
                    _themeManager.SetLossColor(color);
                    _themeManager.ForceThemeUpdate();
                });
            panel.Children.Add(lossColorPanel);

            return panel;
        }

        private StackPanel CreateFontSizePanel(string label, double currentValue, Action<double> onValueChanged)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            
            panel.Children.Add(new TextBlock { 
                Text = label, 
                Width = 150, 
                VerticalAlignment = VerticalAlignment.Center 
            });

            var slider = new Slider
            {
                Minimum = 8,
                Maximum = 32,
                Value = currentValue,
                Width = 150,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            var valueLabel = new TextBlock { 
                Text = currentValue.ToString("F0"), 
                Width = 30, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            slider.ValueChanged += (s, e) =>
            {
                valueLabel.Text = e.NewValue.ToString("F0");
                onValueChanged(e.NewValue);
            };

            panel.Children.Add(slider);
            panel.Children.Add(valueLabel);

            return panel;
        }

        private StackPanel CreateColorPanel(string label, System.Windows.Media.Color currentColor, Action<System.Windows.Media.Color> onColorChanged)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            
            panel.Children.Add(new TextBlock { 
                Text = label, 
                Width = 150, 
                VerticalAlignment = VerticalAlignment.Center 
            });

            var colorButton = new Button
            {
                Width = 50,
                Height = 25,
                Background = new SolidColorBrush(currentColor),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };

            colorButton.Click += (s, e) =>
            {
                var colorDialog = new System.Windows.Forms.ColorDialog
                {
                    Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B),
                    FullOpen = true
                };

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newColor = System.Windows.Media.Color.FromArgb(
                        colorDialog.Color.A,
                        colorDialog.Color.R,
                        colorDialog.Color.G,
                        colorDialog.Color.B
                    );
                    
                    colorButton.Background = new SolidColorBrush(newColor);
                    onColorChanged(newColor);
                }
            };

            panel.Children.Add(colorButton);

            return panel;
        }

        private void ShowSupportSection()
        {
            if (SettingsContent == null) return;

            var panel = new StackPanel { Margin = new Thickness(30, 20, 0, 0) };

            // Email
            var emailBlock = new TextBlock
            {
                Text = "pndmasterbot@gmail.com",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 18)
            };
            panel.Children.Add(emailBlock);

            // GitHub
            var githubBlock = new TextBlock
            {
                Text = "GitHub: https://github.com/nikartom/CryptoPnLWidget",
                Foreground = Brushes.SteelBlue,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10),
                TextDecorations = TextDecorations.Underline
            };
            githubBlock.MouseLeftButtonUp += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/nikartom/CryptoPnLWidget",
                UseShellExecute = true
            });
            panel.Children.Add(githubBlock);

            // Telegram
            var tgBlock = new TextBlock
            {
                Text = "Telegram-бот: https://t.me/PumpDumpMaster_bot",
                Foreground = Brushes.SteelBlue,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10),
                TextDecorations = TextDecorations.Underline
            };
            tgBlock.MouseLeftButtonUp += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://t.me/PumpDumpMaster_bot",
                UseShellExecute = true
            });
            panel.Children.Add(tgBlock);

            // Charts site
            var chartsBlock = new TextBlock
            {
                Text = "Графики: https://bybitcharts.pro/",
                Foreground = Brushes.SteelBlue,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10),
                TextDecorations = TextDecorations.Underline
            };
            chartsBlock.MouseLeftButtonUp += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://bybitcharts.pro/",
                UseShellExecute = true
            });
            panel.Children.Add(chartsBlock);

            SettingsContent.Content = panel;
        }
    }
} 