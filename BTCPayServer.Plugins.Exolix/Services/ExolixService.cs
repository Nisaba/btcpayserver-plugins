using BTCPayServer.Plugins.Exolix.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Exolix.Services
{
    public class ExolixService(ILogger<ExolixService> logger, HttpClient httpClient)
    {
        public const string BaseUrl = "https://api.b2p-central.com/api/greenexo/";

        /*    public async Task<SwapCreationResponse> CreateSwapAsync(SwapCreationRequest req)
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
            }*/

        public async Task<SwapCreationResponse> CreateSwapAsync(SwapCreationRequest req)
        {
            try
            {
                var b2pReq = new B2PSwapCreationRequest()
                {
                    FromCrypto = req.FromCrypto,
                    FromNetwork = req.FromNetwork,
                    FromAmount = req.FromAmount,
                    ToAmount = req.ToAmount,
                    ToCrypto = req.ToCrypto,
                    ToNetwork = req.ToNetwork,
                    ToAddress = req.ToAddress,
                    FromRefundAddress = req.FromRefundAddress ?? string.Empty,
                    IsFixed = req.FromAmount == 0
                };

                var reqJson = JsonConvert.SerializeObject(b2pReq, Formatting.None);
                var webRequest = new HttpRequestMessage(HttpMethod.Post, "Create")
                {
                    Content = new StringContent(reqJson, Encoding.UTF8, "application/json"),
                };

                using var rep = await httpClient.SendAsync(webRequest);
                var sRep = await rep.Content.ReadAsStringAsync();

                if (!rep.IsSuccessStatusCode)
                {
                    string errorMessage = "";

                    try
                    {
                        dynamic apiError = JsonConvert.DeserializeObject(sRep);
                        string fullErrorString = apiError?.error;

                        if (!string.IsNullOrEmpty(fullErrorString))
                        {
                            errorMessage = fullErrorString;

                            int jsonStartIndex = fullErrorString.IndexOf(" - {");
                            if (jsonStartIndex >= 0)
                            {
                                string innerJson = fullErrorString.Substring(jsonStartIndex + 3);
                                dynamic exolixError = JsonConvert.DeserializeObject(innerJson);

                                if (exolixError?.error != null)
                                {
                                    errorMessage = exolixError.error;
                                }
                            }
                        }
                        else
                        {
                            errorMessage = sRep; 
                        }
                    }
                    catch
                    {
                        errorMessage = sRep;
                    }

                    throw new Exception(errorMessage);
                }

                return JsonConvert.DeserializeObject<SwapCreationResponse>(sRep);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ExolixPlugin:CreateSwapAsync()");
                throw;
            }
        }

        public async Task<string> GetSwapInfoAsync(string id)
        {
            string sRep = "";
            try
            {
                var response = await httpClient.GetAsync($"{BaseUrl}Info?swapId={id}");
                sRep = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "";

                    try
                    {
                        dynamic apiError = JsonConvert.DeserializeObject(sRep);
                        string fullErrorString = apiError?.error;

                        if (!string.IsNullOrEmpty(fullErrorString))
                        {
                            errorMessage = fullErrorString;

                            int jsonStartIndex = fullErrorString.IndexOf(" - {");
                            if (jsonStartIndex >= 0)
                            {
                                string innerJson = fullErrorString.Substring(jsonStartIndex + 3);
                                dynamic exolixError = JsonConvert.DeserializeObject(innerJson);

                                if (exolixError?.error != null)
                                {
                                    errorMessage = exolixError.error;
                                }
                            }
                        }
                        else
                        {
                            errorMessage = sRep;
                        }
                    }
                    catch
                    {
                        errorMessage = sRep;
                    }

                    throw new Exception(errorMessage);
                }
                return sRep;
            }
            catch (Exception ex)
            {
                logger.LogError($"ExolixPlugin.GetSwapInfo(): {ex.Message} - {sRep} - {id}");
                throw;
            }
        }

     /*   private static string GetNetwork(string crypto) => crypto switch
        {
            "BNB" => "BSC",
            "POL" => "MATIC",
            _ => crypto
        };*/

    }
}
