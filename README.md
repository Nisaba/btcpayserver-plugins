# Serilog Server Notifications - Plugin for BTCPay


By default, server logs are saved to disk and can be accessed here:
https://your-btcpay-server/server/logs

This plugin allows you to receive these logs on a Telegram or Slack channel.
For this, it is based on the [Serilog library](https://serilog.net/).

The plugin configuration page is now under BTCPay Server settings.

You can define for each channel a differentiated severity level that will trigger the notifications.
It is advisable to set the "Information" level because the vast majority of BTCPay events are recorded under this priority level.
As such, it would undoubtedly be interesting that in the code of BTCPay, certain events are redefined with a higher priority (for example a Warning level for the de-synchronization of the bitcoin node). This would allow for better granularity.

Note: since this is a plugin, it only activates when the server is started. Therefore, some logs generated during the server boot phase cannot be captured and processed by this plugin.

For each type of message, all you have to do is configure the parameters, test them and then save.
To activate them, check the activation box and define the minimum level ("Information" recommended).


How to set up a Slack channel:

https://api.slack.com/messaging/webhooks


How to set up a Telegram channel:

https://core.telegram.org/bots

https://stackoverflow.com/questions/32423837/telegram-bot-how-to-get-a-group-chat-id

