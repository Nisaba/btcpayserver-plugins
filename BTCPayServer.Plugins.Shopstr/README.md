# Shopstr plugin for BTCPay Server
This plugin allows you to publish the products you've defined in your BTCPay point of sale to Shopstr and ensures their synchronization.
It requires the [Nostr plugin](https://plugin-builder.btcpayserver.org/public/plugins/nip5) to initialize the Nostr store settings.

## Configuration
- Start by configuring your settings in the Nostr plugin.
You need to define both your public and private Nostr keys to publish your products.
You also need to verify that the list of retailers is correctly retrieved from your profile. If it isn't, you can populate this list manually. It's essential that the retailers defined here are the same as those found in your Shopstr profile.
<img width="752" height="513" alt="image" src="https://github.com/user-attachments/assets/0e14703e-b217-4e60-b7cd-7348dc873659" />

- You also need an active point of sale in your current store, with products listed online. "Keypad" type point of sale are not supported.

## Using Shopstr plugin
You can now publish your BTCPay point-of-sale items on Nostr.
<img width="1123" height="665" alt="image" src="https://github.com/user-attachments/assets/825f5157-a03e-4d82-80ca-1578af8b63cc" />

Expand the “Default Settings” option. You can set here Shopstr options. Once published, you will find these options in your listings on Shopstr. 

<img width="1135" height="396" alt="image" src="https://github.com/user-attachments/assets/a0e10f70-b04f-4aa2-9833-deb8953af611" />

Note: quantities and categories are managed directly in the Point of Sale app. These values are also synchronized during Nostr publishing. For categories, please enter a value that match with Shopstr default categories.

Click the "Publish on Nostr" button. This creates the new products on Shopstr and updates them if they have been modified on the BTCPay side.
The "Unpublish" button removes the BTCPay products that have been published on Shopstr.

## Version History
- **1.0.1** - Initial release
- **1.1.0** - UI improvements, better relay management, better synchronization with Shopstr listings attributes
- **1.1.1** - Small fixes
 

