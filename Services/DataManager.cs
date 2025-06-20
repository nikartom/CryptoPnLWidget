using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using CryptoPnLWidget.Services.Bybit;
using CryptoPnLWidget.API;

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
                    // Обновляем позиции
                    if (result.Positions != null)
                    {
                        _positionManager.UpdatePositions(result.Positions);
                    }

                    _onDataUpdated(result);
                }
                else
                {
                    _onError($"Ошибка: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _onError($"Общая ошибка: {ex.Message}");
            }
        }

        public void Stop()
        {
            _updateTimer.Stop();
        }
    }
} 