#if BOLTZ_SUPPORT
using BTCPayServer.Plugins.Boltz;
using Grpc.Core;
#endif

using Microsoft.Extensions.Logging;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Services
{
    public class BoltzWrapper(
#if BOLTZ_SUPPORT
    BoltzService boltzService,
#endif
    ILogger<BoltzWrapper> logger
    )
    {
    }
}
