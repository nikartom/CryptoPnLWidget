using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;

namespace CryptoPnLWidget
{
    public partial class MainWindow : Window
    {
        private readonly TrayIconManager _trayIconManager;
        private readonly DataManager _dataManager;
        private readonly UIManager _uiManager;
        private readonly SortingManager _sortingManager;
        private readonly ThemeManager _themeManager;

        public MainWindow(
            CryptoPnLWidget.Services.ExchangeKeysManager exchangeKeysManager,
            CryptoPnLWidget.Services.Bybit.BybitService bybitService,
            CryptoPnLWidget.Services.PositionManager positionManager)
        {
            InitializeComponent();
            
            // Создаем сервисы после инициализации компонентов
            _sortingManager = new SortingManager();
            _themeManager = new ThemeManager();
            _trayIconManager = new TrayIconManager(this);
            _uiManager = new UIManager(PositionsPanel, MarginBalanceTextBlock, AvailableBalanceTextBlock, ConnectionStatusTextBlock, _sortingManager, positionManager, _themeManager);
            _dataManager = new DataManager(exchangeKeysManager, bybitService, positionManager, OnDataUpdated, OnError);

            // Подписка на глобальные ошибки
            CryptoPnLWidget.Services.UIManager.OnGlobalError += OnError;

            // Инициализируем UiConstants с ThemeManager
            UiConstants.Initialize(_themeManager);

            // Подписываемся на изменение темы
            _themeManager.ThemeChanged += OnThemeChanged;

            this.Loaded += MainWindow_Loaded;
            InitializeSortIndicators();
            ApplyCurrentTheme();
        }

        private void InitializeSortIndicators()
        {
            UpdateSortIndicators(_sortingManager.CurrentSortColumn);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            else if (WindowState == WindowState.Normal)
            {
                this.Show();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _trayIconManager?.Dispose();
            _dataManager?.Stop();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Отписка от глобальных ошибок
            CryptoPnLWidget.Services.UIManager.OnGlobalError -= OnError;
            base.OnClosed(e);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _dataManager.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                string columnName = button.Tag as string ?? "";
                _sortingManager.SetSortColumn(columnName);
                UpdateSortIndicators(columnName);
                _uiManager.UpdatePositions();
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _themeManager.ToggleTheme();
        }

        private void OnThemeChanged(Theme newTheme)
        {
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            // Обновляем основные элементы UI
            MainBorder.Background = _themeManager.GetBackgroundColor();
            MainBorder.BorderBrush = _themeManager.GetBorderColor();

            // Обновляем заголовки
            MarginBalanceLabel.Foreground = _themeManager.GetFontColor();
            AvailableBalanceLabel.Foreground = _themeManager.GetFontColor();
            MarginBalanceTextBlock.Foreground = _themeManager.GetFontColor();
            AvailableBalanceTextBlock.Foreground = _themeManager.GetFontColor();
            ConnectionStatusTextBlock.Foreground = _themeManager.GetFontColor();

            // Обновляем кнопки сортировки
            SymbolSortText.Foreground = _themeManager.GetFontColor();
            CostSortText.Foreground = _themeManager.GetFontColor();
            PnlSortText.Foreground = _themeManager.GetFontColor();
            Pnl1hSortText.Foreground = _themeManager.GetFontColor();
            Pnl24hSortText.Foreground = _themeManager.GetFontColor();
            RealizedSortText.Foreground = _themeManager.GetFontColor();

            // Обновляем индикаторы сортировки
            SymbolSortIndicator.Foreground = _themeManager.GetFontColor();
            CostSortIndicator.Foreground = _themeManager.GetFontColor();
            PnlSortIndicator.Foreground = _themeManager.GetFontColor();
            Pnl1hSortIndicator.Foreground = _themeManager.GetFontColor();
            Pnl24hSortIndicator.Foreground = _themeManager.GetFontColor();
            RealizedSortIndicator.Foreground = _themeManager.GetFontColor();

            // Обновляем кнопку переключения темы
            ThemeToggleButton.Foreground = _themeManager.GetThemeToggleColor();

            // Обновляем позиции
            _uiManager.UpdatePositions();
        }

        private void UpdateSortIndicators(string activeColumn)
        {
            // Hide all indicators first
            SymbolSortIndicator.Visibility = Visibility.Collapsed;
            CostSortIndicator.Visibility = Visibility.Collapsed;
            PnlSortIndicator.Visibility = Visibility.Collapsed;
            Pnl1hSortIndicator.Visibility = Visibility.Collapsed;
            Pnl24hSortIndicator.Visibility = Visibility.Collapsed;
            RealizedSortIndicator.Visibility = Visibility.Collapsed;

            // Show active indicator
            TextBlock? activeIndicator = null;
            switch (activeColumn)
            {
                case "Symbol":
                    activeIndicator = SymbolSortIndicator;
                    break;
                case "Cost":
                    activeIndicator = CostSortIndicator;
                    break;
                case "PnL":
                    activeIndicator = PnlSortIndicator;
                    break;
                case "Pnl1h":
                    activeIndicator = Pnl1hSortIndicator;
                    break;
                case "Pnl24h":
                    activeIndicator = Pnl24hSortIndicator;
                    break;
                case "Realized":
                    activeIndicator = RealizedSortIndicator;
                    break;
            }

            if (activeIndicator != null)
            {
                activeIndicator.Visibility = Visibility.Visible;
                activeIndicator.Text = _sortingManager.IsAscending ? "▲" : "▼";
            }
        }

        // Методы для обработки данных от DataManager
        public void OnDataUpdated(BybitDataResult result)
        {
            Dispatcher.Invoke(() =>
            {
                if (result.Success)
                {
                    _uiManager.UpdateBalanceData(result.BalanceData);
                    _uiManager.ClearConnectionStatus(); // Очищаем статус подключения при успешной загрузке
                }
                else
                {
                    // Если это сетевые ошибки, показываем их в отдельной строке
                    if (result.ErrorMessage == "Отсутствует подключение!")
                    {
                        // Показываем последние известные данные баланса (если они есть)
                        _uiManager.UpdateBalanceData(result.BalanceData);
                        _uiManager.UpdateBalanceDataWithError(result.ErrorMessage); // Показываем статус подключения
                    }
                    else
                    {
                        // Для других ошибок используем стандартную обработку
                        _uiManager.UpdateBalanceData(result.BalanceData);
                        _uiManager.ClearConnectionStatus();
                    }
                }
                _uiManager.UpdatePositions();
            });
        }

        public void OnError(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                // Преобразуем технические ошибки в понятные пользователю сообщения
                string userFriendlyMessage = ConvertToUserFriendlyMessage(errorMessage);
                
                // Определяем, является ли ошибка критической
                bool isCriticalError = IsCriticalError(errorMessage);
                
                // Показываем ошибку в отдельном окне
                MessageBox.Show(userFriendlyMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Если ошибка критическая, закрываем приложение
                if (isCriticalError)
                {
                    // Останавливаем таймер обновления
                    _dataManager.StopTimer();
                    Application.Current.Shutdown();
                }
                else
                {
                    // Очищаем интерфейс от предыдущих ошибок
                    _uiManager.ClearErrorDisplay();
                }
            });
        }

