const ExolixCheckout = {
    template: '#exolix-checkout-template',
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
            selectedCrypto: null,
            swapData: null,
            loading: false,
            error: null,
            checkingStatus: false,
            swapStatus: null,
            qrOptions: {
                margin: 0,
                type: 'svg',
                color: { dark: '#000', light: '#fff' }
            },
            protocolMap: {
                'XMR': 'monero',
                'LTC': 'litecoin',
                'DOGE': 'dogecoin',
                'ETH': 'ethereum',
                'TRX': 'tron',
                'POL': 'polygon',
                'BNB': 'bnb',
                'ADA': 'cardano',
                'SOL': 'solana',
                'DAI': 'ethereum',
                'USDT-ETH': 'ethereum-usdt',
                'USDT-TRX': 'tron-usdt',
                'USDT-BSC': 'bnb-usdt',
                'USDT-SOL': 'solana-usdt',
                'USDT-NEAR': 'near-usdt',
                'USDT-MATIC': 'polygon-usdt',
                'USDT-TON': 'ton-usdt',
                'USDT-AVAXC': 'avalanche-usdt',
                'USDC-ETH': 'ethereum-usdc',
                'USDC-BSC': 'bnb-usdc',
                'USDC-SOL': 'solana-usdc',
                'USDC-NEAR': 'near-usdc',
                'USDC-MATIC': 'polygon-usdc',
                'USDC-AVAXC': 'avalanche-usdc',
                'EURT-ETH': 'ethereum-eurt',
                'EURI-ETH': 'ethereum-euri',
                'EURI-BSC': 'bnb-euri',
                'DEURO-ETH': 'ethereum-deuro'
            },
            manualAmount: '',
            showAmountInput: false,

            searchQuery: '',
            lastSearchQuery: '',
            searchResults: [],
            searching: false,
            searchError: null,
            searchDone: false,

            selectedSearchCoin: null   // { code, networkCode, coinIcon, networkIcon, networkName }
        }
    },
    methods: {

        getProtocol(cryptoCode) {
            return this.protocolMap[cryptoCode] || cryptoCode.toLowerCase();
        },

        buildPaymentUrl() {
            if (!this.swapData) return null;

            if (this.selectedSearchCoin) {
                const key = this.selectedSearchCoin.networkCode;
                const protocol = this.protocolMap[key] || key.toLowerCase();
                return `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
            }

            if (!this.selectedCrypto) return null;
            const protocol = this.getProtocol(this.selectedCrypto);
            return `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
        },

        payInWallet() {
            window.location.href = this.buildPaymentUrl();
        },

        // ─── Formatting ──────────────────────────────────────────────────────

        asNumber(val) {
            return val && parseFloat(val.toString().replace(/\s/g, ''));
        },

        formatAmount(amount) {
            return parseFloat(amount).toFixed(8);
        },

        getCryptoIcon(cryptoCode) {
            return `/Resources/ico/${cryptoCode.substring(0, 4).replace("-", "")}.webp`;
        },

        // ─── Standard swap (dropdown selection) ─────────────────────────────

        async createSwap() {
            if (!this.selectedCrypto) return;

            this.loading = true;
            this.error = null;
            this.swapData = null;

            try {
                var btcAmount = this.asNumber(this.model.due);
                if (btcAmount == 0) {
                    this.error = 'Please enter a BTC amount first';
                }
                if (btcAmount > 3) {
                    btcAmount /= 100000000;
                }

                const sInvoiceBitcoinUrl = this.model.invoiceBitcoinUrl || '';
                let sAddress = this.model.address;

                let isLightning = false;
                if (window.exolixData.AllowLightning == 1) {
                    isLightning = sInvoiceBitcoinUrl.includes('lnbc');

                    if (isLightning && !sAddress.includes("lnbc")) {
                        const lightningPart = sInvoiceBitcoinUrl.split('lightning')[1];
                        if (lightningPart) {
                            sAddress = lightningPart.replace(':', '').replace('=', '');
                        }
                    }
                }

                const formData = new FormData();
                formData.append('CryptoFrom', this.selectedCrypto);
                formData.append('BtcAddress', sAddress);
                formData.append('BtcAmount', btcAmount);
                formData.append('BtcNetwork', isLightning ? "LIGHTNING" : "BTC");
                formData.append('BtcPayInvoiceId', window.exolixData.invoiceId);

                const response = await fetch(`/plugins/${window.exolixData.storeId}/ExolixSwap`, {
                    method: 'POST',
                    body: formData
                });
                const result = await response.json();

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                if (!result.success) {
                    this.error = result.statusMessage || 'Swap creation failed';
                    return;
                }
                this.swapData = result;
            } catch (e) {
                this.error = e.message || 'Failed to create swap. Please try again.';
            } finally {
                this.loading = false;
            }
        },

        // ─── Search feature ──────────────────────────────────────────────────

        async searchExolixCurrencies() {
            const query = this.searchQuery.trim();
            if (!query) return;

            this.searching = true;
            this.searchError = null;
            this.searchResults = [];
            this.searchDone = false;
            this.lastSearchQuery = query;

            try {
                const response = await fetch(
                    `https://exolix.com/api/v2/currencies?withNetworks=true&platformType=ALL&direction=from&search=${encodeURIComponent(query)}`,
                    { headers: { 'Accept': 'application/json' } }
                );

                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

                const data = await response.json();
                this.searchResults = data.data || [];
            } catch (e) {
                this.searchError = 'Failed to reach Exolix. Please try again.';
            } finally {
                this.searching = false;
                this.searchDone = true;
            }
        },

        selectFromSearch(coin, network) {
            this.selectedSearchCoin = {
                code: coin.code,
                networkCode: network.network,
                coinIcon: coin.icon,
                networkIcon: network.icon || null,
                networkName: network.name
            };

            this.searchResults = [];
            this.searchQuery = '';
            this.searchDone = false;

            this.swapData = null;
            this.error = null;
            this.swapStatus = null;

            this.createSwapFromSearch(coin.code, network.network);
        },

        // ─── Swap from search result (isTrueNetwork=true) ───────────────────

        async createSwapFromSearch(coinCode, networkCode) {
            this.loading = true;
            this.error = null;
            this.swapData = null;

            try {
                let btcAmount = this.asNumber(this.model.due);
                if (!btcAmount || btcAmount == 0) {
                    this.error = 'Please enter a BTC amount first';
                    return;
                }
                if (btcAmount > 3) {
                    btcAmount /= 100000000;
                }

                const sInvoiceBitcoinUrl = this.model.invoiceBitcoinUrl || '';
                let sAddress = this.model.address;
                let isLightning = false;

                if (window.exolixData.AllowLightning == 1) {
                    isLightning = sInvoiceBitcoinUrl.includes('lnbc');
                    if (isLightning && !sAddress.includes("lnbc")) {
                        const lightningPart = sInvoiceBitcoinUrl.split('lightning')[1];
                        if (lightningPart) {
                            sAddress = lightningPart.replace(':', '').replace('=', '');
                        }
                    }
                }

                const formData = new FormData();
                formData.append('CryptoFrom', coinCode + '-' + networkCode);
                formData.append('BtcAddress', sAddress);
                formData.append('BtcAmount', btcAmount);
                formData.append('BtcNetwork', isLightning ? "LIGHTNING" : "BTC");
                formData.append('BtcPayInvoiceId', window.exolixData.invoiceId);

                const response = await fetch(
                    `/plugins/${window.exolixData.storeId}/ExolixSwap?isTrueNetwork=true`,
                    { method: 'POST', body: formData }
                );
                const result = await response.json();

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                if (!result.success) {
                    this.error = result.statusMessage || 'Swap creation failed';
                    return;
                }
                this.swapData = result;
            } catch (e) {
                this.error = e.message || 'Failed to create swap. Please try again.';
            } finally {
                this.loading = false;
            }
        },

        // ─── Status check ────────────────────────────────────────────────────

        async checkStatus() {
            if (!this.swapData || !this.swapData.swapId) return;

            this.checkingStatus = true;
            try {
                const response = await fetch(`/plugins/${window.exolixData.storeId}/ExolixSwap/${this.swapData.swapId}`, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' }
                });

                if (!response.ok) throw new Error('Failed to fetch status');

                const result = await response.json();
                this.swapStatus = result.status || 'Unknown';
            } catch (error) {
                console.error('Status check failed:', error);
                this.swapStatus = 'Failed to check status';
            } finally {
                this.checkingStatus = false;
            }
        },

        // ─── Manual amount ───────────────────────────────────────────────────

        validateManualAmount() {
            const amount = this.asNumber(this.manualAmount);
            if (!amount || isNaN(amount) || amount <= 0) {
                this.error = 'Please enter a valid BTC amount (greater than 0)';
                return false;
            }
            this.error = null;
            return true;
        },

        handleAmountSubmit() {
            if (this.validateManualAmount()) {
                this.model.due = this.manualAmount;
                this.showAmountInput = false;
            }
        }
    },

    watch: {
        selectedCrypto(newValue) {
            if (newValue) {
                // Reset any search selection when the standard dropdown changes
                this.selectedSearchCoin = null;
                this.createSwap();
            }
        }
    },

    computed: {
        orderAmount() {
            return this.asNumber(this.model.orderAmount);
        },
        due() {
            return this.asNumber(this.model.due);
        },
        qrCodeData() {
            return this.buildPaymentUrl();
        },
        canShowCryptoList() {
            return this.asNumber(this.model.due) > 0;
        },

        // ─── Active crypto display (dropdown or search result) ───────────────

        activeCryptoCode() {
            if (this.selectedSearchCoin) return this.selectedSearchCoin.code == this.selectedSearchCoin.networkCode ? this.selectedSearchCoin.code : this.selectedSearchCoin.code + '-' + this.selectedSearchCoin.networkCode;
            return this.selectedCrypto;
        },

        activeCryptoIcon() {
            if (this.selectedSearchCoin) {
                // Prefer coin-level icon; fall back to network icon
                return this.selectedSearchCoin.coinIcon
                    || this.selectedSearchCoin.networkIcon
                    || this.getCryptoIcon(this.selectedSearchCoin.code);
            }
            return this.getCryptoIcon(this.selectedCrypto);
        }
    }
};

Vue.component('ExolixCheckout', ExolixCheckout);
