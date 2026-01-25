# BTCPay Server Telegram Bot Plugin

This plugin allows you to link a Telegram Bot to a BTCPay Server Point of Sale (PoS), enabling users to browse products and create invoices directly within Telegram.

## Setup Instructions

### 1. Create a Telegram Bot

To use this plugin, you first need to create a bot on Telegram and get its API token.

1.  Open Telegram and search for **@BotFather**.
2.  Start a chat with BotFather and send the command `/newbot`.
3.  Follow the instructions to choose a name and a username for your bot.
4.  Once created, BotFather will provide you with an **API Token**. Keep this token secure.

### 2. Customize Your Bot (Optional)

You can personalize your bot's appearance to match your store using BotFather:

*   **Set Description**: Send `/setdescription` to BotFather, select your bot, and enter a description. This text is shown to users when they first open the bot.
*   **Set User Picture**: Send `/setuserpic` to BotFather, select your bot, and upload an image (e.g., your store logo).

### 3. Configure the Plugin in BTCPay Server

1.  Log in to your BTCPay Server instance.
2.  Navigate to the **Telegram Shopping Bot** plugin settings page.
3.  Choose in the list the **Point of Sale** in the current store you want to link to this bot.
4.  Enter the **Telegram Bot Token** you received from BotFather.
5.  You can then activate or deactivate the bot as needed.
5.  Save the configuration.

Your bot should now be active. Users can start the bot to view products from your linked Point of Sale.
When BTCPay restarts, all the active bots will automatically reconnect.

### Localhost Limitations
If you are running BTCPay Server locally (e.g., on `localhost`):
*   **Product Images**: Telegram servers cannot access images hosted on your localhost. Product images will not appear in the bot interface.
*   **Checkout/Payment**: The checkout process and callbacks may not work correctly because Telegram cannot reach your local instance without a public URL (e.g., via a tunnel or production deployment).

## Invoices listing
In the plugin page, you can see the list of invoices created through the Telegram Bot.
