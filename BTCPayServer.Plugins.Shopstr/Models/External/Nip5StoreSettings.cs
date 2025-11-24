#nullable enable
using BTCPayServer;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models.External
{
    public class Nip5StoreSettings
    {
        [Required] public string PubKey { get; set; }

        public string? PrivateKey { get; set; }
        [Required] public string Name { get; set; }

        public string[]? Relays { get; set; }

        public bool IsConfigured => !string.IsNullOrEmpty(PubKey) && !string.IsNullOrEmpty(PrivateKey)
                                    && Relays != null && Relays.Length > 0;
    }
}
