const B2PCentralCheckout = {
    template: '#b2p-checkout-template',
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
                'MATIC': 'polygon',
                'BNB': 'bnb',
                'BSC': 'bnb',
                'ADA': 'cardano',
                'SOL': 'solana',
                'DAI': 'ethereum',
                'TON': 'ton',
                'DAI-ETH': 'ethereum-dai', 
                'USDT-ETH': 'ethereum-usdt',
                'USDT-TRX': 'tron-usdt',
                'USDT-BSC': 'bnb-usdt',
                'USDT-SOL': 'solana-usdt',
                'USDT-NEAR': 'near-usdt',
                'USDT-MATIC': 'polygon-usdt',
                'USDT-TON': 'ton-usdt',
                'USDT-LIQ': 'liquid-usdt',
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
        }
    },
    methods: {

        getProtocol(cryptoCode) {
            return this.protocolMap[cryptoCode] || cryptoCode.toLowerCase();
        },

        buildPaymentUrl() {
            if (!this.swapData) return null;

            if (!this.selectedCrypto) return null;
            const protocol = this.getProtocol(this.selectedCrypto);
            return `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
        },

        payInWallet() {
            window.location.href = this.buildPaymentUrl();
        },

        asNumber(val) {
            return val && parseFloat(val.toString().replace(/\s/g, ''));
        },

        formatAmount(amount) {
            return parseFloat(amount).toFixed(8);
        },

        getCryptoIcon(cryptoCode) {
            return `/Resources/ico/${cryptoCode.substring(0, 4).replace("-", "")}.webp`;
        },

        getBlockchainIcon(cryptoCode) {
            const tbl = cryptoCode.split("-");
            return tbl.length > 1 ? `/Resources/ico/${tbl[1]}.webp` : '';
        },

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

                let isLightning = sInvoiceBitcoinUrl.includes('lnbc');
                if (isLightning && !sAddress.includes("lnbc")) {
                    const lightningPart = sInvoiceBitcoinUrl.split('lightning')[1];
                    if (lightningPart) {
                        sAddress = lightningPart.replace(':', '').replace('=', '');
                    }
                }

                const formData = new FormData();
                formData.append('Provider', isLightning ? window.b2pData.lightningProvider : window.b2pData.onchainProvider);
                formData.append('QuoteID', '');
                formData.append('FromAmount', 0);
                formData.append('ToAmount', btcAmount);
                formData.append('ToAddress', sAddress);
                formData.append('FromRefundAddress', '');
                formData.append('IsFixed', true);
                formData.append('NotificationEmail', window.b2pData.email);
                formData.append('FromCrypto', this.selectedCrypto);
                formData.append('FromNetwork', '');
                formData.append('ToCrypto', 'BTC');
                formData.append('ToNetwork', isLightning ? "Lightning" : "Bitcoin");
                formData.append('NotificationNpub', '');
                formData.append('ApiKey', window.b2pData.apiKey);
                formData.append('InvoiceId', window.b2pData.invoiceId);

                const response = await fetch(`/plugins/${window.b2pData.storeId}/B2PCentralCheckoutSwap`, {
                    method: 'POST',
                    body: formData
                });
                const result = await response.json();

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                if (!result.success) {
                    try {
                        const errorObj = JSON.parse(result.statusMessage);
                        const fullError = errorObj.error || 'Swap creation failed';

                        const parts = fullError.split(' - ');
                        if (parts.length > 1) {
                            const httpError = parts[0];
                            const content = parts.slice(1).join(' - ');
                            this.error = `${httpError}\n${content}`;
                        } else {
                            this.error = fullError;
                        }
                    } catch (parseError) {
                        this.error = result.statusMessage || 'Swap creation failed';
                    }
                    return;
                    return;
                }
                this.swapData = result;
            } catch (e) {
                this.error = e.message || 'Failed to create swap. Please try again.';
            } finally {
                this.loading = false;
            }
        },

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
        activeCryptoCode() {
            return this.selectedCrypto;
        },
        activeCryptoIcon() {
            return this.getCryptoIcon(this.selectedCrypto);
        },
        activeBlockchainIcon() {
            return this.getBlockchainIcon(this.selectedCrypto);
        }
        activeBlockchain() {
            return this.getBlockchainIcon(this.selectedCrypto) != '';
        }
    }
};

Vue.component('B2PCentralCheckout', B2PCentralCheckout);
