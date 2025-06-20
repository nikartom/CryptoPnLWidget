using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using CryptoPnLWidget.API;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;
using System.Windows.Media;

namespace CryptoPnLWidget
{
    public partial class MainWindow : Window
    {
        private readonly ExchangeKeysManager _keysManager;
        private readonly BybitService _bybitService;
        private readonly PositionManager _positionManager;
        private System.Windows.Threading.DispatcherTimer _updateTimer = new System.Windows.Threading.DispatcherTimer();
        private string _currentSortColumn = "PnL";
        private bool _isAscending = false;
        private System.Windows.Forms.NotifyIcon? _trayIcon;

        // --- ДОБАВЛЕНО/ИЗМЕНЕНО: Словарь для отслеживания Grid-элементов по символу позиции ---
        private Dictionary<string, Grid> _positionGrids = new Dictionary<string, Grid>();

        public MainWindow(ExchangeKeysManager keysManager, BybitService bybitService, PositionManager positionManager)
        {
            InitializeComponent();
            _keysManager = keysManager;
            _bybitService = bybitService;
            _positionManager = positionManager;
            this.Loaded += MainWindow_Loaded;
            // Установка начального индикатора сортировки
            UpdateSortIndicators(_currentSortColumn);

            // Initialize tray icon
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();

            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("CryptoPnLWidget.vdhyq-ubma4-001.ico"))
                {
                    if (stream != null)
                        _trayIcon.Icon = new System.Drawing.Icon(stream);
                    else
                        throw new Exception("Не удалось найти ресурс иконки в сборке");
                }
            }
            catch (Exception ex)
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                System.Windows.MessageBox.Show($"Ошибка при загрузке иконки: {ex.Message}. Используется стандартная иконка.", "Ошибка иконки", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            _trayIcon.Text = "Crypto PnL Widget";
            _trayIcon.Visible = true;

            // Create context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) =>
            {
                _trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };

            contextMenu.Items.Add(exitItem);
            _trayIcon.ContextMenuStrip = contextMenu;

            // Handle single click on tray icon
            _trayIcon.Click += (s, e) =>
            {
                // Проверяем, что это левый клик, чтобы избежать конфликтов с контекстным меню
                if (((System.Windows.Forms.MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (this.Visibility == Visibility.Hidden)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                    }
                    else
                    {
                        this.Hide();
                    }
                }
            };
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
            if (_trayIcon != null)
                _trayIcon.Visible = false;
            base.OnClosing(e);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            if (!_keysManager.HasKeysForExchange("Bybit"))
            {
                var settingsWindow = new ApiSettingsWindow(_keysManager);
                bool? dialogResult = settingsWindow.ShowDialog();

                if (dialogResult == true)
                {
                    ConfigureBybitClientAndLoadData();
                }
                else
                {
                    System.Windows.MessageBox.Show("API ключи не были предоставлены. Приложение будет закрыто.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Windows.Application.Current.Shutdown();
                }
            }
            else
            {
                ConfigureBybitClientAndLoadData();
            }
        }

        private void ConfigureBybitClientAndLoadData()
        {
            var keys = _keysManager.GetKeysForExchange("Bybit");

            if (keys == null || string.IsNullOrEmpty(keys.ApiKey) || string.IsNullOrEmpty(keys.ApiSecret))
            {
                System.Windows.MessageBox.Show("Ошибка при загрузке API ключей после сохранения. Проверьте сохраненные данные.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            _bybitService.SetApiCredentials(keys.ApiKey, keys.ApiSecret);

            // Первый, немедленный вызов при запуске
            _ = LoadBybitData();

            _updateTimer.Interval = TimeSpan.FromSeconds(5);
            _updateTimer.Tick += async (s, args) => await LoadBybitData();
            _updateTimer.Start();
        }

        private async Task LoadBybitData()
        {
            try
            {
                var result = await _bybitService.LoadBybitDataAsync();

                if (result.Success)
                {
                    // Обновляем балансы
                    if (result.BalanceData != null)
                    {
                        if (result.BalanceData.HasUsdtAsset)
                        {
                            if (MarginBalanceTextBlock != null)
                                MarginBalanceTextBlock.Text = result.BalanceData.TotalMarginBalance?.ToString("F2") + " USDT";
                            if (AvailableBalanceTextBlock != null)
                                AvailableBalanceTextBlock.Text = result.BalanceData.AvailableBalance?.ToString("F2") + " USD";
                        }
                        else
                        {
                            if (MarginBalanceTextBlock != null) MarginBalanceTextBlock.Text = "USDT актив не найден.";
                            if (AvailableBalanceTextBlock != null) AvailableBalanceTextBlock.Text = "";
                        }
                    }
                    else
                    {
                        if (MarginBalanceTextBlock != null) MarginBalanceTextBlock.Text = "Баланс Unified Account не найден.";
                        if (AvailableBalanceTextBlock != null) AvailableBalanceTextBlock.Text = "";
                    }

                    // Обновляем позиции
                    if (result.Positions != null)
                    {
                        _positionManager.UpdatePositions(result.Positions);

                        Dispatcher.Invoke(() =>
                        {
                            SortAndUpdatePositions();
                        });
                    }
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (MarginBalanceTextBlock != null) MarginBalanceTextBlock.Text = $"Ошибка: {result.ErrorMessage}";
                        if (AvailableBalanceTextBlock != null) AvailableBalanceTextBlock.Text = "";
                        ClearPositionsPanelAndShowMessage($"Ошибка: {result.ErrorMessage}", System.Windows.Media.Brushes.Red);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    if (MarginBalanceTextBlock != null) MarginBalanceTextBlock.Text = $"Общая ошибка: {ex.Message}";
                    if (AvailableBalanceTextBlock != null) AvailableBalanceTextBlock.Text = "";
                    ClearPositionsPanelAndShowMessage($"Общая ошибка: {ex.Message}", System.Windows.Media.Brushes.Red);
                });
            }
        }

        // --- ДОБАВЛЕНО: Вспомогательный метод для очистки панели и вывода сообщения ---
        private void ClearPositionsPanelAndShowMessage(string message, Brush color)
        {
            PositionsPanel.Children.Clear();
            _positionGrids.Clear(); // Важно очистить словарь отслеживаемых Grid
            PositionsPanel.Children.Add(new TextBlock
            {
                Name = "NoPositionsMessage", // Используем то же имя для единообразия
                Text = message,
                FontStyle = FontStyles.Italic,
                Foreground = color,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
        }

        private void PositionRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid clickedGrid)
            {
                if (clickedGrid.Tag is PositionManager.PositionHistoryTracker tracker)
                {
                    var symbol = tracker.CurrentPosition?.Symbol;
                    if (!string.IsNullOrEmpty(symbol))
                    {
                        // Используем полное название символа (с USDT) для URL
                        string url = $"https://www.bybit.com/trade/usdt/{symbol}";
                        try
                        {
                            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        // --- ДОБАВЛЕНО: Новый метод для создания Grid и его TextBlocks с Tag ---
        private Grid CreatePositionGridAndChildren()
        {
            Grid positionGrid = new Grid();
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.Margin = UiConstants.PositionGridMargin;

            positionGrid.MouseLeftButtonDown += PositionRow_MouseLeftButtonDown;
            positionGrid.Cursor = System.Windows.Input.Cursors.Hand;

            // Создаем TextBlock'и и присваиваем им Tag для удобства поиска
            positionGrid.Children.Add(new TextBlock { Tag = "Symbol", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Cost", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "PnL", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl1h", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl24h", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Realized", Foreground = UiConstants.FontColor, HorizontalAlignment = System.Windows.HorizontalAlignment.Center });

            // Устанавливаем Column для каждого TextBlock
            for (int i = 0; i < positionGrid.Children.Count; i++)
            {
                Grid.SetColumn(positionGrid.Children[i], i);
            }

            return positionGrid;
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
                // Безопасное приведение к строке с дефолтным значением
                string columnName = button.Tag as string ?? "";

                // Toggle sort direction if clicking the same column
                if (columnName == _currentSortColumn)
                {
                    _isAscending = !_isAscending;
                }
                else
                {
                    _currentSortColumn = columnName;
                    _isAscending = true;
                }

                // Update sort indicators
                UpdateSortIndicators(columnName);

                // Sort and update the positions
                SortAndUpdatePositions();
            }
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
                activeIndicator.Text = _isAscending ? "▲" : "▼";
            }
        }

        private void SortAndUpdatePositions()
        {
            var openPositionTrackers = _positionManager.GetActivePositionTrackers().ToList();

            // Если нет открытых позиций, просто показываем сообщение
            if (!openPositionTrackers.Any())
            {
                UpdatePositionsPanel(new List<PositionManager.PositionHistoryTracker>()); // Передаем пустой список
                return;
            }

            // Выполняем сортировку
            var sortedTrackers = (_currentSortColumn switch
            {
                "Symbol" => _isAscending
                    ? openPositionTrackers.OrderBy(t => t.CurrentPosition?.Symbol, StringComparer.Ordinal)
                    : openPositionTrackers.OrderByDescending(t => t.CurrentPosition?.Symbol, StringComparer.Ordinal),
                "Cost" => _isAscending
                    ? openPositionTrackers.OrderBy(t => (t.CurrentPosition?.Quantity * (t.CurrentPosition?.AveragePrice ?? 0)))
                    : openPositionTrackers.OrderByDescending(t => (t.CurrentPosition?.Quantity * (t.CurrentPosition?.AveragePrice ?? 0))),
                "PnL" => _isAscending
                    ? openPositionTrackers.OrderBy(t => t.CurrentPosition?.UnrealizedPnl ?? 0)
                    : openPositionTrackers.OrderByDescending(t => t.CurrentPosition?.UnrealizedPnl ?? 0),
                "Pnl1h" => _isAscending
                    ? openPositionTrackers.OrderBy(t => t.GetPnlChange(TimeSpan.FromHours(1), t.GetCurrentPosition()) ?? 0)
                    : openPositionTrackers.OrderByDescending(t => t.GetPnlChange(TimeSpan.FromHours(1), t.GetCurrentPosition()) ?? 0),
                "Pnl24h" => _isAscending
                    ? openPositionTrackers.OrderBy(t => t.GetPnlChange(TimeSpan.FromHours(24), t.GetCurrentPosition()) ?? 0)
                    : openPositionTrackers.OrderByDescending(t => t.GetPnlChange(TimeSpan.FromHours(24), t.GetCurrentPosition()) ?? 0),
                "Realized" => _isAscending
                    ? openPositionTrackers.OrderBy(t => t.CurrentPosition?.RealizedPnl ?? 0)
                    : openPositionTrackers.OrderByDescending(t => t.CurrentPosition?.RealizedPnl ?? 0),
                // ! ИСПРАВЛЕНО CS8506: Возвращаем IOrderedEnumerable для дефолтного случая !
                _ => openPositionTrackers.OrderBy(t => 0) // Просто для того, чтобы тип был IOrderedEnumerable
            }).ToList(); // <-- ToList() здесь, чтобы получить список для UpdatePositionsPanel

            UpdatePositionsPanel(sortedTrackers);
        }

        // --- ОБНОВЛЕНО/ИЗМЕНЕНО: Метод для обновления панели позиций, теперь с учетом плавности и сортировки ---
        private void UpdatePositionsPanel(List<PositionManager.PositionHistoryTracker> sortedTrackers)
        {
            var noPositionsMessage = PositionsPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "NoPositionsMessage");
            if (noPositionsMessage != null)
            {
                PositionsPanel.Children.Remove(noPositionsMessage);
            }

            HashSet<string> symbolsToDisplay = new HashSet<string>();
            foreach (var tracker in sortedTrackers)
            {
                if (tracker.CurrentPosition?.Symbol != null)
                {
                    symbolsToDisplay.Add(tracker.CurrentPosition.Symbol);
                }
            }

            List<string> symbolsToRemove = new List<string>();
            foreach (var symbolInUi in _positionGrids.Keys)
            {
                if (!symbolsToDisplay.Contains(symbolInUi))
                {
                    if (_positionGrids.TryGetValue(symbolInUi, out Grid? gridToRemove))
                    {
                        PositionsPanel.Children.Remove(gridToRemove);
                        symbolsToRemove.Add(symbolInUi);
                    }
                }
            }
            foreach (var symbol in symbolsToRemove)
            {
                _positionGrids.Remove(symbol);
            }

            List<UIElement> newChildrenOrder = new List<UIElement>();
            foreach (var tracker in sortedTrackers)
            {
                var position = tracker.CurrentPosition;
                if (position == null || string.IsNullOrEmpty(position.Symbol)) continue;

                Grid? positionGrid;
                if (!_positionGrids.TryGetValue(position.Symbol, out positionGrid))
                {
                    positionGrid = CreatePositionGridAndChildren();
                    _positionGrids[position.Symbol] = positionGrid;
                }
                newChildrenOrder.Add(positionGrid);

                UpdatePositionGridContent(positionGrid, tracker);
            }

            PositionsPanel.Children.Clear();
            foreach (var child in newChildrenOrder)
            {
                PositionsPanel.Children.Add(child);
            }

            if (!sortedTrackers.Any() && !_positionGrids.Any())
            {
                PositionsPanel.Children.Add(new TextBlock
                {
                    Name = "NoPositionsMessage",
                    Text = "Нет открытых позиций.",
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }

        // --- ДОБАВЛЕНО: Метод для обновления содержимого TextBlocks внутри Grid ---
        private void UpdatePositionGridContent(Grid positionGrid, PositionManager.PositionHistoryTracker tracker)
        {
            var position = tracker.CurrentPosition;

            positionGrid.Tag = tracker;

            var symbolBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "Symbol");
            var costBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "Cost");
            var pnlBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "PnL");
            var pnl1hBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "Pnl1h");
            var pnl24hBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "Pnl24h");
            var realizedPnlBlock = positionGrid.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Tag?.ToString() == "Realized");

            if (symbolBlock != null)
            {
                // Убираем USDT из названия символа
                string displaySymbol = position?.Symbol?.Replace("USDT", "") ?? string.Empty;
                symbolBlock.Text = displaySymbol;
                symbolBlock.FontSize = UiConstants.FontSizeSmall;
                symbolBlock.FontWeight = UiConstants.FontWeightBold;
                symbolBlock.Foreground = UiConstants.FontColor;
            }

            string costText = "N/A";
            if (position != null && position.AveragePrice.HasValue)
            {
                decimal totalCost = position.Quantity * position.AveragePrice.Value;
                costText = totalCost.ToString("F2");
            }        
            if (costBlock != null) 
            {
                costBlock.Text = costText;
                costBlock.Foreground = UiConstants.FontColor;
            }

            if (position != null && pnlBlock != null)
            {
                pnlBlock.Text = position.UnrealizedPnl?.ToString("F2") ?? "N/A";
                pnlBlock.Foreground = GetPnlColor(position.UnrealizedPnl);
                pnlBlock.FontWeight = UiConstants.FontWeightBold;
            }

            decimal? pnl1hChange = tracker.GetPnlChange(TimeSpan.FromHours(1), tracker.GetCurrentPosition());
            if (pnl1hBlock != null)
            {
                pnl1hBlock.Text = pnl1hChange?.ToString("F2") ?? "--";
                pnl1hBlock.Foreground = GetPnlColor(pnl1hChange);
            }

            decimal? pnl24hChange = tracker.GetPnlChange(TimeSpan.FromHours(24), tracker.GetCurrentPosition());
            if (pnl24hBlock != null)
            {
                pnl24hBlock.Text = pnl24hChange?.ToString("F2") ?? "--";
                pnl24hBlock.Foreground = GetPnlColor(pnl24hChange);
            }

            if (position != null && realizedPnlBlock != null)
            {
                realizedPnlBlock.Text = position.RealizedPnl?.ToString("F2") ?? "N/A";
                realizedPnlBlock.Foreground = GetPnlColor(position.RealizedPnl);
            }
        }

        // --- ДОБАВЛЕНО: Вспомогательный метод для определения цвета PnL ---
        private Brush GetPnlColor(decimal? pnlValue)
        {
            if (!pnlValue.HasValue)
                return UiConstants.FontColor; // Цвет для "N/A" или "--"

            if (pnlValue > 0)
                return UiConstants.ForegroundGreen; // Положительный PnL
            else if (pnlValue < 0)
                return UiConstants.ForegroundRed;   // Отрицательный PnL
            else
                return UiConstants.FontColor; // Нулевой PnL
        }
    }
}