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
            return `${this.swapResult.fromAddress}?amount=${this.swapResult.fromAmount}`;
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
            window.open(this.qrCodeData, '_blank', 'noopener,noreferrer');
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
                this.swapStatus = await response || 'Unknown';

            } catch (error) {
                console.error('Status check failed:', error);
                this.swapStatus = 'Failed to check status';
            } finally {
                this.checkingStatus = false;
            }
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
            if (paymentMethodId.includes("LIGHTNING") || paymentMethodId.includes("LN")) {
                return "LIGHTNING";
            }
            if (paymentMethodId.includes("ARKADE")) {
                return "ARKADE";
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

                this.swapResult = await response.json();

                if (!swapResult.success) {
                    throw new Error(statusMessage || "Swap creation failed.");
                }
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