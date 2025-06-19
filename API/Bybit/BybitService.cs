using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoExchange.Net.Authentication;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoPnLWidget.API.Bybit
{
    public class BybitService
    {
        private readonly BybitRestClient _bybitRestClient;

        public BybitService(BybitRestClient bybitRestClient)
        {
            _bybitRestClient = bybitRestClient;
        }

        public void SetApiCredentials(string apiKey, string apiSecret)
        {
            _bybitRestClient.SetApiCredentials(new ApiCredentials(apiKey, apiSecret));
        }

        public async Task<decimal?> GetMarginBalanceAsync()
        {
            var walletBalancesResponse = await _bybitRestClient.V5Api.Account.GetBalancesAsync(AccountType.Unified);
            if (walletBalancesResponse.Success)
            {
                var unifiedAccountBalance = walletBalancesResponse.Data.List.FirstOrDefault();
                return unifiedAccountBalance?.TotalMarginBalance;
            }
            return null;
        }

        // Добавьте другие методы для работы с Bybit API по мере необходимости
    }
} 