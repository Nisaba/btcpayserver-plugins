# MtPelerin plugin for BTCPay Server
This plugin allows you to forward received BTC funds (onchain or Lightning) to <a href="https://www.mtpelerin.com/" target="_blank">MtPelerin</a>, a Swiss offramp provider. Get funds in fiat (EUR, CHF or other currencies) or make a swap to the crypto you want.
The plugin allows for an integrated connection to the Mt Pelerin widget, so you can easily send your bitcoins for sale or exchange

## Settings

The plugin settings must be configured for each BTCPay store. It is accessible to users with the "CanModifyStoreSettings" permission.
- Phone : Pre-fills your user's mobile phone number during the login step. Enter it as numbers that begin with the country code. Example : phone=41791234567 for the phone number +41 79 123 45 67
- Use Bridge Wallet : you can choose this option to connect to Mt Pelerin if you have already it in your mobile phone. So you will be able to scan q QR Code during the process to make easy the identification. If this parameter is set, it ovverrides the phone parameter
- Display Language : To choose the language used in the Mt Pelerin widget
![image](https://github.com/user-attachments/assets/dc64e1bb-89e5-4fcb-9235-2c56c96fe96f)

## Operations

This section is accessible to users who have permissions "CanCreateNonApprovedPullPayments" and "CanManagePayouts".<br/>
First, select the network (onchain or Lightning), then set the amount (percent) to send. It is required to leave enough funds to pay transaction fees and not to leave dirty UTXOs.<br/>
Then you can call the Mt Pelerin widget. It will be opened in a popup window.<br/>
From there, you can follow the Mt Pelerin process to sell or exchange your bitcoins. The plugin has passed as much information as possible to the widget to make the experience as seamless as possible.

![image](https://github.com/user-attachments/assets/08b6fe11-72ed-4ae8-aad4-a6648c243d59)

Note: For on-chain transactions, Mt Pelerin requires that the sending address be certified to facilitate the process. To do this, the plugin uses the address with the highest UTXO value in your wallet to sign a message. This is done automatically, but you'll need to ensure that this UTXO will be used in the sending transaction.<br/>
If this fails, you can manually perform this process via the widget.

## Onchain Payouts

The plugin detects automatically when you have finished the process in the widget.
So, a BTCPay payout is automatically created.
![image](https://github.com/user-attachments/assets/190e2229-44a1-4d67-b288-c8b9976a89d6)

All you have to do now is go to the BTCPay payouts page to send the payment.
![image](https://github.com/user-attachments/assets/6eb429a1-7940-4b63-b974-1116014844df)

Note: to avoid any issue, please check that there is no previous pending Mt Pelerin payout in BTCPay before placing another order. Each Mt Pelerin payout must be processed before submitting another.

## Lightning Payouts

The plugin detects automatically when you have finished the process in the widget.
So, a BTCPay Lightning payout is automatically created.

If you have this error, so your store settings must be updated :
![image](https://github.com/user-attachments/assets/fb5634e8-05a9-49e3-81fe-d9b5178c4885)

Go to the store settings, and set the "Minimum acceptable expiration time for BOLT11 for refunds to 0
![image](https://github.com/user-attachments/assets/43087738-a092-4fea-97a6-ca2b64526a4d)

So, the payout can be created by BTCPay. You can pay the Lightning invoice with BTCPay or with your wallet and mark the payout as paid.
