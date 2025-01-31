using BTCPayServer.Plugins.Ecwid.Data;

namespace BTCPayServer.Plugins.Ecwid.Model
{
    public struct EcwidModel
    {
        public EcwidSettings Settings { get; set; }
        public string EcwidPluginUrl { get; set; }
    }
}
