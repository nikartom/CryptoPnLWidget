using Bybit.Net.Objects.Models.V5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPnLWidget.Models
{
    public class BybitDataResult
    {
        public bool Success { get; set; }
        public BybitBalanceData? BalanceData { get; set; }
        public IEnumerable<BybitPosition>? Positions { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
