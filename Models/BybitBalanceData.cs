using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPnLWidget.Models
{
    public class BybitBalanceData
    {
        public decimal? TotalMarginBalance { get; set; }
        public decimal? AvailableBalance { get; set; }
        public bool HasUsdtAsset { get; set; }
    }
}
