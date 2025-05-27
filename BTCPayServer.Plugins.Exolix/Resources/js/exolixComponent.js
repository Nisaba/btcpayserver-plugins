Vue.component('ExolixCheckout', {
    template: '#exolix-checkout-template',
    props: {
        srvModel: {
            type: Object,
            required: true
        }
    },
    data() {
        return {
            isLoaded: true
        }
    }
});
        console.log('Payment Model:', this.srvModel);
