using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using CryptoPnLWidget.Services;
using CryptoPnLWidget.Services.Bybit;

namespace CryptoPnLWidget.Services
{
    public class UIManager
    {
        private readonly StackPanel _positionsPanel;
        private readonly TextBlock _marginBalanceTextBlock;
        private readonly TextBlock _availableBalanceTextBlock;
        private readonly Dictionary<string, Grid> _positionGrids = new Dictionary<string, Grid>();
        private readonly SortingManager _sortingManager;
        private readonly PositionManager _positionManager;

        public UIManager(
            StackPanel positionsPanel,
            TextBlock marginBalanceTextBlock,
            TextBlock availableBalanceTextBlock,
            SortingManager sortingManager,
            PositionManager positionManager)
        {
            _positionsPanel = positionsPanel;
            _marginBalanceTextBlock = marginBalanceTextBlock;
            _availableBalanceTextBlock = availableBalanceTextBlock;
            _sortingManager = sortingManager;
            _positionManager = positionManager;
        }

        public void UpdateBalanceData(BybitBalanceData? balanceData)
        {
            if (balanceData != null)
            {
                if (balanceData.HasUsdtAsset)
                {
                    _marginBalanceTextBlock.Text = balanceData.TotalMarginBalance?.ToString("F2") + " USDT";
                    _availableBalanceTextBlock.Text = balanceData.AvailableBalance?.ToString("F2") + " USD";
                }
                else
                {
                    _marginBalanceTextBlock.Text = "USDT актив не найден.";
                    _availableBalanceTextBlock.Text = "";
                }
            }
            else
            {
                _marginBalanceTextBlock.Text = "Баланс Unified Account не найден.";
                _availableBalanceTextBlock.Text = "";
            }
        }

        public void ShowError(string errorMessage)
        {
            _marginBalanceTextBlock.Text = errorMessage;
            _availableBalanceTextBlock.Text = "";
            ClearPositionsPanelAndShowMessage(errorMessage, Brushes.Red);
        }

        public void UpdatePositions()
        {
            var sortedTrackers = _sortingManager.SortPositions(_positionManager.GetActivePositionTrackers());
            UpdatePositionsPanel(sortedTrackers);
        }

        private void ClearPositionsPanelAndShowMessage(string message, Brush color)
        {
            _positionsPanel.Children.Clear();
            _positionGrids.Clear();
            _positionsPanel.Children.Add(new TextBlock
            {
                Name = "NoPositionsMessage",
                Text = message,
                FontStyle = FontStyles.Italic,
                Foreground = color,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            });
        }

        private void UpdatePositionsPanel(List<PositionManager.PositionHistoryTracker> sortedTrackers)
        {
            var noPositionsMessage = _positionsPanel.Children.OfType<TextBlock>().FirstOrDefault(tb => tb.Name == "NoPositionsMessage");
            if (noPositionsMessage != null)
            {
                _positionsPanel.Children.Remove(noPositionsMessage);
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
                        _positionsPanel.Children.Remove(gridToRemove);
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

            _positionsPanel.Children.Clear();
            foreach (var child in newChildrenOrder)
            {
                _positionsPanel.Children.Add(child);
            }

            if (!sortedTrackers.Any() && !_positionGrids.Any())
            {
                _positionsPanel.Children.Add(new TextBlock
                {
                    Name = "NoPositionsMessage",
                    Text = "Нет открытых позиций.",
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }
        }

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
            positionGrid.Cursor = Cursors.Hand;

            // Создаем TextBlock'и и присваиваем им Tag для удобства поиска
            positionGrid.Children.Add(new TextBlock { Tag = "Symbol", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Cost", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "PnL", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl1h", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Pnl24h", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });
            positionGrid.Children.Add(new TextBlock { Tag = "Realized", Foreground = UiConstants.FontColor, HorizontalAlignment = HorizontalAlignment.Center });

            // Устанавливаем Column для каждого TextBlock
            for (int i = 0; i < positionGrid.Children.Count; i++)
            {
                Grid.SetColumn(positionGrid.Children[i], i);
            }

            return positionGrid;
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
                            MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
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