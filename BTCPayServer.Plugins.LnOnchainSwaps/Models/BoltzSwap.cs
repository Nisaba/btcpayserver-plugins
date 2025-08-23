using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Models
{
    public class BoltzSwap
    {
        public const string SwapTypeOnChainToLn = "onchain_to_ln";
        public const string SwapTypeLnToOnChain = "ln_to_onchain";

        [Key]
        public string SwapId { get; set; }
        public string StoreId { get; set; }

        public DateTime DateT { get; set; }
        public string Type { get; set; } // "onchain_to_ln" or "ln_to_onchain"
        public string Status { get; set; }
        public string PreImage { get; set; }
        public string PreImageHash { get; set; }
        public string Destination { get; set; } // BtcAddress or LnInvoice
        public decimal OriginalAmount { get; set; }
        public decimal ExpectedAmount { get; set; }
        public string BTCPayInvoiceId { get; set; }
        public string BTCPayPullPaymentId { get; set; }
        public string BTCPayPayoutId { get; set; }
        public string Json { get; set; }

        public string RefundSignature { get; set; }


        [NotMapped]
        public string HighlightJson
        {
            get
            {
                return DoHighlightJson(Json);
            }
        }

        private string DoHighlightJson(string sJson)
        {
            var doc = JsonDocument.Parse(sJson);
            string pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            pretty = System.Net.WebUtility.HtmlEncode(pretty);

            pretty = Regex.Replace(pretty,
                @"(""(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\\""])*""(\s*:)?|\b(true|false|null)\b|-?\d+(\.\d*)?([eE][+\-]?\d+)?)",
                match =>
                {
                    string cls = "number";
                    string val = match.Value;

                    if (val.StartsWith("\""))
                    {
                        if (val.EndsWith(":"))
                            cls = "key";
                        else
                            cls = "string";
                    }
                    else if (val == "true" || val == "false")
                    {
                        cls = "boolean";
                    }
                    else if (val == "null")
                    {
                        cls = "null";
                    }

                    return $"<span class=\"{cls}\">{val}</span>";
                });
            return $"<pre>{pretty}</pre>";
        }
    }
}
