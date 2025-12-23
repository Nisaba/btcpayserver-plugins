(function (window) {
    'use strict';

    window.B2PCore = {
        tblOfrs: [],
        tblOfrsOnChain: [],
        tblOfrsLightning: [],
        antiForgeryToken: null,

        init: function () {
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            this.antiForgeryToken = tokenInput ? tokenInput.value : null;
        },

        getPartial: function (reqData, container, bt, controller) {
            $(container).html('<div style="display: flex; justify-content: center;"><img src="/Resources/img/Loading_icon.gif"/></div>');
            $(bt).hide();
            $.ajax({
                url: 'b2pcentral/' + controller,
                type: 'POST',
                contentType: "application/json; charset=utf-8",
                headers: {
                    'RequestVerificationToken': this.antiForgeryToken
                },
                data: JSON.stringify(reqData),
                success: function (result) {
                    $(container).html(result);
                    if (container !== "#container-B2pSwap") {
                        window.B2PCore.tblOfrs = [];
                        $("[id^='sortLink']").each(function () {
                            var $this = $(this);
                            if (!$._data(this, "events") || !$._data(this, "events").click) {
                                $this.on("click", function (event) {
                                    event.preventDefault();
                                    var param1 = $(this).data('param1');
                                    if (window.B2PCore.tblOfrs.length === 0) {
                                        var param2Str = $(this).attr('data-param2');
                                        window.B2PCore.tblOfrs = JSON.parse(param2Str);
                                    }
                                    window.B2PCore.sortHtmlTable(param1);
                                });
                            }
                        });
                    }
                    $(bt).show();
                },
                error: function (xhr, status, error) {
                    $(container).html("Error: " + error + "<br/>" + xhr.responseText);
                    $(bt).show();
                }
            });
        },

        sortHtmlTable: function (currentID) {
            var table, rows, switching, i, xTbl1, xTbl2, shouldSwitch, dir, switchcount = 0;
            table = document.getElementById("offers" + currentID);
            switching = true;
            dir = "asc";

            while (switching) {
                switching = false;
                rows = table.rows;
                for (i = 1; i < (rows.length - 2); i += 2) {
                    shouldSwitch = false;
                    xTbl1 = (i - 1) / 2;
                    xTbl2 = xTbl1 + 1;
                    if (dir === "asc") {
                        if (this.tblOfrs[xTbl1] > this.tblOfrs[xTbl2]) {
                            shouldSwitch = true;
                            break;
                        }
                    } else if (dir === "desc") {
                        if (this.tblOfrs[xTbl1] < this.tblOfrs[xTbl2]) {
                            shouldSwitch = true;
                            break;
                        }
                    }
                }
                if (shouldSwitch) {
                    rows[i].parentNode.insertBefore(rows[i + 2], rows[i]);
                    rows[i].parentNode.insertBefore(rows[i + 3], rows[i + 1]);
                    var v = this.tblOfrs[xTbl1];
                    this.tblOfrs[xTbl1] = this.tblOfrs[xTbl2];
                    this.tblOfrs[xTbl2] = v;
                    switching = true;
                    switchcount++;
                } else {
                    if (switchcount === 0 && dir === "asc") {
                        dir = "desc";
                        switching = true;
                    }
                }
            }
        },

        switchTab: function (switchValue) {
            switch (switchValue) {
                case 1:
                    $("#SectionNav-1").addClass("active").siblings().removeClass("active");
                    $("#tabOnChain").show();
                    $("#tabLightning").hide();
                    $("#tabSwaps").hide();
                    $("#tabListSwaps").hide();
                    this.tblOfrsLightning = this.tblOfrs;
                    this.tblOfrs = this.tblOfrsOnChain;
                    break;
                case 2:
                    $("#SectionNav-2").addClass("active").siblings().removeClass("active");
                    $("#tabOnChain").hide();
                    $("#tabLightning").show();
                    $("#tabSwaps").hide();
                    $("#tabListSwaps").hide();
                    this.tblOfrsOnChain = this.tblOfrs;
                    this.tblOfrs = this.tblOfrsLightning;
                    break;
                case 3:
                    $("#SectionNav-3").addClass("active").siblings().removeClass("active");
                    $("#tabOnChain").hide();
                    $("#tabLightning").hide();
                    $("#tabSwaps").show();
                    $("#tabListSwaps").hide();
                    break;
                case 4:
                    $("#SectionNav-4").addClass("active").siblings().removeClass("active");
                    $("#tabOnChain").hide();
                    $("#tabLightning").hide();
                    $("#tabSwaps").hide();
                    $("#tabListSwaps").show();
                    break;
            }
        }
    };

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

            $('#confirmSwapBtn').prop('disabled', true).text('Processing...'); 
            $('#Provider').val(this.currentSwap.Provider);
            $('#QuoteID').val(this.currentRateType === 'fixed' ? this.currentSwap.FixedQuoteId : this.currentSwap.FloatQuoteId);
            $('#ToCrypto').val(this.rateReq.ToCrypto);
            $('#FromAmount').val(this.currentRateType === 'fixed' ? this.currentSwap.FromFixedAmount : this.currentSwap.FromFloatAmount);
            $('#ToAmount').val(this.currentRateType === 'fixed' ? this.currentSwap.ToFixedAmount : this.currentSwap.ToFloatAmount);
            $('#ToAddress').val(receivingAddress);
            $('#FromRefundAddress').val(refundAddress);
            $('#IsFixed').val(this.currentRateType === 'fixed');
            $('#NotificationEmail').val(email);
            $('#frmSwap').submit();
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
        },

        setSwapCurrency: function () {
            var sCurr = this.getSwapCurrency();
            $('#lblCurrency').text(sCurr);
            if (sCurr === "BTC")
                $('#divFiatSwap').show();
            else
                $('#divFiatSwap').hide();                
        },

        getSwapCurrency: function () {
            const isToSend = $("input[name='rdFromTo']:checked").val() === "ToSend";
            return isToSend ? "BTC" : $('#lstSwapToCrypto').val();
        },

        setFiatValue: function (fiatBalance, btcBalance) {
            if ($('#lblCurrency').text() === "BTC") {
                const swapBtcAmount = parseFloat($('#swapAmount').val());
                console.log(swapBtcAmount)
                const mtFiat = fiatBalance * (swapBtcAmount / btcBalance);
                console.log(mtFiat)
                $('#lblSwapFiat').text(parseInt(mtFiat,10));
             }
        },

        toggleKYCProviders: function () {
            const noKYC = document.querySelector('#chkNoKYC').checked;
            const providerItems = document.querySelectorAll('.swap-provider-item');

            providerItems.forEach(item => {
                const isKYC = item.getAttribute('data-is-kyc') === 'true';
                if (noKYC && isKYC) {
                    item.style.display = 'none';
                    const checkbox = item.querySelector('.swap-provider-checkbox');
                    if (checkbox) {
                        checkbox.checked = false;
                    }
                } else {
                    item.style.display = 'flex';
                }
            });
        },

        onSearchSwap: function (apiKey, fiatCurrency) {
            var selectedProviders = [];

            document.querySelectorAll('.swap-provider-checkbox').forEach(checkbox => {
                const parentItem = checkbox.closest('.swap-provider-item');
                if (checkbox.checked && parentItem.style.display !== 'none') {
                    selectedProviders.push(parseInt(checkbox.value));
                }
            });

            if (selectedProviders.length === 0) {
                alert('Please select at least one swap provider');
                return;
            }

            const swapAmount = parseFloat($('#swapAmount').val());
            if (!swapAmount || swapAmount <= 0) {
                alert('Please enter a correct amount');
                return;
            }

            const isToSend = $("input[name='rdFromTo']:checked").val() === "ToSend";
            const data = {
                ToCrypto: $('#lstSwapToCrypto').val(),
                FromAmount: isToSend ? swapAmount : 0,
                ToAmount: isToSend ? 0 : swapAmount,
                FiatCurrency: fiatCurrency,
                Providers: selectedProviders,
                ApiKey: apiKey
            };

            window.B2PCore.getPartial(data, "#container-B2pSwap", "#btSwapSearch", "GetPartialB2PSwapResult");
        }
    };

    $(document).ready(function () {
        window.B2PCore.init();
    });

})(window);
