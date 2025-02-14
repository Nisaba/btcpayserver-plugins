# Ecwid plugin for BTCPay Server

This plugin allows you to interface your Ecwid online store with your BTCPay Server instance, so your customers can pay with bitcoins.

## Steps to setup BTCPay as payment gateway in Ecwid:

1. Install this plugin "Ecwid plugin" on your BTCPay instance, you can find it under "Manage Plugins" (this only works on your own BTCPay Server, if you are on a 3rd party host, they need to install the plugin for the whole server)
   - After you installed the plugin select the right store and click on "Ecwid" in the left sidebar
   - It will show you a "Ecwid Payment URL" at the top, copy the shown URL, we will need it in a few steps below.
     :::IMAGE_ECWID_PLUGIN
 
2. In your Ecwid store, you need to create a custom app. Follow [this link](https://my.ecwid.com/#develop-apps).
  - Click on "Create" next to "Create one more app"
    ![image](https://github.com/user-attachments/assets/2d6d391e-ab16-4e60-94d1-2d035d0156e5)
  - Next you need to contact the Ecwid support via the support form at the bottom of that app page.
    :::IMAGE_SUPPORT_BOTTOM
  - And ask them to change the following settings:
    - "Access scope": we need the scope `add_payment_method` 
    - "Payment title": `Pay with Bitcoin / Lightning Network` (this will be shown to customers as payment option, can be changed to your preference)
    - "Payment URL": here enter the URL from the Plugin on BTCPay, which you copied at step 1. above. E.g. `https://BTCPAY.YOURDOMAIN.COM/plugins/STORE_ID/EcwidPayment`

After you sent the form you need to wait for Ecwid support to confirm the changes done.

3. Once Ecwid support has confirmed the app settings updates, go back to the app (App -> My apps click on "Manage app")
   - Scroll down to *App keys* section and click on the "Show client secret" link
   - Copy the value from the *Client secret* field
   ![image](https://github.com/user-attachments/assets/718a1789-abfc-428d-b2a1-b621efe73607)

4. Back on your BTCPay stores Ecwid plugin
- Paste the value of that "Client secret" into the field "Ecwid Client Secret"
- Click on "Save" 
- Finally, click on *Create Webhook* to automatically create under BTCPay the webhook which will notify payments to your Ecwid store.

Congratulations, Bitcoin payments are now live on your Ecwid store.

![image](https://github.com/user-attachments/assets/1bea2636-aaa6-4199-a172-fa0d80c38d9a)




