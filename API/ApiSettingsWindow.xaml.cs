using System.Windows;
using System.Windows.Controls;
using CryptoPnLWidget.API;
using CryptoPnLWidget.Services;

namespace CryptoPnLWidget.API
{
    public partial class ApiSettingsWindow : Window
    {
        private readonly ExchangeKeysManager _keysManager;
        private string? _selectedExchange;

        public ApiSettingsWindow(ExchangeKeysManager keysManager)
        {
            InitializeComponent();
            _keysManager = keysManager;
            InitializeExchanges();
            
            // Устанавливаем начальное значение выбранной биржи
            if (ExchangeComboBox.SelectedItem is string selectedExchange)
            {
                _selectedExchange = selectedExchange;
                var keys = _keysManager.GetKeysForExchange(selectedExchange);
                if (keys != null)
                {
                    ApiKeyTextBox.Text = keys.ApiKey;
                    ApiSecretPasswordBox.Password = keys.ApiSecret;
                }
            }
        }

        private void InitializeExchanges()
        {
            var exchanges = _keysManager.GetAvailableExchanges();
            ExchangeComboBox.ItemsSource = exchanges;
            ExchangeComboBox.SelectedIndex = 0;
            ExchangeComboBox.SelectionChanged += ExchangeComboBox_SelectionChanged;
        }

        private void ExchangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExchangeComboBox.SelectedItem is string selectedExchange)
            {
                _selectedExchange = selectedExchange;
                var keys = _keysManager.GetKeysForExchange(selectedExchange);
                if (keys != null)
                {
                    ApiKeyTextBox.Text = keys.ApiKey;
                    ApiSecretPasswordBox.Password = keys.ApiSecret;
                }
                else
                {
                    ApiKeyTextBox.Text = string.Empty;
                    ApiSecretPasswordBox.Password = string.Empty;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedExchange))
            {
                MessageBox.Show("Пожалуйста, выберите биржу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string apiKey = ApiKeyTextBox.Text;
            string apiSecret = ApiSecretPasswordBox.Password;

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                try
                {
                    _keysManager.SaveKeysForExchange(_selectedExchange, apiKey, apiSecret);
                    DialogResult = true;
                    Close();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении ключей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите оба ключа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 