using System;

namespace CryptoPnLWidget.Models
{
    public class ExchangeApiKeys
    {
        public string ExchangeName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 