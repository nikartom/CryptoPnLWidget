using System;

namespace CryptoPnLWidget.Models
{
    public class ExchangeApiKeys
    {
        public string ExchangeName { get; set; } = string.Empty; 
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
} 