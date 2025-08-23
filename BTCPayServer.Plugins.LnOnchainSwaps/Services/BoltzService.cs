using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Crypto;
using NBitpayClient;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Services
{
    public class BoltzService
    {
        private const string BaseUrl = "https://api.boltz.exchange/v2/";
        private const string Referral = "nisaba";

        private readonly HttpClient _httpClient;
        private readonly ILogger<BoltzService> _logger;

        public BoltzService(ILogger<BoltzService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<BoltzSwap> CreateOnChainToLnSwapAsync(string lnInvoice, string pubKey, string preImageHash)
        {
            string sRep = "";
            try
            {
                var swapRequest = new Dictionary<string, object>
                {
                    ["from"] = "BTC",
                    ["to"] = "BTC",
                    ["refundPublicKey"] = pubKey,
                    ["invoice"] = lnInvoice,
                    ["referralId"] = Referral,
                    ["preimageHash"] = preImageHash
                };

                var swapJson = JsonConvert.SerializeObject(swapRequest);
                var webRequest = new HttpRequestMessage(HttpMethod.Post, "swap/submarine")
                {
                    Content = new StringContent(swapJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);

                _logger.LogInformation($"Boltzswap created: {JsonRep.id}");
                return new BoltzSwap
                {
                    DateT = DateTime.UtcNow,
                    Type = BoltzSwap.SwapTypeOnChainToLn,
                    Status = string.Empty,
                    PreImage = string.Empty,
                    PreImageHash = preImageHash,
                    SwapId = JsonRep.id,
                    Destination = JsonRep.address,
                    ExpectedAmount = (decimal)JsonRep.expectedAmount / 100000000m,
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

        public async Task<BoltzSwap> CreateLnToOnChainSwapAsync(string btcReceptionAdress, Key privateKey, decimal btcAmount)
        {
            string sRep = "";
            try
            {
                GeneratePreimageHash(out var preImage, out var preImageHash);
                var swapRequest = new Dictionary<string, object>
                {
                    ["from"] = "BTC",
                    ["to"] = "BTC",
                    ["claimPublicKey"] = privateKey.PubKey.ToHex(),
                    ["address"] = btcReceptionAdress,
                    ["addressSignature"] = SignMessage(btcReceptionAdress, privateKey),
                    ["onchainAmount"] = btcAmount * 100000000,
                    ["referralId"] = Referral,
                    ["preimageHash"] = preImageHash
                };

                var swapJson = JsonConvert.SerializeObject(swapRequest);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, "swap/reverse")
                {
                    Content = new StringContent(swapJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await _httpClient.SendAsync(webRequest))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);

                _logger.LogInformation($"Boltzswap created: {JsonRep.id}");
                return new BoltzSwap
                {
                    DateT = DateTime.UtcNow,
                    Type = BoltzSwap.SwapTypeOnChainToLn,
                    Status = string.Empty,
                    PreImage = preImage,
                    PreImageHash = preImageHash,
                    SwapId = JsonRep.id,
                    Destination = JsonRep.invoice,
                    ExpectedAmount = (decimal)JsonRep.expectedAmount / 100000000m,
                    Json = sRep
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"LnOnchainSwapsPlugin.CreateLnToOnChainSwap(): {ex.Message} - {sRep}");
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

        public async Task<string> GetSwapStatusAsync(string swapId)
        {
            string sRep = "";
            try
            {
                using (var rep = await _httpClient.GetAsync($"swap/{swapId}"))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                return JsonRep.status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"LnOnchainSwapsPlugin.GetSwapStatus(): {ex.Message} - {sRep}");
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

        public async Task<string> GetSubmarineRefundSignatureAsync(string swapId)
        {
            string sRep = "";
            try
            {
                using (var rep = await _httpClient.GetAsync($"swap/submarine/{swapId}/refund"))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                return JsonRep.signature;
            }
            catch (Exception ex)
            {
                _logger.LogError($"LnOnchainSwapsPlugin.GetSubmarineRefundInfos(): {ex.Message}");
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
                throw;
            }
        }

        private void GeneratePreimageHash(out string preImage, out string preImageHash)
        {
            byte[] data = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            preImage = BitConverter.ToString(data).Replace("-", "").ToLower();
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                preImageHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private string SignMessage(string message, Key privKey)
        {
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
                    return BitConverter.ToString(compactSig).Replace("-", "").ToLower();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "LnOnchainSwapsPlugin:SignMessage()");
                throw;
            }
        }

    }
}