        private bool IsCriticalError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            // Критические ошибки, при которых нужно закрыть приложение
            return errorMessage.Contains("timestamp") || 
                   errorMessage.Contains("recv_window") ||
                   errorMessage.Contains("invalid api") ||
                   errorMessage.Contains("api key") ||
                   errorMessage.Contains("permission") ||
                   errorMessage.Contains("access");
        }

        private string ConvertToUserFriendlyMessage(string technicalError)
        {
            if (string.IsNullOrEmpty(technicalError))
                return "Произошла неизвестная ошибка";

            // Ошибки связанные с временными метками
            if (technicalError.Contains("timestamp") || technicalError.Contains("recv_window"))
            {
                return "⚠️ Ошибка синхронизации времени\n\nСинхронизируйте время на компьютере и перезапустите приложение.\n\nДля синхронизации времени:\n1. Откройте Параметры Windows\n2. Перейдите в 'Время и язык' → 'Дата и время'\n3. Нажмите 'Синхронизировать сейчас'";
            }

            // Ошибки API ключей
            if (technicalError.Contains("invalid api") || technicalError.Contains("api key"))
            {
                return "🔑 Неверные API ключи\n\nПроверьте настройки API в меню приложения.";
            }

            // Ошибки сети
            if (technicalError.Contains("network") || technicalError.Contains("connection") || technicalError.Contains("timeout"))
            {
                return "🌐 Ошибка подключения\n\nПроверьте интернет-соединение и попробуйте снова.";
            }

            // Ошибки доступа
            if (technicalError.Contains("permission") || technicalError.Contains("access"))
            {
                return "🚫 Ошибка доступа\n\nПроверьте права API ключей на Bybit.";
            }

            // Ошибки лимитов
            if (technicalError.Contains("rate limit") || technicalError.Contains("too many requests"))
            {
                return "⏱️ Превышен лимит запросов\n\nПодождите немного и попробуйте снова.";
            }

            // Ошибки аккаунта
            if (technicalError.Contains("unified account") || technicalError.Contains("account not found"))
            {
                return "💼 Unified Account не найден\n\nПроверьте настройки аккаунта на Bybit.";
            }

            // Общие ошибки
            return $"❌ Ошибка: {technicalError}";
        }
    }
}