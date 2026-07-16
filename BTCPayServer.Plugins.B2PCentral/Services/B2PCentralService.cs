using BTCPayServer.Plugins.B2PCentral.Models;
using BTCPayServer.Plugins.B2PCentral.Models.P2P;
using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.B2PCentral.Services
{
    public class B2PCentralService (HttpClient httpClient, ILogger<B2PCentralService> logger)
    {
        // public const string BaseApiUrl = "https://localhost:7137/api/";
        public const string BaseApiUrl = "https://api.b2p-central.com/api/";


        public async Task<List<B2POffer>> GetOffersListAsync(OffersRequest req, string key)
        {
            try
            {
                var reqJson = JsonConvert.SerializeObject(req, Formatting.None);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, "Offers")
                {
                    Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
                    Headers =
                {
                    { "B2P-API-KEY", key },
                },
                };

                using var rep = await httpClient.SendAsync(webRequest);
                rep.EnsureSuccessStatusCode();
                var sRep = await rep.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<B2POffer>>(sRep);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "B2PCentral:GetOffersListAsync()");
                throw;
            }
        }

        public async Task<List<B2PSwap>> GetSwapsListAsync(SwapRateRequest req, string key)
        {
            try
            {
                var reqJson = JsonConvert.SerializeObject(req, Formatting.None);

                var webRequest = new HttpRequestMessage(HttpMethod.Post, "swaps")
                {
                    Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
                    Headers =
                {
                    { "B2P-API-KEY", key },
                },
                };

                using var rep = await httpClient.SendAsync(webRequest);
                rep.EnsureSuccessStatusCode();
                var sRep = await rep.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<B2PSwapResponse>(sRep).Swaps;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "B2PCentral:GetSwapsListAsync()");
                throw;
            }
        }

        public async Task<SwapCreationResponse> CreateSwapAsync(SwapCreationRequest req, string key)
        {
            try
            {
                var reqJson = JsonConvert.SerializeObject(req, Formatting.None);
                var webRequest = new HttpRequestMessage(HttpMethod.Put, "swaps")
                {
                    Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
                    Headers =
                {
                    { "B2P-API-KEY", key },
                },
                };

                using var rep = await httpClient.SendAsync(webRequest);
                rep.EnsureSuccessStatusCode();
                var sRep = await rep.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<SwapCreationResponse>(sRep);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "B2PCentral:CreateSwapAsync()");
                throw;
            }
        }

        public async Task<List<ProviderInfo>> GetSwapProvidersInfos(string key)
        {
            string sRep = "";
            try
            {
                var webRequest = new HttpRequestMessage(HttpMethod.Get, "swaps/providersinfos")
                {
                    Headers =
                    {
                        { "B2P-API-KEY", key },
                    },
                };

                using var rep = await httpClient.SendAsync(webRequest);

                sRep = await rep.Content.ReadAsStringAsync();
                rep.EnsureSuccessStatusCode();
                return JsonConvert.DeserializeObject<List<ProviderInfo>>(sRep);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "B2PCentral:GetSwapProvidersInfos()");
                throw;
            }
        }


    }
}
