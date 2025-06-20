using System;
using System.Collections.Generic;
using System.Linq;
using CryptoPnLWidget.Services;

namespace CryptoPnLWidget.Services
{
    public class SortingManager
    {
        private string _currentSortColumn = "PnL";
        private bool _isAscending = false;

        public string CurrentSortColumn => _currentSortColumn;
        public bool IsAscending => _isAscending;

        public void SetSortColumn(string columnName)
        {
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
        }

        public List<PositionManager.PositionHistoryTracker> SortPositions(IEnumerable<PositionManager.PositionHistoryTracker> positions)
        {
            var openPositionTrackers = positions.ToList();

            // Если нет открытых позиций, возвращаем пустой список
            if (!openPositionTrackers.Any())
            {
                return new List<PositionManager.PositionHistoryTracker>();
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
                _ => openPositionTrackers.OrderBy(t => 0) // Просто для того, чтобы тип был IOrderedEnumerable
            }).ToList();

            return sortedTrackers;
        }
    }
} 