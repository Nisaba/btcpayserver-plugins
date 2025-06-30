namespace BTCPayServer.Plugins.MtPelerin.Model
{
    public class MtPelerinOperation
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string MtPelerinId { get; set; }
        public bool IsOnChain { get; set; }
        public string LnInvoice { get; set; }
      
    }
}
