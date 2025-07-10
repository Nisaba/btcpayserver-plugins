using BTCPayServer.Plugins.Peach.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace BTCPayServer.Plugins.Peach.Services
{
    public class PeachService
    {
        public static string BaseUrl => "https://api.peachbitcoin.com/v1/";
        public string Referal => "PRF4BC";

        private readonly ILogger<PeachService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public PeachService(ILogger<PeachService> logger, HttpClient httpClient, IMemoryCache cache)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _cache = cache;
        }

        public async Task<List<PeachBid>> GetBidsListAsync(PeachRequest req)
        {
            var cacheKey = $"{req.CurrencyCode}-{req.BtcAmount.ToString()}";
            var Bids = await _cache.GetOrCreateAsync<List<PeachBid>>(cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await DoGetBidsListAsync(req);
                });
            return Bids;
        }

        public async Task<List<PeachBid>> DoGetBidsListAsync(PeachRequest req)
        {
            var Bids = new List<PeachBid>();
            string sRep = "";
            try
            {

                dynamic peachRequest = new ExpandoObject();
                peachRequest.type = "bid";
                //if (req.Amount > 0) peachRequest.amount = req.Amount;

                var peachJson = JsonConvert.SerializeObject(peachRequest, Formatting.None);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, "offer/search")
                {
                    Content = new StringContent(peachJson, Encoding.UTF8, "application/json"),
                };


                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                    {
                        sRep = await rdr.ReadToEndAsync();
                    }
                    rep.EnsureSuccessStatusCode();
                }

                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                float vRate = Convert.ToSingle(req.Rate);
                float vBtcAmount = Convert.ToSingle(req.BtcAmount);
                foreach (dynamic PeachTrade in JsonRep.offers)
                {
                    //if (PeachTrade.prices[req.CurrencyCode] != null)
                    if (PeachTrade.meansOfPayment[req.CurrencyCode] != null)
                    {
                        float vMin = Convert.ToSingle(PeachTrade.amount[0] * 100000000) ;
                        float vMax = Convert.ToSingle(PeachTrade.amount[1] * 100000000);
                        if (vMin <= vBtcAmount && vBtcAmount <= vMax)
                        {
                            var ofr = new PeachBid()
                            {
                                ID = PeachTrade.id,
                                CountryCode = PeachTrade.countryCode,
                                MinAmount = vMin,
                                MaxAmount = vMax,
                                PaymentMethods = PeachTrade.meansOfPayment[req.CurrencyCode],
                                User = new PeachUser()
                                {
                                    Id = PeachTrade.user.id,
                                    NbTrades = PeachTrade.user.trades,
                                    OpenedTrades = PeachTrade.user.openedTrades,
                                    CanceledTrades = PeachTrade.user.canceledTrades,
                                    Rating = PeachTrade.user.rating,
                                    RatingCount = PeachTrade.user.ratingCount,
                                    Medals = PeachTrade.user.medals,
                                    OpenedDisputes = PeachTrade.user.disputes.opened,
                                    WonDisputes = PeachTrade.user.disputes.won,
                                    LostDisputes = PeachTrade.user.disputes.lost,
                                    ResolvedDisputes = PeachTrade.user.disputes.resolved
                                },
                            };
                            Bids.Add(ofr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PeachPlugin.GetBids(): {ex.Message} - {sRep}");
            }

            return Bids;

        }

    }
}
