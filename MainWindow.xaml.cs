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

        public MainWindow(
            CryptoPnLWidget.Services.ExchangeKeysManager exchangeKeysManager,
            CryptoPnLWidget.Services.Bybit.BybitService bybitService,
            CryptoPnLWidget.Services.PositionManager positionManager)
        {
            InitializeComponent();
            
            // Создаем сервисы после инициализации компонентов
            _sortingManager = new SortingManager();
            _trayIconManager = new TrayIconManager(this);
            _uiManager = new UIManager(PositionsPanel, MarginBalanceTextBlock, AvailableBalanceTextBlock, _sortingManager, positionManager);
            _dataManager = new DataManager(exchangeKeysManager, bybitService, positionManager, OnDataUpdated, OnError);

            this.Loaded += MainWindow_Loaded;
            InitializeSortIndicators();
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
                _uiManager.UpdateBalanceData(result.BalanceData);
                _uiManager.UpdatePositions();
            });
        }

        public void OnError(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                _uiManager.ShowError(errorMessage);
            });
        }
    }
}