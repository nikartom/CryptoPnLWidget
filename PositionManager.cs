using Bybit.Net.Objects.Models.V5;
using BybitWidget.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;        // <-- ДОБАВЛЕНО
using System.Linq;
using System.Text.Json; // <-- ДОБАВЛЕНО

namespace BybitWidget
{
    public class PositionManager
    {
        // Словарь для хранения текущих позиций и их истории PnL.
        private readonly ConcurrentDictionary<string, PositionHistoryTracker> _positionTrackers;

        // Константы для пути к файлу истории PnL
        private const string PnlHistoryFileName = "pnl_history.json";
        private readonly string _pnlHistoryFilePath;
        private DateTime _lastSaveTime = DateTime.MinValue;
        private const int SaveIntervalMinutes = 5; // Save every 5 minutes
        private bool _hasChanges = false;

        public PositionManager()
        {
            _positionTrackers = new ConcurrentDictionary<string, PositionHistoryTracker>();

            // Определяем путь к файлу истории PnL
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificFolder = Path.Combine(appDataFolder, "BybitWidget"); // Та же папка, что и для API ключей
            Directory.CreateDirectory(appSpecificFolder); // Убедимся, что папка существует
            _pnlHistoryFilePath = Path.Combine(appSpecificFolder, PnlHistoryFileName);

            LoadHistory(); // <--- Загружаем историю при создании менеджера
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
                    Console.WriteLine($"Position {symbol} removed from tracker as it's no longer active.");
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
        }

        public IEnumerable<PositionHistoryTracker> GetActivePositionTrackers()
        {
            return _positionTrackers.Values.Where(t => t.CurrentPosition?.Quantity != 0);
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
                Console.WriteLine($"PnL history saved to {_pnlHistoryFilePath}");
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
                Console.WriteLine("PnL history file not found. Starting with empty history.");
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
                    Console.WriteLine($"PnL history loaded from {_pnlHistoryFilePath}");
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

        public class PositionHistoryTracker
        {
            public string Symbol { get; private set; }
            public BybitPosition CurrentPosition { get; private set; }
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

            public decimal? GetPnlChange(TimeSpan interval)
            {
                lock (_lock)
                {
                    if (_pnlHistory.Count < 2 || CurrentPosition?.UnrealizedPnl.HasValue == false)
                    {
                        return null; // Недостаточно данных
                    }

                    var timeAgo = DateTime.UtcNow.Subtract(interval);

                    var oldestEntryInInterval = _pnlHistory
                        .OrderBy(e => e.Timestamp)
                        .FirstOrDefault(e => e.Timestamp >= timeAgo);

                    if (oldestEntryInInterval != null)
                    {
                        return CurrentPosition.UnrealizedPnl.Value - oldestEntryInInterval.Pnl;
                    }
                    else if (_pnlHistory.Any())
                    {
                        // Если нет записи точно в интервале, но есть старые записи
                        // Берем самую старую доступную, если она старше TimeAgo (т.е. за пределами интервала)
                        var firstEntry = _pnlHistory.First();
                        if (firstEntry.Timestamp < timeAgo)
                        {
                            return CurrentPosition.UnrealizedPnl.Value - firstEntry.Pnl;
                        }
                    }
                    return null;
                }
            }
        }
    }
}