# Ecwid plugin for BTCPay

This plugin allows you to interface your Ecwid online store with your BTCPay instance, so your customers can pay with bitcoins.

### Steps for setup in BTCPay and Ecwid:

- Install this plugin in your BTCPay instance
- In Ecwid, you need to create a custom app. Follow [this link](https://my.ecwid.com/#develop-apps) for that.
  ![image](https://github.com/user-attachments/assets/2d6d391e-ab16-4e60-94d1-2d035d0156e5)

- In the *Access scopes* area, make sure that *add_payment_method* and *update_orders* are present.
  ![image](https://github.com/user-attachments/assets/412c9c1a-8a4e-45be-927a-915a89082e53)

- In the *Connect new payment method* box, enter "BTCPay" as the Payment title, or another name that seems appropriate to you, such as "Bitcoin payment"
  
![image](https://github.com/user-attachments/assets/22869eb1-91e2-4c95-9b0d-04a757cc81f0)

- Next, you need to go to BTCPay, in the Ecwid plugin settings. Be careful to select the correct BTCPay store. There, you need to copy the URL from the *Ecwid plugin Url* field and paste it into the *Payment URL* field under Ecwid. Now you need to validate and wait for confirmation from Ecwid support.
  
  ![image](https://github.com/user-attachments/assets/d1bd38c4-7a97-4269-b22e-66225e4c7b79)

- Once Ecwid support has confirmed the app creation, go to the *App keys* area and copy the value from the *Client secret* field.
![image](https://github.com/user-attachments/assets/718a1789-abfc-428d-b2a1-b621efe73607)

- Paste this value into the Ecwid plugin settings under BTCPay, in the *Ecwid Client Secret* field. Save.
- Finally, click on *Create Webhook* to automatically create under BTCPay the webhook which will notify payments to your Ecwid store.

Bitcoin payments from your Ecwid store are now live.

![image](https://github.com/user-attachments/assets/1bea2636-aaa6-4199-a172-fa0d80c38d9a)




