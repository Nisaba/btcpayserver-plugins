using BTCPayServer.Plugins.Peach.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<string> GetToken(PeachSettings peachSettings)
        {
            var cacheKey = $"Token-{peachSettings.StoreId}";
            return await _cache.GetOrCreateAsync<string>(cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await DoGetToken(peachSettings);
                });
        }

        private async Task<string> DoGetToken(PeachSettings peachSettings)
        {
            return "MY-TOKEN-XXX";
            /* string sRep = "";
            try
            {
                var dto = new DateTimeOffset(DateTime.UtcNow);
                var sMsg = $"Peach Registration {dto.ToUnixTimeMilliseconds().ToString()}";

                dynamic peachRequest = new ExpandoObject();
                peachRequest.publicKey = peachSettings.PublicKey;
                peachRequest.message = sMsg;
                peachRequest.signature = SignMessage(sMsg, peachSettings.PrivateKey);
                peachRequest.uniqueId = "btcpay";

                var peachJson = JsonConvert.SerializeObject(peachRequest, Formatting.None);
                var webRequest = new HttpRequestMessage(HttpMethod.Post, $"user/{(peachSettings.IsRegistered ? "auth" : "register")}")
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
                string sToken = JsonRep.accessToken;
                return sToken;
            }
            catch (Exception ex)
            {
                var sError = $"{ex.Message} - {sRep}";
                _logger.LogError($"PeachPlugin.GetToken(): sError");
                throw new Exception(sError);
            }*/

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

                float vBtcAmount = Convert.ToSingle(req.BtcAmount) * 100000000;
                dynamic peachRequest = new ExpandoObject();
                peachRequest.type = "bid";
                peachRequest.amount = vBtcAmount;

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

                var lstPaymentMethods = await GetUserPaymentMethods(req.Token);

                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                var vRate = Convert.ToSingle(req.Rate) / 100000000;
                foreach (dynamic PeachTrade in JsonRep.offers)
                {
                    //if (PeachTrade.prices[req.CurrencyCode] != null)
                    if (PeachTrade.meansOfPayment[req.CurrencyCode] != null)
                    {
                        float vMin, vMax;
                        if (PeachTrade.amount is Newtonsoft.Json.Linq.JArray)
                        {
                            vMin = Convert.ToSingle(PeachTrade.amount[0]);
                            vMax = Convert.ToSingle(PeachTrade.amount[1]);
                        }
                        else
                        {
                            vMin = vMax = Convert.ToSingle(PeachTrade.amount);
                        }
                        //                 if (vMin <= vBtcAmount && vBtcAmount <= vMax)
                        //                 {
                        var ofr = new PeachBid()
                        {
                            Id = PeachTrade.id,
                            MinAmount = vMin,
                            MaxAmount = vMax,
                            PaymentMethods = ((Newtonsoft.Json.Linq.JArray)PeachTrade.meansOfPayment[req.CurrencyCode]).ToObject<List<string>>(),
                            User = new PeachUser()
                            {
                                Id = PeachTrade.user.id,
                                NbTrades = PeachTrade.user.trades,
                                OpenedTrades = PeachTrade.user.openedTrades,
                                CanceledTrades = PeachTrade.user.canceledTrades,
                                Rating = PeachTrade.user.rating,
                                RatingCount = PeachTrade.user.ratingCount,
                                Medals = ((Newtonsoft.Json.Linq.JArray)PeachTrade.user.medals).ToObject<List<string>>(),
                                OpenedDisputes = PeachTrade.user.disputes.opened,
                                WonDisputes = PeachTrade.user.disputes.won,
                                LostDisputes = PeachTrade.user.disputes.lost,
                                ResolvedDisputes = PeachTrade.user.disputes.resolved
                            },
                           // IsOnline = PeachTrade.online,
                            MinFiatAmount = vMin * vRate,
                            MaxFiatAmount = vMax * vRate
                        };
                        if (ofr.PaymentMethods != null && ofr.PaymentMethods.Count > 0)
                        {
                            bool hasCommonMethod = false;
                            foreach (var method in ofr.PaymentMethods)
                            {
                                if (lstPaymentMethods.Contains(method))
                                {
                                    hasCommonMethod = true;
                                    break;
                                }
                            }
                            if (hasCommonMethod) Bids.Add(ofr);
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

        public async Task<List<string>> GetUserPaymentMethods(string token)
        {
            var cacheKey = $"PaymentMethods-{token}";
            return await _cache.GetOrCreateAsync<List<string>>(cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await DoGetUserPaymentMethods(token);
                });
        }

        private async Task<List<string>>DoGetUserPaymentMethods(string token)
        {
            var paymentMethods = new List<string>() { "paypal", "instantSepa", "wise" };
            /*string sRep = "";
            try
            {
                var webRequest = new HttpRequestMessage(HttpMethod.Get, $"user/me/paymentMethods");
                webRequest.Headers.Add("Authorization", $"Bearer {token}");
                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                    {
                        sRep = await rdr.ReadToEndAsync();
                    }
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                foreach (var method in JsonRep.paymentMethods.sell)
                {
                    paymentMethods.Add(method.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"PeachPlugin.GetUserPaymentMethods(): {ex.Message} - {sRep}");
            }*/
            return paymentMethods;
        }

        private string SignMessage(string message, string hexPrivateKey)
        {
            byte[] privKeyBytes = Encoders.Hex.DecodeData(hexPrivateKey);

            if (privKeyBytes.Length != 32)
            {
                throw new ArgumentException("Private key must have exactly 32 bytes (64 chars hex).", nameof(hexPrivateKey));
            }

            var privKey = new Key(privKeyBytes);

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] hash = Hashes.SHA256(messageBytes);

            var signatureBytes = privKey.Sign(new uint256(hash)).ToCompact();

            return Encoders.Hex.EncodeData(signatureBytes);
        }

    }
}
