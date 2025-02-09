# Ecwid plugin for BTCPay

This plugin allows you to interface your Ecwid online store with your BTCPay instance, so your customers can pay with bitcoins.

### Steps for setup in BTCPay and Ecwid:

- Install this plugin in your BTCPay instance
- In Ecwid, you need to create a custom app. Follow [this link](https://my.ecwid.com/#develop-apps) for that.
- In the *Access scopes* area, make sure that *add_payment_method* is present.
- In the *Connect new payment method* box, enter "BTCPay" as the payment method, or another name that seems appropriate to you, such as "Bitcoin payment"
- Next, you need to go to BTCPay, in the Ecwid plugin settings. Be careful to select the correct BTCPay store. There, you need to copy the URL from the *Ecwid plugin Url* field and paste it into the *Payment URL* field under Ecwid. Now you need to validate and wait for confirmation from Ecwid support.
- Once Ecwid support has confirmed the app creation, go to the *App keys* area and copy the value from the *Client secret* field.
- Paste this value into the Ecwid plugin settings under BTCPay, in the *Ecwid Client Secret* field. Save.
- Finally, click on *Create Webhook* to automatically create under BTCPay the webhook which will notify payments to your Ecwid store.

Bitcoin payments from your Ecwid store are now live.



