using BTCPayServer.Plugins.Exolix.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Services
{
    public class ExolixService(ILogger<ExolixService> logger, HttpClient httpClient)
    {
        public const string BaseUrl = "https://exolix.com/api/v2/";
        public const string APIKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImVsbGlwc2UtMjFAbmlzYWJhLXNvbHV0aW9ucy5jb20iLCJzdWIiOjQxOTY0LCJpYXQiOjE3NDc1MjIxNjksImV4cCI6MTkwNTMxMDE2OX0.thq7I9--XW7DoPlSg9lGCqaU804h_fYk3EUOkMGSEag";

        public async Task<SwapCreationResponse> CreateSwapAsync(SwapCreationRequest req)
        {
            string sRep = "";
            try
            {
                var createSwapRequest = new Dictionary<string, object>
                {
                    ["coinFrom"] = req.FromCrypto,
                    ["networkFrom"] = GetNetwork(req.FromNetwork),
                    ["coinTo"] = req.ToCrypto,
                    ["networkTo"] = GetNetwork(req.ToNetwork) ?? "BTC",
                    ["amount"] = req.FromAmount,
                    ["withdrawalAddress"] = req.ToAddress,
                    ["rateType"] = "fixed"
                };

                if (req.ToAmount > 0)
                    createSwapRequest["withdrawalAmount"] = req.ToAmount;

                if (!string.IsNullOrEmpty(req.FromRefundAddress))
                    createSwapRequest["refundAddress"] = req.FromRefundAddress;

                var createJson = JsonConvert.SerializeObject(createSwapRequest);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, $"transactions")
                {
                    Content = new StringContent(createJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await httpClient.SendAsync(webRequest))
                {
                    sRep = await rep.Content.ReadAsStringAsync();
                    rep.EnsureSuccessStatusCode();
                }
                dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                var swap = new SwapCreationResponse()
                {
                    StatusMessage = JsonRep.status,
                    SwapId = JsonRep.id,
                    FromAddress = JsonRep.depositAddress,
                    FromAmount = Convert.ToSingle(JsonRep.amount),
                    ToAmount = Convert.ToSingle(JsonRep.amountTo)
                };
                logger.LogInformation($"Exolix Swap Created : {swap.SwapId} {req.FromCrypto} {req.ToCrypto}");
                return swap;
            }
            catch (Exception ex)
            {
                logger.LogError($"ExolixPlugin.CreateSwap(): {ex.Message} - {sRep} - {req.FromCrypto} {req.ToCrypto}");
                if (string.IsNullOrEmpty(sRep))
                {
                    throw;
                } else
                {
                    dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                    string sMsg = JsonRep.message ?? JsonRep.error;
                    throw new Exception(sMsg);
                }
            }
        }

        public async Task<string> GetSwapInfoAsync(string id)
        {
            string sRep = "";
            try
            {
                var response = await httpClient.GetAsync($"{BaseUrl}transactions/{id}");
                sRep = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                return sRep;
            }
            catch (Exception ex)
            {
                logger.LogError($"ExolixPlugin.GetSwapInfo(): {ex.Message} - {sRep} - {id}");
                throw;
            }
        }


        private static string GetNetwork(string crypto) => crypto switch
        {
            "BNB" => "BSC",
            "POL" => "MATIC",
            _ => crypto
        };

    }
}
