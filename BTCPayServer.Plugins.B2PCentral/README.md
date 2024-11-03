# BTCPayServer.Plugins.B2PCentral

If you want to sell bitcoins you earn with your store activity, this plugin allows you to get P2P bitcoin buy offers from <a href="https://www.b2p-central.com" target="b2p">B2P Central</a>, according to your onchain and lightning wallets balances and the store's default currency.
Offers are provided by HodlHodl, Paxful, NoOnes, LocalCoinSwap and Peach for onchain bitcoins. And by LNp2pBot and RoboSats for Lightning bitcoins.

First, you need to get a B2P Central API key <a href="https://getapi.b2p-central.com" target="b2pApi">here</a>.
The configuration section is available for users having CanModifyStoreSettings rights. Click on Save button, then you have a Test button to check it's OK.

![image](https://github.com/user-attachments/assets/d7a41bde-0e5e-49a8-b449-28069f6055f5)

Then you have 2 tabs for onchain and Lightning, if the walets are configured in the current store.
These tabs are available for users having CanViewStoreSettings rights.

Each wallet shows the current balance in BTC and in store fiat currency.

So you have to select providers you want and choose the percent of the balance you want to sell. Then click on Search.
After a few seconds, result is displayed. You can sort the offers by amount.

Click on the offer you want (column id) to open it in a new browser tab and make the deal if you want.

![image](https://github.com/user-attachments/assets/e44b05fb-b06e-4d3d-b895-5b02b7aea501)

