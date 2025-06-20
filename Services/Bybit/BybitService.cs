using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Authentication;
using System;
using System.Linq;
using System.Threading.Tasks;
using CryptoPnLWidget.Models;
using System.Collections.Generic;

namespace CryptoPnLWidget.Services.Bybit
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

        public async Task<decimal?> GetAvailableBalanceAsync()
        {
            var walletBalancesResponse = await _bybitRestClient.V5Api.Account.GetBalancesAsync(AccountType.Unified);
            if (walletBalancesResponse.Success)
            {
                var unifiedAccountBalance = walletBalancesResponse.Data.List.FirstOrDefault();
                if (unifiedAccountBalance != null)
                {
                    var usdtAsset = unifiedAccountBalance.Assets.FirstOrDefault(a => a.Asset == "USDT");
                    if (usdtAsset != null)
                    {
                        return usdtAsset.UsdValue - (usdtAsset.TotalOrderInitialMargin + usdtAsset.TotalPositionInitialMargin);
                    }
                }
            }
            return null;
        }

        public async Task<BybitBalanceData?> GetBalanceDataAsync()
        {
            var walletBalancesResponse = await _bybitRestClient.V5Api.Account.GetBalancesAsync(AccountType.Unified);
            if (walletBalancesResponse.Success)
            {
                var unifiedAccountBalance = walletBalancesResponse.Data.List.FirstOrDefault();
                if (unifiedAccountBalance != null)
                {
                    var usdtAsset = unifiedAccountBalance.Assets.FirstOrDefault(a => a.Asset == "USDT");
                    return new BybitBalanceData
                    {
                        TotalMarginBalance = unifiedAccountBalance.TotalMarginBalance,
                        AvailableBalance = usdtAsset != null 
                            ? usdtAsset.UsdValue - (usdtAsset.TotalOrderInitialMargin + usdtAsset.TotalPositionInitialMargin)
                            : null,
                        HasUsdtAsset = usdtAsset != null
                    };
                }
            }
            return null;
        }

        public async Task<IEnumerable<BybitPosition>?> GetPositionsAsync()
        {
            var positionsResponse = await _bybitRestClient.V5Api.Trading.GetPositionsAsync(category: Category.Linear, settleAsset: "USDT");
            if (positionsResponse.Success)
            {
                return positionsResponse.Data.List;
            }
            return null;
        }

        public async Task<BybitDataResult> LoadBybitDataAsync()
        {
            try
            {
                var balanceData = await GetBalanceDataAsync();
                var positions = await GetPositionsAsync();

                return new BybitDataResult
                {
                    Success = true,
                    BalanceData = balanceData,
                    Positions = positions,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                return new BybitDataResult
                {
                    Success = false,
                    BalanceData = null,
                    Positions = null,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class BybitBalanceData
    {
        public decimal? TotalMarginBalance { get; set; }
        public decimal? AvailableBalance { get; set; }
        public bool HasUsdtAsset { get; set; }
    }

    public class BybitDataResult
    {
        public bool Success { get; set; }
        public BybitBalanceData? BalanceData { get; set; }
        public IEnumerable<BybitPosition>? Positions { get; set; }
        public string? ErrorMessage { get; set; }
    }
} 