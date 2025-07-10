using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Model
{
    public class PeachUser
    {
        public string Id { get; set; }
        public int NbTrades { get; set; }
        public int OpenedTrades { get; set; }
        public int CanceledTrades { get; set; }
        public int Rating { get; set; }
        public int RatingCount { get; set; }
        public List<string> Medals { get; set; }
        public int OpenedDisputes { get; set; }
        public int WonDisputes { get; set; }
        public int LostDisputes { get; set; }
        public int ResolvedDisputes { get; set; }
    }
}
