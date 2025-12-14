(function (window) {
    'use strict';

    window.B2PSwap = {
        swaps: [],
        rateReq: null,
        currentSwap: null,
        currentRateType: 'fixed',

        init: function (swapsData, rateReqData) {
            this.swaps = swapsData;
            this.rateReq = rateReqData;
            this.attachHandlers();
        },

        attachHandlers: function () {
            const self = this;

            $(document).off('click', '.swap-btn').on('click', '.swap-btn', function () {
                const providerId = parseInt($(this).data('provider-id'));
                const providerDescription = $(this).data('provider-description');
                self.onSwap(providerId, providerDescription);
            });

            $('input[name="rateType"]').off('change').on('change', function () {
                self.currentRateType = this.value;
                self.updateSwapAmounts();
            });

            $('#confirmSwapBtn').off('click').on('click', function () {
                self.confirmSwap();
            });

            $('[data-bs-toggle="tooltip"]').each(function () {
                new bootstrap.Tooltip(this);
            });
        },

        onSwap: function (providerId, providerDescription) {
            this.currentSwap = this.swaps.find(s => s.Provider === providerId);
            if (!this.currentSwap) {
                return;
            }

            $('#swapModalLabel').text(`Create a Swap with ${providerDescription}`);
            $('#modalProviderLogo').attr('src', `/Resources/img/${providerDescription}.webp`);
            $('#modalProviderLogo').attr('alt', providerDescription);

            const toCrypto = this.rateReq.ToCrypto;
            $('#receivingAddress').attr('placeholder', `${toCrypto} (${this.getFullCryptoName(toCrypto)})`);

            this.updateSwapAmounts();

            if (this.currentSwap.ValidUntil) {
                $('#modalValidUntil').text(`Valid until: ${this.currentSwap.ValidUntil}`);
                $('#modalValidUntil').css('display', 'block');
            } else {
                $('#modalValidUntil').css('display', 'none');
            }

            const modal = new bootstrap.Modal($('#swapModal')[0]);
            modal.show();
        },

        updateSwapAmounts: function () {
            const swap = this.currentSwap;
            if (!swap) return;

            const isFixed = this.currentRateType === 'fixed';
            const fiatCurrency = this.rateReq.FiatCurrency;
            const toCrypto = this.rateReq.ToCrypto;

            if (isFixed) {
                $('#modalFromAmount').text(`${swap.FromFixedAmount.toFixed(8)} BTC`);
                $('#modalFromFiat').text(`${swap.FromFiatFixedAmount.toFixed(2)} ${fiatCurrency}`);
                $('#modalToAmount').text(`${swap.ToFixedAmount.toFixed(8)} ${toCrypto}`);
                $('#modalToFiat').text(`${swap.ToFiatFixedAmount.toFixed(2)} ${fiatCurrency}`);
            } else {
                $('#modalFromAmount').text(`${swap.FromFloatAmount.toFixed(8)} BTC`);
                $('#modalFromFiat').text(`${swap.FromFiatFloatAmount.toFixed(2)} ${fiatCurrency}`);
                $('#modalToAmount').text(`${swap.ToFloatAmount.toFixed(8)} ${toCrypto}`);
                $('#modalToFiat').text(`${swap.ToFiatFloatAmount.toFixed(2)} ${fiatCurrency}`);
            }
        },

        confirmSwap: function () {
            const receivingAddress = $('#receivingAddress').val();
            const refundAddress = $('#refundAddress').val();
            const email = $('#emailAddress').val();

            let isValid = true;

            if (!receivingAddress) {
                $('#receivingAddressError').text('Receiving address is required.');
                isValid = false;
            } else {
                $('#receivingAddressError').text('');
            }

            if (!email) {
                $('#emailAddressError').text('Email is required.');
                isValid = false;
            } else if (!this.isValidEmail(email)) {
                $('#emailAddressError').text('Please enter a valid email address.');
                isValid = false;
            } else {
                $('#emailAddressError').text('');
            }
            if (!isValid) return;

            $('#Provider').val(this.currentSwap.Provider);
            $('#QuoteID').val(this.currentSwap.QuoteID);
            $('FromAmount').val(this.currentRateType === 'fixed' ? this.currentSwap.FromFixedAmount : this.currentSwap.FromFloatAmount);
            $('ToAmount').val(this.currentRateType === 'fixed' ? this.currentSwap.ToFixedAmount : this.currentSwap.ToFloatAmount);
            $('ToAddress').val(receivingAddress);
            $('FromRefundAddress').val(refundAddress);
            $('IsFixed').val(this.currentRateType === 'fixed');
            $('NotificationEmail').val(email);
            $('#frmSwap').submit();
            //bootstrap.Modal.getInstance($('#swapModal')[0]).hide();
        },

        isValidEmail: function (email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        },

        getFullCryptoName: function (crypto) {
            const names = {
                'XMR': 'Monero',
                'BTC': 'Bitcoin',
                'LTC': 'Litecoin',
                'ETH': 'Ethereum',
                'BCH': 'Bitcoin Cash'
            };
            return names[crypto] || crypto;
        }
    };
})(window);
