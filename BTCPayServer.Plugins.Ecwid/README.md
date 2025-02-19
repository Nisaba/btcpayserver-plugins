# Ecwid plugin for BTCPay Server

This plugin allows you to interface your Ecwid online store with your BTCPay Server instance, so your customers can pay with bitcoins.

## Steps to setup BTCPay as payment gateway in Ecwid:

1. Install this plugin "Ecwid plugin" on your BTCPay instance, you can find it under "Manage Plugins" (this only works on your own BTCPay Server, if you are on a 3rd party host, they need to install the plugin for the whole server)
   - After you installed the plugin select the right store and click on "Ecwid" in the left sidebar
     ![Select Ecwid plugin in sidebar](./docs/img/plugin-selection.png)
   - It will show you a "Ecwid Payment URL" at the top, copy the shown URL, we will need it in a few steps below.
     ![Ecwid plugin settings page](./docs/img/plugin-settings-page.png)
----- 
2. In your Ecwid store, you need to create a custom app. Follow [this link](https://my.ecwid.com/#develop-apps).
  - Click on "Create" next to "Create one more app"
    ![Create one more app](./docs/img/ecwid-app-create-app.png)
  - Next you need to contact the Ecwid support via the support form at the bottom of that app page.
    ![Contact ecwid support](./docs/img/ecwid-app-contact-support.png)
  - And ask them to change the following settings:
    - "Access scope": we need the scope `add_payment_method` 
    - "Payment title": `Pay with Bitcoin / Lightning Network` (this will be shown to customers as payment option, can be changed to your preference)
    - "Payment URL": here enter the URL from the Plugin on BTCPay, which you copied at step 1. above. E.g. `https://BTCPAY.YOURDOMAIN.COM/plugins/STORE_ID/EcwidPayment`

After you sent the form you need to wait for Ecwid support to confirm the changes done.
-----
3. Once Ecwid support has confirmed the app settings updates, go back to the app (App -> My apps click on "Manage app")
   - Scroll down to *App keys* section and click on the "Show client secret" link
   - Copy the value from the *Client secret* field
   ![ecwid-app-app-keys.png](./docs/img/ecwid-app-app-keys.png)
-----
4. Back on your BTCPay stores Ecwid plugin
- Paste the value of that "Client secret" into the field "Ecwid Client Secret"
- Click on "Save" 
- Finally, click on *Create Webhook* to automatically create under BTCPay the webhook which will notify payments to your Ecwid store.

Congratulations! Bitcoin payments are now live on your Ecwid store.

You should now see the payment option in your Ecwid store checkout page. You can check by clicking on "Payment" on the left sidebar.
![Payment Options](./docs/img/ecwid-payment-options.png)




