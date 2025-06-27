namespace BTCPayServer.Plugins.MtPelerin.Model
{
    public class MtPelerinModel
    {
        public MtPelerinSettings Settings { get; set; }
        public MtPelerinSigningInfo SigningInfo { get; set; }

        public bool IsPayoutCreated { get; set; } = false;
    }
}
