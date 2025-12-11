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
<img width="938" height="715" alt="image" src="https://github.com/user-attachments/assets/536922fa-c67c-482e-9b36-c7bd2df23ab3" />

In the "Location" field, you can enter a value that matches the list found in the "Location" filter on the Shopstr homepage. Click "Save".

Click the "Publish on Nostr" button. This creates the new products on Shopstr and updates them if they have been modified on the BTCPay side.
The "Unpublish" button removes the BTCPay products that have been published on Shopstr.
