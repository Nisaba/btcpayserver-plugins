using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BTCPayServer.Plugins.Exolix.Model;

namespace BTCPayServer.Plugins.Exolix.Services
{
    public class ExolixService
    {
        private readonly string BaseUrl = "https://exolix.com/api/v2/";
        private readonly string APIKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImVsbGlwc2UtMjFAbmlzYWJhLXNvbHV0aW9ucy5jb20iLCJzdWIiOjQxOTY0LCJpYXQiOjE3NDc1MjIxNjksImV4cCI6MTkwNTMxMDE2OX0.thq7I9--XW7DoPlSg9lGCqaU804h_fYk3EUOkMGSEag";

        private readonly HttpClient _httpClient;
        private readonly ILogger<ExolixService> _logger;

        public ExolixService(ILogger<ExolixService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {APIKey}");
        }

        public async Task<SwapCreationResponse> CreateSwapAsync(SwapCreationRequest req)
        {
            string sRep = "";
            try
            {
                var createSwapRequest = new Dictionary<string, object>
                {
                    ["coinFrom"] = req.FromCrypto,
                    ["networkFrom"] = req.FromNetwork,
                    ["coinTo"] = req.ToCrypto,
                    ["networkTo"] = req.ToNetwork,
                    ["amount"] = req.FromAmount,
                    ["withdrawalAmount"] = req.ToAmount,
                    ["withdrawalAddress"] = req.ToAddress,
                    ["rateType"] = "fixed"
                };

                if (!string.IsNullOrEmpty(req.FromRefundAddress))
                    createSwapRequest["refundAddress"] = req.FromRefundAddress;

                var createJson = JsonConvert.SerializeObject(createSwapRequest);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, $"transactions")
                {
                    Content = new StringContent(createJson, Encoding.UTF8, "application/json"),
                };
                using (var rep = await _httpClient.SendAsync(webRequest))
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
                };
                _logger.LogInformation($"Exolix Swap Created : {swap.SwapId} {req.FromCrypto}");
                return swap;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ExolixPlugin.CreateSwap(): {ex.Message} - {sRep} - {req.FromCrypto} {req.ToCrypto}");
                if (string.IsNullOrEmpty(sRep))
                {
                    throw;
                } else
                {
                    dynamic JsonRep = JsonConvert.DeserializeObject<dynamic>(sRep);
                    string sMsg = JsonRep.message;
                    throw new Exception(sMsg);
                }
            }
        }

    }
}
