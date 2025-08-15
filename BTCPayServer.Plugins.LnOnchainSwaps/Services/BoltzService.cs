using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using Microsoft.Extensions.Logging;
using NBitpayClient;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Services
{
    public class BoltzService
    {
        private const string BaseUrl = "https://api.boltz.exchange/v2";
        private const string Referral = "xxx";

        private readonly HttpClient _httpClient;
        private readonly ILogger<BoltzService> _logger;

        public BoltzService(ILogger<BoltzService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<BoltzSwap> CreateOnChainToLnSwapAsync(string lnInvoice)
        {
            string sRep = "";
            try
            {
                var preImageHash = GeneratePreimageHash();
                var swapRequest = new Dictionary<string, object>
                {
                    ["from"] = "BTC",
                    ["to"] = "BTC",
                    ["invoice"] = lnInvoice,
                    ["referralId"] = Referral,
                    ["preimageHash"] = preImageHash
                };

                var swapJson = JsonConvert.SerializeObject(swapRequest);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, $"swap/submarine")
                {
                    Content = new StringContent(swapJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);

                return new BoltzSwap
                {
                    Type = BoltzSwap.SwapTypeOnChainToLn,
                    PreImageHash = preImageHash,
                    SwapId = JsonRep.id,
                    Destination = JsonRep.address,
                    ExpectedAmount = JsonRep.expectedAmount,
                    Json = sRep
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"LnOnchainSwapsPlugin.CreateOnChainToLnSwap(): {ex.Message} - {sRep}");
                if (string.IsNullOrEmpty(sRep))
                {
                    throw;
                }
                else
                {
                    dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                    string sMsg = JsonRep.error;
                    throw new Exception(sMsg);
                }
            }
        }

        public async Task<BoltzSwap> CreateLnToOnChainSwapAsync(string btcReceptionAdress, decimal btcAmount)
        {
            string sRep = "";
            try
            {
                var preImageHash = GeneratePreimageHash();
                var swapRequest = new Dictionary<string, object>
                {
                    ["from"] = "BTC",
                    ["to"] = "BTC",
                    ["claimAddress"] = btcReceptionAdress,
                    ["onchainAmount"] = btcAmount,
                    ["referralId"] = Referral,
                    ["preimageHash"] = preImageHash
                };

                var swapJson = JsonConvert.SerializeObject(swapRequest);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, $"swap/reverse")
                {
                    Content = new StringContent(swapJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);

                return new BoltzSwap
                {
                    Type = BoltzSwap.SwapTypeOnChainToLn,
                    PreImageHash = preImageHash,
                    SwapId = JsonRep.id,
                    Destination = JsonRep.invoice,
                    ExpectedAmount = JsonRep.expectedAmount,
                    Json = sRep
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"LnOnchainSwapsPlugin.CreateOnChainToLnSwap(): {ex.Message} - {sRep}");
                if (string.IsNullOrEmpty(sRep))
                {
                    throw;
                }
                else
                {
                    dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                    string sMsg = JsonRep.error;
                    throw new Exception(sMsg);
                }
            }
        }
        public async Task<LnOnchainSwapsOperation> GetSwapStatusAsync(string swapId)
        {
            // Implement the logic to get the status of a swap using Boltz API
            throw new NotImplementedException();
        }

        private string GeneratePreimageHash()
        {
            byte[] data = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
