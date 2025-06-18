using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPnLWidget.Models
{
    public class PositionPnlHistoryEntry
    {
        public decimal Pnl { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
