using BTCPayServer.Plugins.Lendasat.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Lendasat.Services
{
    public class LendasatService
    {
        public static string BaseBorrowUrl => "https://borrow.lendasat.com/api/";
        public static string BaseLendUrl => "https://lend.lendasat.com/api/";
        public string Referal => "PRF4BC";

        private readonly ILogger<LendasatService> _logger;
        private readonly HttpClient _httpBorrowClient;
        private readonly HttpClient _httpLendClient;
        private readonly IMemoryCache _cache;

        public LendasatService(ILogger<LendasatService> logger, IMemoryCache cache, HttpClient httpBorrowClient, HttpClient httpLndClient)
        {
            _logger = logger;
            _cache = cache;
            _httpBorrowClient = httpBorrowClient;
            _httpBorrowClient.BaseAddress = new Uri(BaseBorrowUrl);
            _httpLendClient = httpLndClient;
            _httpLendClient.BaseAddress = new Uri(BaseLendUrl);
        }

        public async Task<List<LendasatLoanOffer>> GetLoanOffers(LendasatSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.APIKey))
            {
                throw new ArgumentException("Lendasat settings are not configured.");
            }
            var cacheKey = $"LoanOffers_{settings.StoreId}";
            return await _cache.GetOrCreateAsync<List<LendasatLoanOffer>>(cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await DoGetLoanOffers(settings);
                });

        }

        private async Task<List<LendasatLoanOffer>> DoGetLoanOffers(LendasatSettings settings)
        {
            string sRep = "";
            try
            {
                var webRequest = new HttpRequestMessage(HttpMethod.Get, "offers?loan_type=All");
                webRequest.Headers.Add("x-api-key", settings.APIKey);

                using (var rep = await _httpBorrowClient.SendAsync(webRequest))
                {
                    using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                    {
                        sRep = await rdr.ReadToEndAsync();
                    }
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                // ...
                return new List<LendasatLoanOffer>;
            }
            catch (Exception ex)
            {
                var sError = $"{ex.Message} - {sRep}";
                _logger.LogError($"PeachPlugin.GetToken(): {sError}");
                throw new Exception(sError);
            }
        }
    }
}
