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
                'USDC-AVAXC': 'avalanche-usdc'
            },
            manualAmount: '',
            showAmountInput: false
        }
    },
    methods: {
        getProtocol(cryptoCode) {
            return this.protocolMap[cryptoCode] || cryptoCode.toLowerCase();
        },

        payInWallet() {
            if (!this.swapData || !this.selectedCrypto) return;

            const protocol = this.getProtocol(this.selectedCrypto);
            const paymentUrl = `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
            window.open(paymentUrl, '_blank', 'noopener,noreferrer');
        },
        asNumber(val) {
            return val && parseFloat(val.toString().replace(/\s/g, ''));
        },
        formatAmount(amount) {
            return parseFloat(amount).toFixed(8);
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
                const formData = new FormData();
                formData.append('CryptoFrom', this.selectedCrypto);
                formData.append('BtcAddress', this.model.address);
                formData.append('BtcAmount', btcAmount);
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
        async checkStatus() {
            if (!this.swapData || !this.swapData.swapId) return;

            this.checkingStatus = true;
            try {
                const response = await fetch(`https://exolix.com/api/v2/transactions/${this.swapData.swapId}`, {
                    method: 'GET',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error('Failed to fetch status');
                }

                const result = await response.json();
                this.swapStatus = result.status || 'Unknown';

            } catch (error) {
                console.error('Status check failed:', error);
                this.swapStatus = 'Failed to check status';
            } finally {
                this.checkingStatus = false;
            }
        },
        getCryptoIcon(cryptoCode) {
            return `/Resources/ico/${cryptoCode.substring(0,4)}.webp`;
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
            if (!this.swapData || !this.selectedCrypto) return null;
            const protocol = this.getProtocol(this.selectedCrypto);
            return `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
        },
        canShowCryptoList() {
            return this.asNumber(this.model.due) > 0;
        }
    }
};

Vue.component('ExolixCheckout', ExolixCheckout);
