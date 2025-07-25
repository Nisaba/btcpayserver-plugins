# Exolix plugin for BTCPay Server
This plugin allows merchants to accept altcoins as a payment method. It must be installed via the BTCPay Server plugin plugin admin page in your instance.

The principle is as follows: During checkout, the buyer pays in altcoins to a service provider who performs a bitcoin swap to the address generated by BTCPay for the checkout.
This allows BTCPay to transparently consider the invoice as paid.

Similar plugins for BTCPay already exist. However, this one uses Exolix as the swap service provider. Furthermore, there are no intrusive IFRAMEs at checkout, which can disrupt the customer and hinder their purchasing process. Instead, payment with altcoins respects the BTCPay user experience for a smoother checkout process.

## Settings
Exolix generates the bitcoin payment as soon as the altcoin transaction is confirmed, according to the terms of the blockchain used. Therefore, in the store settings, it is advisable to specify a sufficient time limit for invoice expiration.

Please also note that Exolix requires a minimum amount for a swap to work, around 10 USD.
If this amount is not reached, the swap cannot be completed.

The plugin settings must be configured for each BTCPay store. It is accessible to users with the "CanModifyStoreSettings" permission.

You must activate the plugin and select the desired cryptocurrencies from the list during checkout.
For the plugin to be active, it must be activated and at least one altcoin must be selected.

![image](https://github.com/user-attachments/assets/e4b67eff-83c0-405d-aca6-58138b337b21)

## Checkout
During checkout, the customer must click on "Altcoins" and then choose their crypto from the drop-down list. They can then complete their transaction according to the information provided, using the appropriate QR code.

![image](https://github.com/user-attachments/assets/cdb528cd-6706-4f3d-9372-558a1df1c028)

## Swap List

Swaps are displayed to users with the "CanViewInvoices" permission. By clicking on the links, you can view the status of the Exolix swap and the BTCPay invoice.

![image](https://github.com/user-attachments/assets/4605fdfa-f696-4851-9896-d949c271b35f)

## Version History
- **1.0.1** - Initial release
- **1.0.2** - Fixed a bug with BSC in checkout, add TRX and POL/MATIC cryptos, add informations about support in checkout
- **1.0.3** - Improve status message in case of Exolix error, ask to enter BTC amount if BTCPay invoice amount is 0
- **1.0.4** - Better QR Code generation, now readable by more wallets


