const SatoraCheckout = {
    template: '#satora-checkout-template',
    props: {
        model: {
            type: Object,
            required: true
        }
    },
    data() {
        return {
            availableCryptos: window.satoraData ? window.satoraData.availableCryptos : {},
            selectedStablecoin: "",
            selectedBlockchain: ""
        };
    },
    watch: {
        selectedStablecoin(newVal) {
            this.selectedBlockchain = "";

            if (newVal && this.availableCryptos[newVal]) {
                const blockchains = this.availableCryptos[newVal];
                if (blockchains.length === 1) {
                    this.selectedBlockchain = blockchains[0];
                }
            }
        }
    }
};

Vue.component('SatoraCheckout', SatoraCheckout);