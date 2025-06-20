using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using CryptoPnLWidget.Models;

namespace CryptoPnLWidget.Services
{
    public class ExchangeKeysManager
    {
        private readonly string _keysFilePath;
        private readonly byte[] _entropy;
        private Dictionary<string, ExchangeApiKeys> _exchangeKeys;

        public ExchangeKeysManager()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificFolder = Path.Combine(appDataFolder, "CryptoPnLWidget");
            Directory.CreateDirectory(appSpecificFolder);
            _keysFilePath = Path.Combine(appSpecificFolder, "exchange_keys.dat");
            _entropy = Encoding.UTF8.GetBytes("YourUniqueCryptoPnLWidgetEntropyStringHere");
            _exchangeKeys = new Dictionary<string, ExchangeApiKeys>();
            LoadKeys();
        }

        public List<string> GetAvailableExchanges()
        {
            return new List<string> { "Bybit" }; // В будущем здесь будет список всех поддерживаемых бирж
        }

        public bool HasKeysForExchange(string exchangeName)
        {
            return _exchangeKeys.ContainsKey(exchangeName) && 
                   !string.IsNullOrEmpty(_exchangeKeys[exchangeName].ApiKey) && 
                   !string.IsNullOrEmpty(_exchangeKeys[exchangeName].ApiSecret);
        }

        public ExchangeApiKeys? GetKeysForExchange(string exchangeName)
        {
            if (_exchangeKeys.TryGetValue(exchangeName, out var keys))
            {
                return keys;
            }
            return null;
        }

        public void SaveKeysForExchange(string exchangeName, string apiKey, string apiSecret)
        {
            var keys = new ExchangeApiKeys
            {
                ExchangeName = exchangeName,
                ApiKey = apiKey,
                ApiSecret = apiSecret,
                LastUpdated = DateTime.UtcNow
            };

            _exchangeKeys[exchangeName] = keys;
            SaveKeys();
        }

        private void LoadKeys()
        {
            if (!File.Exists(_keysFilePath))
            {
                return;
            }

            try
            {
                byte[] encryptedData = File.ReadAllBytes(_keysFilePath);
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                string json = Encoding.UTF8.GetString(decryptedData);
                _exchangeKeys = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ExchangeApiKeys>>(json) ?? new Dictionary<string, ExchangeApiKeys>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при загрузке ключей: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _exchangeKeys = new Dictionary<string, ExchangeApiKeys>();
            }
        }

        private void SaveKeys()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(_exchangeKeys);
                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] encryptedData = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_keysFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении ключей: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 