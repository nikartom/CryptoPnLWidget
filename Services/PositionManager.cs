using Bybit.Net.Objects.Models.V5;
using CryptoPnLWidget.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;        // <-- ДОБАВЛЕНО
using System.Linq;
using System.Text.Json; // <-- ДОБАВЛЕНО

namespace CryptoPnLWidget.Services
{
    public class PositionManager
    {
        // Словарь для хранения текущих позиций и их истории PnL.
        private readonly ConcurrentDictionary<string, PositionHistoryTracker> _positionTrackers;

        // Константы для пути к файлу истории PnL
        private const string PnlHistoryFileName = "pnl_history.json";
        private const string HoldPositionsFileName = "hold_positions.json";
        private readonly string _pnlHistoryFilePath;
        private readonly string _holdPositionsFilePath;
        private DateTime _lastSaveTime = DateTime.MinValue;
        private const int SaveIntervalMinutes = 5; // Save every 5 minutes
        private bool _hasChanges = false;
        private bool _hasHoldChanges = false;

        public PositionManager()
        {
            _positionTrackers = new ConcurrentDictionary<string, PositionHistoryTracker>();

            // Определяем путь к файлу истории PnL
            string? appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string? appSpecificFolder = Path.Combine(appDataFolder, "CryptoPnLWidget"); // Та же папка, что и для API ключей
            Directory.CreateDirectory(appSpecificFolder); // Убедимся, что папка существует
            _pnlHistoryFilePath = Path.Combine(appSpecificFolder, PnlHistoryFileName);
            _holdPositionsFilePath = Path.Combine(appSpecificFolder, HoldPositionsFileName);

            LoadHistory(); // <--- Загружаем историю при создании менеджера
            LoadHoldPositions(); // <--- Загружаем состояние галочек
        }

        // Обновляет или добавляет позиции и их PnL историю
        public void UpdatePositions(IEnumerable<BybitPosition> currentPositions)
        {
            var activeSymbols = new HashSet<string>();

            foreach (var position in currentPositions)
            {
                if (position.Symbol == null) continue;

                activeSymbols.Add(position.Symbol);

                var tracker = _positionTrackers.GetOrAdd(position.Symbol, newSymbol => new PositionHistoryTracker(newSymbol));
                tracker.UpdateCurrentPosition(position);

                if (position.UnrealizedPnl.HasValue)
                {
                    tracker.AddPnlHistoryEntry(position.UnrealizedPnl.Value);
                    _hasChanges = true;
                }
            }

            var symbolsToRemove = _positionTrackers.Keys.Except(activeSymbols).ToList();
            foreach (var symbol in symbolsToRemove)
            {
                if (_positionTrackers.TryRemove(symbol, out var _))
                {
                    _hasChanges = true;
                }
            }

            foreach (var tracker in _positionTrackers.Values)
            {
                tracker.CleanOldHistory();
            }

            // Save only if there are changes and enough time has passed
            if (_hasChanges && (DateTime.Now - _lastSaveTime).TotalMinutes >= SaveIntervalMinutes)
            {
                SaveHistory();
                _lastSaveTime = DateTime.Now;
                _hasChanges = false;
            }

            // Save hold positions if there are changes
            if (_hasHoldChanges)
            {
                SaveHoldPositions();
                _hasHoldChanges = false;
            }
        }

        public IEnumerable<PositionHistoryTracker> GetActivePositionTrackers()
        {
            return _positionTrackers.Values.Where(t => t.CurrentPosition?.Quantity != 0);
        }

        public IEnumerable<PositionHistoryTracker> GetShortTermPositions()
        {
            return GetActivePositionTrackers().Where(t => !t.IsHold);
        }

        public IEnumerable<PositionHistoryTracker> GetLongTermPositions()
        {
            return GetActivePositionTrackers().Where(t => t.IsHold);
        }

        public void SetHoldPosition(string symbol, bool isHold)
        {
            if (_positionTrackers.TryGetValue(symbol, out var tracker))
            {
                tracker.IsHold = isHold;
                _hasHoldChanges = true;
            }
        }

        // --- НОВЫЙ МЕТОД: Сохранение истории PnL ---
        public void SaveHistory()
        {
            try
            {
                // Создаем простой словарь для сериализации: Symbol -> List<PnlHistoryEntry>
                var dataToSave = new Dictionary<string, List<PositionPnlHistoryEntry>>();
                foreach (var tracker in _positionTrackers.Values)
                {
                    lock (tracker._lock) // Блокируем, чтобы избежать изменений во время сохранения
                    {
                        if (tracker._pnlHistory.Any())
                        {
                            dataToSave[tracker.Symbol] = tracker._pnlHistory.ToList(); // Копируем список
                        }
                    }
                }

                var jsonString = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_pnlHistoryFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving PnL history: {ex.Message}");
                // В реальном приложении здесь можно показать MessageBox или логгировать
            }
        }

