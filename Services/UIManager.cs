using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;

namespace CryptoPnLWidget.Services
{
    public class UIManager
    {
        public static event Action<string>? OnGlobalError;
        
        public static void RaiseGlobalError(string errorMessage)
        {
            OnGlobalError?.Invoke(errorMessage);
        }
        
        private readonly StackPanel _positionsPanel;
        private readonly TextBlock _marginBalanceTextBlock;
        private readonly TextBlock _availableBalanceTextBlock;
        private readonly TextBlock _connectionStatusTextBlock;
        private readonly Dictionary<string, Grid> _positionGrids = new Dictionary<string, Grid>();
        private readonly SortingManager _sortingManager;
        private readonly PositionManager _positionManager;
        private readonly ThemeManager _themeManager;

        public UIManager(
            StackPanel positionsPanel,
            TextBlock marginBalanceTextBlock,
            TextBlock availableBalanceTextBlock,
            TextBlock connectionStatusTextBlock,
            SortingManager sortingManager,
            PositionManager positionManager,
            ThemeManager themeManager)
        {
            _positionsPanel = positionsPanel;
            _marginBalanceTextBlock = marginBalanceTextBlock;
            _availableBalanceTextBlock = availableBalanceTextBlock;
            _connectionStatusTextBlock = connectionStatusTextBlock;
            _sortingManager = sortingManager;
            _positionManager = positionManager;
            _themeManager = themeManager;
        }

        public void UpdateBalanceData(BybitBalanceData? balanceData)
        {
            if (balanceData != null)
            {
                if (balanceData.HasUsdtAsset)
                {
                    _marginBalanceTextBlock.Text = balanceData.TotalMarginBalance?.ToString("F2");
                    _availableBalanceTextBlock.Text = balanceData.AvailableBalance?.ToString("F2");
                }
                else
                {
                    _marginBalanceTextBlock.Text = "💵 USDT актив не найден";
                    _availableBalanceTextBlock.Text = "Пополните аккаунт USDT";
                }
            }
            else
            {
                _marginBalanceTextBlock.Text = "Загрузка...";
                _availableBalanceTextBlock.Text = "";
            }
        }

