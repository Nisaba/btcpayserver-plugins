using System.ComponentModel;

namespace BTCPayServer.Plugins.B2PCentral.Models
{
    /// <summary>
    /// Possible values for offers providers.
    /// </summary>
    public enum ProvidersEnum
    {
       [Description("empty")]
       None = 0,

        [Description("HodlHodl")]
        HodlHodl = 1,

        [Description("AgoraDesk. no longer works")]
        AgoraDesk = 2,

        [Description("RoboSats")]
        RoboSats = 3,

        [Description("Peach")]
        Peach = 4,

        [Description("reserved")]
        Reserved1 = 5,

        [Description("LocalCoinSwap")]
        LocalCoinSwap = 6,

        [Description("Paxful")]
        Paxful = 7,

        [Description("reserved")]
        Reserved2 = 8,

        [Description("LNp2pBot")]
        LNp2pBot = 9,

        [Description("reserved")]
        Reserved3 = 10,
    }
}