        // --- НОВЫЙ МЕТОД: Загрузка истории PnL ---
        private void LoadHistory()
        {
            if (!File.Exists(_pnlHistoryFilePath))
            {
                return;
            }

            try
            {
                var jsonString = File.ReadAllText(_pnlHistoryFilePath);
                var loadedData = JsonSerializer.Deserialize<Dictionary<string, List<PositionPnlHistoryEntry>>>(jsonString);

                if (loadedData != null)
                {
                    foreach (var entry in loadedData)
                    {
                        var symbol = entry.Key;
                        var history = entry.Value;

                        // Создаем новый трекер и заполняем его историей
                        // CurrentPosition будет обновлен при первом GetPositionsAsync
                        var tracker = _positionTrackers.GetOrAdd(symbol, newSymbol => new PositionHistoryTracker(newSymbol));
                        lock (tracker._lock)
                        {
                            tracker._pnlHistory.AddRange(history);
                            tracker.CleanOldHistory(); // Очищаем старые записи при загрузке на случай, если файл не чистился долго
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing PnL history file (JSON error): {ex.Message}");
                // Возможно, файл поврежден, или его формат изменился
                // Можно удалить поврежденный файл: File.Delete(_pnlHistoryFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading PnL history: {ex.Message}");
            }
        }

        // --- НОВЫЕ МЕТОДЫ: Сохранение и загрузка состояния галочек ---
        private void SaveHoldPositions()
        {
            try
            {
                var holdPositions = _positionTrackers.Values
                    .Where(t => t.IsHold)
                    .Select(t => t.Symbol)
                    .ToList();

                var jsonString = JsonSerializer.Serialize(holdPositions, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_holdPositionsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving hold positions: {ex.Message}");
            }
        }

        private void LoadHoldPositions()
        {
            if (!File.Exists(_holdPositionsFilePath))
            {
                return;
            }

            try
            {
                var jsonString = File.ReadAllText(_holdPositionsFilePath);
                var holdPositions = JsonSerializer.Deserialize<List<string>>(jsonString);

                if (holdPositions != null)
                {
                    foreach (var symbol in holdPositions)
                    {
                        var tracker = _positionTrackers.GetOrAdd(symbol, newSymbol => new PositionHistoryTracker(newSymbol));
                        tracker.IsHold = true;
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing hold positions file (JSON error): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading hold positions: {ex.Message}");
            }
        }

        public class PositionHistoryTracker
        {
            public string Symbol { get; private set; }
            public BybitPosition? CurrentPosition { get; private set; }
            public bool IsHold { get; set; } = false; // <--- НОВОЕ ПОЛЕ для галочки
            // Поле должно быть публичным или иметь getter для сериализации, если вы решите сериализовать tracker целиком
            // Сейчас оно остается private, потому что мы сериализуем Dictionary<string, List<PositionPnlHistoryEntry>>
            // Но для CleanOldHistory() и AddPnlHistoryEntry() оно должно быть доступно
            internal readonly List<PositionPnlHistoryEntry> _pnlHistory; // <--- Changed to internal for access in Save/Load
            internal readonly object _lock = new object(); // Для защиты _pnlHistory

            public PositionHistoryTracker(string symbol)
            {
                Symbol = symbol;
                _pnlHistory = new List<PositionPnlHistoryEntry>();
            }

            public void UpdateCurrentPosition(BybitPosition position)
            {
                CurrentPosition = position;
            }

            public void AddPnlHistoryEntry(decimal pnl)
            {
                lock (_lock)
                {
                    _pnlHistory.Add(new PositionPnlHistoryEntry { Pnl = pnl, Timestamp = DateTime.UtcNow });
                }
            }

            public void CleanOldHistory()
            {
                lock (_lock)
                {
                    var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
                    _pnlHistory.RemoveAll(entry => entry.Timestamp < twentyFourHoursAgo);
                }
            }

            public BybitPosition? GetCurrentPosition()
            {
                return CurrentPosition;
            }

            public decimal? GetPnlChange(TimeSpan interval, BybitPosition? currentPosition)
            {
                lock (_lock)
                {
                    if (_pnlHistory.Count < 2 || CurrentPosition?.UnrealizedPnl is not decimal currentUnrealizedPnl)
                    {
                        return null; // Недостаточно данных или нет значения PnL
                    }

                    var timeAgo = DateTime.UtcNow.Subtract(interval);

                    var oldestEntryInInterval = _pnlHistory
                        .OrderBy(e => e.Timestamp)
                        .FirstOrDefault(e => e.Timestamp >= timeAgo);

                    if (oldestEntryInInterval != null)
                    {
                        return currentUnrealizedPnl - oldestEntryInInterval.Pnl;
                    }
                    else if (_pnlHistory.Any())
                    {
                        var firstEntry = _pnlHistory.First();
                        if (firstEntry.Timestamp < timeAgo && currentPosition?.UnrealizedPnl is decimal cpUnrealizedPnl)
                        {
                            return cpUnrealizedPnl - firstEntry.Pnl;
                        }
                    }
                    return null;
                }
            }
        
        }
    }
} 