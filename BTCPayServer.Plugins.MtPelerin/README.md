# MtPelerin plugin for BTCPay Server

This plugin allows you to forward received BTC funds (on-chain or Lightning) to [MtPelerin](https://www.mtpelerin.com/).  
Get funds in fiat (EUR, CHF or other currencies) or swap them into the crypto of your choice.  
The plugin provides an integrated connection to the Mt Pelerin widget, so you can easily send your bitcoins for sale or exchange.

## Settings

The plugin settings must be configured for each BTCPay store. They are accessible to users with the `CanModifyStoreSettings` permission.

- **Phone**: Pre-fills your user's mobile phone number during the login step. Enter it as digits starting with the country code.  
  Example: `phone=41791234567` for the phone number +41 79 123 45 67.
- **Use Bridge Wallet**: You can select this option if you already have the Bridge Wallet app installed on your phone.  
  You will then be able to scan a QR Code during the process to make identification easier.  
  If this option is enabled, it overrides the `phone` parameter.
- **Display Language**: Select the language used in the Mt Pelerin widget.  
  ![image](https://github.com/user-attachments/assets/dc64e1bb-89e5-4fcb-9235-2c56c96fe96f)

## Operations

This section is accessible to users with the permissions `CanCreateNonApprovedPullPayments` and `CanManagePayouts`.

1. Select the network (on-chain or Lightning).
2. Set the amount (as a percentage) to send.  
   Make sure to leave enough funds to cover transaction fees and avoid leaving small unusable UTXOs.
3. Launch the Mt Pelerin widget — it will open in a popup window.
4. Follow the Mt Pelerin steps to sell or exchange your bitcoins.  
   The plugin passes along as much info as possible to simplify the process.

![image](https://github.com/user-attachments/assets/08b6fe11-72ed-4ae8-aad4-a6648c243d59)

**Note**: For on-chain transactions, Mt Pelerin requires the sending address to be verified — either by signing a message or completing a Satoshi test.

## On-chain Payouts

The plugin automatically detects when you finish the process in the widget.  
A BTCPay payout is then automatically created.

![image](https://github.com/user-attachments/assets/190e2229-44a1-4d67-b288-c8b9976a89d6)

Go to the BTCPay payouts page to send the payment.

![image](https://github.com/user-attachments/assets/6eb429a1-7940-4b63-b974-1116014844df)

**Note**: To avoid issues, make sure there is no pending Mt Pelerin payout in BTCPay before creating a new one.  
Each Mt Pelerin payout must be completed before starting another.

## Lightning Payouts

The plugin also automatically detects when the process in the widget is completed.  
A Lightning payout is then automatically created in BTCPay.

If you encounter this error:

![image](https://github.com/user-attachments/assets/fb5634e8-05a9-49e3-81fe-d9b5178c4885)

Go to your store settings and set:

```
Minimum acceptable expiration time for BOLT11 for refunds = 0
```

![image](https://github.com/user-attachments/assets/43087738-a092-4fea-97a6-ca2b64526a4d)

This allows BTCPay to create the payout.  
You can then pay the Lightning invoice with BTCPay or with your wallet, and mark the payout as paid.
