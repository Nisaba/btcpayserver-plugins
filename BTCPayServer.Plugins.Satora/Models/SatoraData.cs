using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BTCPayServer.Plugins.Satora.Models
{
    public enum Blockchains
    {
        Arbitrum,
        //Avalanche,
        //Base,
        Ethereum,
        //Conflux_eSpace,
        //Corn,
        //Berachain,
        //Flare,
        //Hedera,
        //HyperEVM,
        //Ink,
        //Linea,
        //Lnk,
        //Mantle,
        //MegaETH,
        //Monad,
        //Morph,
        //Optimism,
        //Plasma,
        Polygon,
        //Rootstock,
        //Sei,
        //Solana,
        //Sonic,
        //Stable,
        //Tempo,
        //Unichain,
        //World_Chain,
        //XLayer
    }

    public enum Stablecoins
    {
        BTC,
        EURC,
        USAT,
        USDC,
        USDT,
        USDT0,
        XAUt,
        tBTC,
        WBTC
    }

    public static class SatoraData
    {
        public static readonly IReadOnlyDictionary<Stablecoins, List<Blockchains>> AvailableCryptos = new ReadOnlyDictionary<Stablecoins, List<Blockchains>>(
            new Dictionary<Stablecoins, List<Blockchains>>
            {
                { Stablecoins.EURC, new List<Blockchains> { Blockchains.Ethereum } },
                { Stablecoins.tBTC, new List<Blockchains> { Blockchains.Ethereum, Blockchains.Arbitrum } },
                { Stablecoins.USAT, new List<Blockchains> { Blockchains.Ethereum } },
                { Stablecoins.USDC, new List<Blockchains> {
                    Blockchains.Polygon,
                    Blockchains.Ethereum,
                    Blockchains.Arbitrum,
                    //Blockchains.Avalanche,
                    //Blockchains.Optimism,
                    //Blockchains.Solana,
                    //Blockchains.Base,
                    //Blockchains.Unichain,
                    //Blockchains.Linea,
                    //Blockchains.Sonic,
                    //Blockchains.World_Chain,
                    //Blockchains.Monad,
                    //Blockchains.Sei,
                    //Blockchains.HyperEVM,
                    //Blockchains.Ink
                } },
                { Stablecoins.USDT, new List<Blockchains> {
                    Blockchains.Ethereum,
                    //Blockchains.Optimism,
                    //Blockchains.Solana,
                    //Blockchains.Base,
                    //Blockchains.Unichain,
                    //Blockchains.Linea,
                    //Blockchains.Sonic,
                    //Blockchains.World_Chain,
                    //Blockchains.Monad,
                    //Blockchains.Sei,
                    //Blockchains.HyperEVM,
                    //Blockchains.Ink,
                    //Blockchains.Mantle,
                    //Blockchains.Conflux_eSpace,
                    //Blockchains.XLayer,
                    //Blockchains.Corn,
                    //Blockchains.Rootstock,
                    //Blockchains.Berachain,
                    //Blockchains.Plasma,
                    //Blockchains.Stable,
                    //Blockchains.MegaETH,
                    //Blockchains.Tempo,
                    //Blockchains.Flare,
                    //Blockchains.Hedera,
                    //Blockchains.Morph
                } },
                { Stablecoins.USDT0, new List<Blockchains> { Blockchains.Polygon, Blockchains.Arbitrum } },
                { Stablecoins.WBTC, new List<Blockchains> { Blockchains.Polygon, Blockchains.Ethereum, Blockchains.Arbitrum } },
                { Stablecoins.XAUt, new List<Blockchains> { Blockchains.Ethereum } }
            }
        );
    }
}