        public void UpdateBalanceDataWithError(string errorMessage)
        {
            if (errorMessage == "Отсутствует подключение!")
            {
                _connectionStatusTextBlock.Text = "🌐 Отсутствует подключение!";
                _connectionStatusTextBlock.Visibility = Visibility.Visible;
                _connectionStatusTextBlock.Foreground = _themeManager.GetErrorColor();
            }
            else
            {
                _connectionStatusTextBlock.Text = "";
                _connectionStatusTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowError(string errorMessage)
        {
            // Преобразуем технические ошибки в понятные пользователю сообщения
            string userFriendlyMessage = ConvertToUserFriendlyMessage(errorMessage);
            
            _marginBalanceTextBlock.Text = userFriendlyMessage;
            _availableBalanceTextBlock.Text = "";
            ClearPositionsPanelAndShowMessage(userFriendlyMessage, Brushes.Red);
        }

        private string ConvertToUserFriendlyMessage(string technicalError)
        {
            if (string.IsNullOrEmpty(technicalError))
                return "Произошла неизвестная ошибка";

            // Ошибки связанные с временными метками
            if (technicalError.Contains("timestamp") || technicalError.Contains("recv_window"))
            {
                return "⚠️ Ошибка синхронизации времени\nСинхронизируйте время на компьютере\nи перезапустите приложение";
            }

            // Ошибки API ключей
            if (technicalError.Contains("invalid api") || technicalError.Contains("api key"))
            {
                return "🔑 Неверные API ключи\nПроверьте настройки API в меню";
            }

            // Ошибки сети
            if (technicalError.Contains("network") || technicalError.Contains("connection") || technicalError.Contains("timeout"))
            {
                return "🌐 Ошибка подключения\nПроверьте интернет-соединение";
            }

            // Ошибки доступа
            if (technicalError.Contains("permission") || technicalError.Contains("access"))
            {
                return "🚫 Ошибка доступа\nПроверьте права API ключей";
            }

            // Ошибки лимитов
            if (technicalError.Contains("rate limit") || technicalError.Contains("too many requests"))
            {
                return "⏱️ Превышен лимит запросов\nПодождите немного и попробуйте снова";
            }

            // Ошибки аккаунта
            if (technicalError.Contains("unified account") || technicalError.Contains("account not found"))
            {
                return "💼 Unified Account не найден\nПроверьте настройки аккаунта на Bybit";
            }

            // Общие ошибки
            return $"❌ Ошибка: {technicalError}";
        }

        public void UpdatePositions()
        {
            try
            {
                var shortTermPositions = _positionManager.GetShortTermPositions().ToList();
                var longTermPositions = _positionManager.GetLongTermPositions().ToList();

                var sortedShortTerm = _sortingManager.SortPositions(shortTermPositions);
                var sortedLongTerm = _sortingManager.SortPositions(longTermPositions);

                UpdatePositionsPanel(sortedShortTerm, sortedLongTerm);
            }
            catch (Exception ex)
            {
                ClearPositionsPanelAndShowMessage($"Ошибка при обновлении позиций: {ConvertToUserFriendlyMessage(ex.Message)}", _themeManager.GetRedColor());
            }
        }

        private void ClearPositionsPanelAndShowMessage(string message, Brush color)
        {
            _positionsPanel.Children.Clear();
            _positionsPanel.Children.Add(new TextBlock
            {
                Text = message,
                FontStyle = FontStyles.Italic,
                Foreground = color,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
        }

        private void UpdatePositionsPanel(List<PositionManager.PositionHistoryTracker> sortedShortTerm, List<PositionManager.PositionHistoryTracker> sortedLongTerm)
        {
            // Очищаем старые Grid'ы, которые больше не используются
            var currentSymbols = sortedShortTerm.Concat(sortedLongTerm).Select(t => t.CurrentPosition?.Symbol).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
            var symbolsToRemove = _positionGrids.Keys.Except(currentSymbols).ToList();
            foreach (var symbol in symbolsToRemove)
            {
                if (!string.IsNullOrEmpty(symbol))
                {
                    _positionGrids.Remove(symbol);
                }
            }

            List<UIElement> newChildrenOrder = new List<UIElement>();

            // Добавляем заголовок для краткосрочных позиций
            if (sortedShortTerm.Any())
            {
                newChildrenOrder.Add(CreateSectionHeader("Краткосрочные позиции"));
                
                foreach (var tracker in sortedShortTerm)
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

                // Добавляем итоговую строку для краткосрочных позиций
                newChildrenOrder.Add(CreateTotalRow(sortedShortTerm));
            }

            // Добавляем заголовок для долгосрочных позиций
            if (sortedLongTerm.Any())
            {
                newChildrenOrder.Add(CreateSectionHeader("Долгосрочные позиции"));
                
                foreach (var tracker in sortedLongTerm)
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

                // Добавляем итоговую строку для долгосрочных позиций
                newChildrenOrder.Add(CreateTotalRow(sortedLongTerm));
            }

            _positionsPanel.Children.Clear();
            foreach (var child in newChildrenOrder)
            {
                _positionsPanel.Children.Add(child);
            }

            if (!sortedShortTerm.Any() && !sortedLongTerm.Any() && !_positionGrids.Any())
            {
                _positionsPanel.Children.Add(new TextBlock
                {
                    Name = "NoPositionsMessage",
                    Text = "📊 Нет открытых позиций",
                    FontStyle = FontStyles.Italic,
                    Foreground = _themeManager.GetFontColor(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }

        private TextBlock CreateSectionHeader(string title)
        {
            return new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = _themeManager.GetHeaderFontSize(),
                Foreground = _themeManager.GetFontColor(),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 15, 0, 5)
            };
        }

        private Grid CreatePositionGridAndChildren()
        {
            Grid positionGrid = new Grid();
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            positionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            positionGrid.Margin = new Thickness(0, 3, 0, 3);

            positionGrid.MouseLeftButtonDown += PositionRow_MouseLeftButtonDown;
            positionGrid.Cursor = Cursors.Hand;

            // Создаем TextBlock'и и присваиваем им Tag для удобства поиска
            positionGrid.Children.Add(new TextBlock { Tag = "Symbol", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Cost", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "PnL", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl1h", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl24h", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Realized", Foreground = _themeManager.GetFontColor(), HorizontalAlignment = HorizontalAlignment.Center });
            
            // Создаем CheckBox для Hold
            var holdCheckBox = new CheckBox
            {
                Tag = "Hold",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            holdCheckBox.Checked += HoldCheckBox_Changed;
            holdCheckBox.Unchecked += HoldCheckBox_Changed;
            positionGrid.Children.Add(holdCheckBox);

            // Устанавливаем Column для каждого элемента
            for (int i = 0; i < positionGrid.Children.Count; i++)
            {
                Grid.SetColumn(positionGrid.Children[i], i);
            }

            return positionGrid;
        }

        private void HoldCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Parent is Grid positionGrid && positionGrid.Tag is PositionManager.PositionHistoryTracker tracker)
            {
                var symbol = tracker.CurrentPosition?.Symbol;
                if (!string.IsNullOrEmpty(symbol))
                {
                    _positionManager.SetHoldPosition(symbol, checkBox.IsChecked ?? false);
                    
                    // Немедленно обновляем UI для отображения изменений
                    UpdatePositions();
                }
            }
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
                            RaiseGlobalError($"Не удалось открыть ссылку: {ex.Message}");
                        }
                    }
                }
            }
        }

        private string FormatPnl(decimal? pnlValue)
        {
            if (!pnlValue.HasValue)
                return "N/A";

            var absValue = Math.Abs(pnlValue.Value);
            
            // Если значение больше или равно 100, показываем только целые числа
            if (absValue >= 100)
            {
                return pnlValue.Value.ToString("F0");
            }
            else
            {
                // Для значений меньше 100 показываем с центами
                return pnlValue.Value.ToString("F2");
            }
        }

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
            var holdCheckBox = positionGrid.Children.OfType<CheckBox>().FirstOrDefault(cb => cb.Tag?.ToString() == "Hold");

            if (symbolBlock != null)
            {
                // Убираем USDT из названия символа
                string displaySymbol = position?.Symbol?.Replace("USDT", "") ?? string.Empty;
                symbolBlock.Text = displaySymbol;
                symbolBlock.FontSize = _themeManager.GetContentFontSize();
                symbolBlock.FontWeight = FontWeights.Bold;
                symbolBlock.Foreground = _themeManager.GetFontColor();
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
                costBlock.FontSize = _themeManager.GetContentFontSize();
                costBlock.Foreground = _themeManager.GetFontColor();
            }

            if (position != null && pnlBlock != null)
            {
                pnlBlock.Text = FormatPnl(position.UnrealizedPnl);
                pnlBlock.Foreground = GetPnlColor(position.UnrealizedPnl);
                pnlBlock.FontWeight = FontWeights.Bold;
                pnlBlock.FontSize = _themeManager.GetContentFontSize();
            }

            if (pnl1hBlock != null)
            {
                decimal? pnl1hChange = tracker.GetPnlChange(TimeSpan.FromHours(1), tracker.GetCurrentPosition());
                pnl1hBlock.Text = FormatPnl(pnl1hChange);
                pnl1hBlock.Foreground = GetPnlColor(pnl1hChange);
                pnl1hBlock.FontSize = _themeManager.GetContentFontSize();
            }

            if (pnl24hBlock != null)
            {
                decimal? pnl24hChange = tracker.GetPnlChange(TimeSpan.FromHours(24), tracker.GetCurrentPosition());
                pnl24hBlock.Text = FormatPnl(pnl24hChange);
                pnl24hBlock.Foreground = GetPnlColor(pnl24hChange);
                pnl24hBlock.FontSize = _themeManager.GetContentFontSize();
            }

            if (realizedPnlBlock != null)
            {
                realizedPnlBlock.Text = FormatPnl(position?.RealizedPnl);
                realizedPnlBlock.Foreground = GetPnlColor(position?.RealizedPnl);
                realizedPnlBlock.FontSize = _themeManager.GetContentFontSize();
            }

            if (holdCheckBox != null)
            {
                holdCheckBox.IsChecked = tracker.IsHold;
            }
        }

