using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using CryptoPnLWidget.Services.Bybit;
using CryptoPnLWidget.API;
using System.Linq;

namespace CryptoPnLWidget.Services
{
    public class DataManager
    {
        private readonly ExchangeKeysManager _keysManager;
        private readonly BybitService _bybitService;
        private readonly PositionManager _positionManager;
        private readonly DispatcherTimer _updateTimer;
        private readonly Action<BybitDataResult> _onDataUpdated;
        private readonly Action<string> _onError;
        private BybitBalanceData? _lastSuccessfulBalanceData; // Храним последние успешные данные баланса

        public DataManager(
            ExchangeKeysManager keysManager, 
            BybitService bybitService, 
            PositionManager positionManager,
            Action<BybitDataResult> onDataUpdated,
            Action<string> onError)
        {
            _keysManager = keysManager;
            _bybitService = bybitService;
            _positionManager = positionManager;
            _onDataUpdated = onDataUpdated;
            _onError = onError;
            
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(5);
            _updateTimer.Tick += async (s, args) => await LoadBybitData();
        }

        public async Task InitializeAsync()
        {
            if (!_keysManager.HasKeysForExchange("Bybit"))
            {
                var settingsWindow = new ApiSettingsWindow(_keysManager);
                bool? dialogResult = settingsWindow.ShowDialog();

                if (dialogResult == true)
                {
                    await ConfigureBybitClientAndLoadData();
                }
                else
                {
                    throw new InvalidOperationException("API ключи не были предоставлены.");
                }
            }
            else
            {
                await ConfigureBybitClientAndLoadData();
            }
        }

        private async Task ConfigureBybitClientAndLoadData()
        {
            var keys = _keysManager.GetKeysForExchange("Bybit");

            if (keys == null || string.IsNullOrEmpty(keys.ApiKey) || string.IsNullOrEmpty(keys.ApiSecret))
            {
                throw new InvalidOperationException("Ошибка при загрузке API ключей после сохранения. Проверьте сохраненные данные.");
            }

            _bybitService.SetApiCredentials(keys.ApiKey, keys.ApiSecret);

            // Первый, немедленный вызов при запуске
            await LoadBybitData();

            _updateTimer.Start();
        }

        private async Task LoadBybitData()
        {
            try
            {
                var result = await _bybitService.LoadBybitDataAsync();

                if (result.Success)
                {
                    // Сохраняем успешные данные баланса
                    if (result.BalanceData != null)
                    {
                        _lastSuccessfulBalanceData = result.BalanceData;
                    }

                    // Обновляем позиции
                    if (result.Positions != null)
                    {
                        _positionManager.UpdatePositions(result.Positions);
                    }

                    _onDataUpdated(result);
                }
                else
                {
                    // Проверяем, является ли это сетевой ошибкой
                    if (IsNetworkError(result.ErrorMessage))
                    {
                        // Для сетевых ошибок показываем последние успешные данные баланса
                        _onDataUpdated(new BybitDataResult
                        {
                            Success = false,
                            BalanceData = _lastSuccessfulBalanceData, // Используем последние успешные данные
                            Positions = null,
                            ErrorMessage = "Отсутствует подключение!"
                        });
                    }
                    else
                    {
                        _onError($"Ошибка: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Проверяем, является ли это сетевой ошибкой
                if (IsNetworkError(ex.Message))
                {
                    // Для сетевых ошибок показываем последние успешные данные баланса
                    _onDataUpdated(new BybitDataResult
                    {
                        Success = false,
                        BalanceData = _lastSuccessfulBalanceData, // Используем последние успешные данные
                        Positions = null,
                        ErrorMessage = "Отсутствует подключение!"
                    });
                }
                else
                {
                    _onError($"Общая ошибка: {ex.Message}");
                }
            }
        }

        private bool IsNetworkError(string? errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            // Сетевые ошибки
            return errorMessage.Contains("host") ||
                   errorMessage.Contains("network") ||
                   errorMessage.Contains("connection") ||
                   errorMessage.Contains("timeout") ||
                   errorMessage.Contains("unreachable") ||
                   errorMessage.Contains("dns") ||
                   errorMessage.Contains("api.bybit.com");
        }

        public void Stop()
        {
            _updateTimer.Stop();
        }

        public void StopTimer()
        {
            _updateTimer.Stop();
        }
    }
} 