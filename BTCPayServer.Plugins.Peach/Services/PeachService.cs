using BTCPayServer.Plugins.Peach.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Crypto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Network = NBitcoin.Network;

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
            if (string.IsNullOrEmpty(peachSettings.Pwd))
                return string.Empty;

            var cacheKey = $"Token-{peachSettings.StoreId}-{peachSettings.Pwd}";
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
            string sRep = string.Empty;
            string sToken = string.Empty;
            try
            {
                var sMsg = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
                var t = SignMessage(sMsg, peachSettings.PrivKey);

                dynamic peachRequest = new ExpandoObject();
                peachRequest.publicKey = t.Item1;
                peachRequest.message = sMsg;
                peachRequest.signature = t.Item2;
                peachRequest.uniqueId = "btcpay-" + RandomNumberGenerator.GetInt32(100000).ToString();

                var peachJson = JsonConvert.SerializeObject(peachRequest, Formatting.None);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, "user/auth")
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
                sToken = JsonRep.accessToken;

              /*  var webRequestUser = new HttpRequestMessage(HttpMethod.Get, "user/me");
                webRequestUser.Headers.Add("Authorization", $"Bearer {sToken}");
                using (var rep = await _httpClient.SendAsync(webRequestUser))
                {
                    using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                    {
                        sRep = await rdr.ReadToEndAsync();
                    }
                    rep.EnsureSuccessStatusCode();
                }*/
            }
            catch (Exception ex)
            {
                var sError = $"{ex.Message} - {sRep}";
                _logger.LogError($"PeachPlugin.GetToken(): {sError}");
            }
            return sToken;

        }

        public async Task<List<PeachBid>> GetBidsListAsync(PeachRequest req, List<string> lstPaymentMethods)
        {
            var cacheKey = $"{req.CurrencyCode}-{req.BtcAmount.ToString()}";
            var Bids = await _cache.GetOrCreateAsync<List<PeachBid>>(cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await DoGetBidsListAsync(req, lstPaymentMethods);
                });
            return Bids;
        }
        public async Task<List<PeachBid>> DoGetBidsListAsync(PeachRequest req, List<string> lstPaymentMethods)
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

        public async Task<string> PostSellOffer(PeachPostOfferRequest req)
        {
            string sRep = "";
            try
            {
                var dicPaymentData  = new Dictionary<string, dynamic>();
                foreach (var mop in req.MeansOfPayment)
                {
                    dicPaymentData[mop.MoP] = new ExpandoObject();
                    var data = (IDictionary<string, object>)dicPaymentData[mop.MoP];
                    data["hashes"] = new[] { mop.HashPaymentData };
                }

                dynamic peachRequest = new ExpandoObject();
                peachRequest.type = "ask";
                peachRequest.amount = Convert.ToUInt64(Convert.ToSingle(req.Amount) * 100000000);
                peachRequest.premium = Convert.ToInt16(req.Premium);
                peachRequest.meansOfPayment = new Dictionary<string, List<string>> { [req.CurrencyCode] = req.MeansOfPayment.Select(p => p.MoP).ToList() };
                peachRequest.paymentData = dicPaymentData;
#if DEBUG
                peachRequest.returnAddress = "bc1qfksx4fuzm6xua093zhllzwmzadtzegyhpqn4au";
#else
                peachRequest.returnAddress = req.ReturnAdress; 
#endif 
                var peachJson = JsonConvert.SerializeObject(peachRequest, Formatting.None);
                var webRequest = new HttpRequestMessage(HttpMethod.Post, "offer")
                {
                    Content = new StringContent(peachJson, Encoding.UTF8, "application/json"),
                };
                webRequest.Headers.Add("Accept", "application/json");
              //  webRequest.Headers.Add("User-Agent", "Mozilla/5.0");
              //  webRequest.Headers.TryAddWithoutValidation("Authorization", req.PeachToken);
                webRequest.Headers.Add("Authorization", $"Bearer {HttpUtility.HtmlDecode(req.PeachToken)}");

                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                    {
                        sRep = await rdr.ReadToEndAsync();
                    }
                    rep.EnsureSuccessStatusCode();
                }
                var jObj = JObject.Parse(sRep);
                string id = jObj["id"]?.ToString();
                _logger.LogInformation($"Peach Offer created: {id}");
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError($"PeachPlugin.PostSellOffer(): {ex.Message} - {sRep} - {req.PeachToken}");
                throw;
            }
        }

        public async Task<string> CreateEscrow(string token, string offerId, string privKey)
        {
            string sRep = "";
             try
             {
                 dynamic peachRequest = new ExpandoObject();
                 peachRequest.publicKey = GetOfferPubKey(offerId, privKey);

                 var peachJson = JsonConvert.SerializeObject(peachRequest, Formatting.None);
                 var webRequest = new HttpRequestMessage(HttpMethod.Post, $"offer/{offerId}/escrow")
                 {
                     Content = new StringContent(peachJson, Encoding.UTF8, "application/json"),
                 };
                 webRequest.Headers.Add("Accept", "application/json");
                 // webRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                 webRequest.Headers.Add("Authorization", $"Bearer {HttpUtility.HtmlDecode(token)}");
                using (var rep = await _httpClient.SendAsync(webRequest))
                 {
                     using (var rdr = new StreamReader(await rep.Content.ReadAsStreamAsync()))
                     {
                         sRep = await rdr.ReadToEndAsync();
                     }
                     rep.EnsureSuccessStatusCode();
                 }
                var jObj = JObject.Parse(sRep);
                string escrow = jObj["escrow"]?.ToString();
                _logger.LogInformation($"Peach Escrow created");
#if DEBUG
                return "bcrt1qpzfyktpawhcy66ctqpujdhfxsm8atjqzezq9p4";
#else
            return escrow;
#endif 
             }
             catch (Exception ex)
             {
                 _logger.LogError($"PeachPlugin.CreateEscrow(): {ex.Message} - {sRep}");
                 throw;
             }

        }

        private Tuple<string, string> SignMessage(string message, string privateKeyHex)
        {
            var master = ExtKey.Parse(privateKeyHex, Network.Main);

            var derived = master.Derive(new KeyPath("m/48'/0'/0'/0'"));


            var privKey = derived.PrivateKey;
            var pubKey = privKey.PubKey;

            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
                    byte[] messageHash = sha256.ComputeHash(messageBytes);

                    uint256 hash = new uint256(messageHash);
                    ECDSASignature signature = privKey.Sign(hash);
                    byte[] compactSig = signature.ToCompact();

                    if (compactSig.Length != 64)
                    {
                        throw new InvalidOperationException($"Compact signature length is {compactSig.Length}, expected 64.");
                    }
                    return Tuple.Create(pubKey.ToHex(), BitConverter.ToString(compactSig).Replace("-", "").ToLower());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        private string GetOfferPubKey(string offerId, string privateKeyHex)
        {
            var extKey = ExtKey.Parse(privateKeyHex, Network.Main);

            var path = new KeyPath($"m/48'/0'/0'/{offerId}'");
            var derivedKey = extKey.Derive(path);

            return derivedKey.PrivateKey.PubKey.ToHex();

        }

        public static string OfferIdToHex(string offerId)
        {
            if (!long.TryParse(offerId, out long parsedId))
                throw new ArgumentException("Invalid offer ID");

            string hex = parsedId.ToString("X");
            return $"P-{hex}";
        }
    }
}
