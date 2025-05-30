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
            qrOptions: {
                margin: 0,
                type: 'svg',
                color: { dark: '#000', light: '#fff' }
            },
            protocolMap: {
                'XMR': 'monero',
                'ETH': 'ethereum',
                'LTC': 'litecoin',
                'BNB': 'bnb',
                'ADA': 'cardano',
                'DOGE': 'dogecoin',
                'SOL': 'solana',
                'DAI': 'ethereum',
                'USDT-ETH': 'ethereum',
                'USDT-TRX': 'tron',
                'USDT-BSC': 'bnb',
                'USDT-SOL': 'solana',
                'USDT-NEAR': 'near',
                'USDT-MATIC': 'polygon',
                'USDT-TON': 'ton',
                'USDT-AVAXC': 'avalanche',
                'USDC-ETH': 'ethereum',
                'USDC-BSC': 'bnb',
                'USDC-SOL': 'solana',
                'USDC-NEAR': 'near',
                'USDC-MATIC': 'polygon',
                'USDC-AVAXC': 'avalanche'
            }
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
                const formData = new FormData();
                formData.append('CryptoFrom', this.selectedCrypto);
                formData.append('BtcAddress', this.model.address);
                formData.append('BtcAmount', this.model.due);
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
        getCryptoIcon(cryptoCode) {
            return `/Resources/ico/${cryptoCode.substring(0,4)}.webp`;
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
            const protocol = this.selectedCrypto.toLowerCase();
            return `${protocol}:${this.swapData.fromAddress}?amount=${this.formatAmount(this.swapData.fromAmount)}`;
        }
    }
};

Vue.component('ExolixCheckout', ExolixCheckout);