        private Brush GetPnlColor(decimal? pnlValue)
        {
            if (!pnlValue.HasValue)
                return _themeManager.GetFontColor(); // Цвет для "N/A" или "--"

            if (pnlValue > 0)
                return _themeManager.GetGreenColor(); // Положительный PnL
            else if (pnlValue < 0)
                return _themeManager.GetRedColor();   // Отрицательный PnL
            else
                return _themeManager.GetFontColor(); // Нулевой PnL
        }

        private Grid CreateTotalRow(List<PositionManager.PositionHistoryTracker> positions)
        {
            Grid totalGrid = new Grid();
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalGrid.Margin = new Thickness(0, 8, 0, 8);

            // Вычисляем сумму только PnL
            decimal totalPnl = 0;

            foreach (var tracker in positions)
            {
                if (tracker.CurrentPosition?.UnrealizedPnl.HasValue == true)
                {
                    totalPnl += tracker.CurrentPosition.UnrealizedPnl.Value;
                }
            }

            // Создаем элементы строки
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = "Итого", 
                FontWeight = FontWeights.Bold,
                FontSize = _themeManager.GetContentFontSize(),
                Foreground = _themeManager.GetFontColor(), 
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 0, 0, 0)
            });
            
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = "", 
                Foreground = _themeManager.GetFontColor(), 
                HorizontalAlignment = HorizontalAlignment.Center 
            });
            
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = FormatPnl(totalPnl), 
                FontWeight = FontWeights.Bold,
                FontSize = _themeManager.GetContentFontSize(),
                Foreground = GetPnlColor(totalPnl), 
                HorizontalAlignment = HorizontalAlignment.Center 
            });
            
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = "", 
                Foreground = _themeManager.GetFontColor(), 
                HorizontalAlignment = HorizontalAlignment.Center 
            });
            
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = "", 
                Foreground = _themeManager.GetFontColor(), 
                HorizontalAlignment = HorizontalAlignment.Center 
            });
            
            totalGrid.Children.Add(new TextBlock 
            { 
                Text = "", 
                Foreground = _themeManager.GetFontColor(), 
                HorizontalAlignment = HorizontalAlignment.Center 
            });

            // Устанавливаем Column для каждого элемента
            for (int i = 0; i < totalGrid.Children.Count; i++)
            {
                Grid.SetColumn(totalGrid.Children[i], i);
            }

            return totalGrid;
        }

        public void ClearErrorDisplay()
        {
            // Очищаем сообщения об ошибках, возвращаем к нормальному состоянию
            _marginBalanceTextBlock.Text = "Загрузка...";
            _availableBalanceTextBlock.Text = "";
            
            // Очищаем сообщения в панели позиций
            var noPositionsMessage = _positionsPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "NoPositionsMessage");
            if (noPositionsMessage != null)
            {
                _positionsPanel.Children.Remove(noPositionsMessage);
            }
        }

        public void ClearConnectionStatus()
        {
            _connectionStatusTextBlock.Text = "";
            _connectionStatusTextBlock.Visibility = Visibility.Collapsed;
            _connectionStatusTextBlock.Foreground = _themeManager.GetFontColor();
        }
    }
} 