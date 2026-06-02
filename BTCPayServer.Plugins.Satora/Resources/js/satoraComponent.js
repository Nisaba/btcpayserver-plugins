const SatoraCheckout = {
    template: '#satora-checkout-template',
    props: {
        model: {
            type: Object,
            required: true
        }
    },
    components: {
        qrcode: VueQrcode
    },
    data() {
        return {
            availableCryptos: window.satoraData ? window.satoraData.availableCryptos : {},
            selectedStablecoin: "",
            selectedBlockchain: "",
            isLoadingSwap: false,
            swapError: "",
            swapResult: null,
            lastSwapKey: "",
            swapAbortController: null,
            checkingStatus: false,
            swapStatus: null,
            qrOptions: {
                margin: 0,
                type: 'svg',
                color: { dark: '#000', light: '#fff' }
            }
        };
    },
    computed: {
        qrCodeData() {
            if (!this.swapResult || !this.swapResult.success) return null;

            const network = this.selectedBlockchain
                ? this.selectedBlockchain.toLowerCase().replace(/\s+/g, '')
                : 'crypto';
            const asset = this.selectedStablecoin ? this.selectedStablecoin : '';
            return `${network}:${this.swapResult.fromAddress}?amount=${this.swapResult.fromAmount}&asset=${asset}`;
        }
    },
    watch: {
        selectedStablecoin(newVal) {
            this.selectedBlockchain = "";
            this.swapResult = null;
            this.swapError = "";
            this.lastSwapKey = "";

            if (newVal && this.availableCryptos[newVal]) {
                const blockchains = this.availableCryptos[newVal];
                if (blockchains.length === 1) {
                    this.selectedBlockchain = blockchains[0];
                }
            }
        },
        selectedBlockchain() {
            this.tryCreateSwap();
        }
    },
    methods: {
        payInWallet() {
            if (!this.qrCodeData) return;
            window.location.href = this.qrCodeData;
        },
        async checkStatus() {
            if (!this.swapResult || !this.swapResult.swapId) return;

            this.checkingStatus = true;
            this.swapStatus = null;

            try {
                const response = await fetch(`${this.getSwapUrl()}/${this.swapResult.swapId}`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' }
                });

                if (!response.ok) {
                    throw new Error('Failed to fetch status');
                }
                this.swapStatus = await response.text() || 'Unknown';

            } catch (error) {
                console.error('Status check failed:', error);
                this.swapStatus = 'Failed to check status';
            } finally {
                this.checkingStatus = false;
            }
        },
        getCryptoIcon(cryptoCode) {
            return `/Resources/ico-satora/${cryptoCode}.webp`;
        },
        normalizeBlockchain(blockchain) {
            return (blockchain || "").replace(/ /g, "_");
        },
        parseAmount(value) {
            if (value === null || value === undefined) return 0;
            const normalized = value.toString().replace(/\s/g, "").replace(",", ".");
            const parsed = Number(normalized);
            return Number.isFinite(parsed) ? parsed : 0;
        },
        resolveBtcNetwork() {
            const paymentMethodId = (this.model.paymentMethodId || "").toUpperCase();
            if (paymentMethodId.includes("ARKADE")) {
                return "ARKADE";
            }
            if (paymentMethodId.includes("LIGHTNING") || paymentMethodId.includes("LN")) {
                return "LIGHTNING";
            }
            return "BTC";
        },
        getSwapUrl() {
            const rootPath = (this.model.rootPath || "/").replace(/\/?$/, "/");
            return `${rootPath}plugins/${encodeURIComponent(this.model.storeId)}/SatoraSwap`;
        },
        async tryCreateSwap() {
            if (!this.selectedStablecoin || !this.selectedBlockchain) {
                return;
            }

            const swapKey = `${this.selectedStablecoin}|${this.selectedBlockchain}|${this.model.invoiceId}`;
            if (swapKey === this.lastSwapKey) {
                return;
            }

            if (this.swapAbortController) {
                this.swapAbortController.abort();
            }
            this.swapAbortController = new AbortController();

            this.isLoadingSwap = true;
            this.swapError = "";
            this.swapResult = null;

            const payload = new URLSearchParams();
            payload.set("CryptoFrom", this.selectedStablecoin);
            payload.set("NetworkFrom", this.normalizeBlockchain(this.selectedBlockchain));
            payload.set("BtcAmount", this.parseAmount(this.model.due).toString());
            payload.set("BtcDestination", this.model.address || "");
            payload.set("BtcNetwork", this.resolveBtcNetwork());
            payload.set("BtcPayInvoiceId", this.model.invoiceId || "");

            try {
                const response = await fetch(this.getSwapUrl(), {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
                    },
                    body: payload.toString(),
                    signal: this.swapAbortController.signal
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }

                const result = await response.json();

                const success = result.success ?? result.Success;
                const fromAddress = result.fromAddress ?? result.FromAddress ?? "";
                const fromAmount = result.fromAmount ?? result.FromAmount ?? null;
                const swapId = result.swapId ?? result.SwapId ?? "";
                const statusMessage = result.statusMessage ?? result.StatusMessage ?? "";

                if (!success) {
                    throw new Error(statusMessage || "Swap creation failed.");
                }

                this.swapResult = {
                    success: true,
                    swapId,
                    statusMessage,
                    fromAddress,
                    fromAmount
                };
                this.lastSwapKey = swapKey;
            } catch (err) {
                if (err.name === "AbortError") {
                    return;
                }

                this.swapError = err?.message || "Unable to create swap.";
                this.swapResult = {
                    success: false,
                    statusMessage: this.swapError
                };
            } finally {
                this.isLoadingSwap = false;
            }
        }
    }
};

Vue.component('SatoraCheckout', SatoraCheckout);