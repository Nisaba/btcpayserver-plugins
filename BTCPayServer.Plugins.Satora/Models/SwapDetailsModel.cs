 namespace BTCPayServer.Plugins.Satora.Models
{
    // View model for the details page. Combines the local DB row (what
    // the customer paid against, our own invoice id, etc.) with the
    // current backend swap state pulled fresh from the Satora API. Both
    // sides nullable on the model so the page renders even when one
    // lookup fails — e.g. backend unreachable, but we can still show the
    // local row.
    public class SwapDetailsModel
    {
        public string StoreId { get; set; } = "";
        public SatoraTx? LocalTx { get; set; }

        // Backend-side fields. All null when the backend lookup fails;
        // the error is surfaced in BackendError.
        public string? BackendStatus { get; set; }
        public string? DepositAddress { get; set; }
        public string? DepositAmount { get; set; }
        public string? DepositToken { get; set; }
        public string? ReceiveAddress { get; set; }
        public string? ReceiveAmount { get; set; }
        public string? ReceiveToken { get; set; }
        public string? BackendError { get; set; }

        // Default destination for the Claim step — derived from the
        // plugin's mnemonic at page-render time so the operator can see
        // (and override) what would be used.
        public string? DerivedArkadeAddress { get; set; }
    }
}
