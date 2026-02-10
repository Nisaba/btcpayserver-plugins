# BTCPayServer.Plugins.B2PCentral

If you want to sell bitcoins you earn with your store activity, this plugin allows you to get P2P bitcoin buy offers from <a href="https://www.b2p-central.com" target="b2p">B2P Central</a>, according to your onchain and lightning wallets balances and the store's default currency.
You can also perform onchain swaps to a variety of altcoins and stablecoins supported by B2P Central.<br />

## Configuration

First, you need to get a B2P Central API key <a href="https://getapi.b2p-central.com" target="b2pApi">here</a>.
The configuration section is available for users having CanModifyStoreSettings rights. Click on Save button, then you have a Test button to check it's OK.

<img width="960" height="209" alt="image" src="https://github.com/user-attachments/assets/1cda655e-a089-42f7-8de8-db4235d9e073" />

## P2P Bitcoin Sell Offers

Offers are provided by HodlHodl, NoOnes, LocalCoinSwap and Peach for onchain bitcoins. And by LNp2pBot, Mostro and RoboSats for Lightning bitcoins.
Then you have 2 tabs for onchain and Lightning, if the walets are configured in the current store.
These tabs are available for users having CanViewStoreSettings rights.

Each wallet shows the current balance in BTC and in store fiat currency.

So you have to select providers you want and choose the percent of the balance you want to sell. Then click on Search.
After a few seconds, result is displayed. You can sort the offers by price.

Click on the offer you want (column id) to open it in a new browser tab and make the deal if you want.

<img width="1206" height="717" alt="image" src="https://github.com/user-attachments/assets/a6b0bc0e-0023-480f-9400-10a6a7cfe7da" />


## Swaps (On chain)

The **Swaps (On chain)** tab allows you to perform atomic swaps to convert your on-chain BTC to fiat currency through supported swap providers, using B2P Central as an intermediary.
You can then find the best swaps rates from various providers and execute the swap directly from your BTCPay Server interface.

### How to use:

1. **Select the swap direction**:
   - **To Send**: Enter the amount of BTC you want to swap
   - **To Receive**: Enter the amount of the target cryptocurrency you want to receive

2. **Enter the amount**: Specify the BTC amount to swap (default is 95% of your balance)

3. **Select the target cryptocurrency**: Choose from available options grouped by category:
   - **Altcoins**: BTC (other networks), ETH, LTC, XMR, DOGE, TRX, BCH, etc.
   - **USD Stablecoins**: USDT, USDC (on various networks)
   - **EUR Stablecoins**: EURS, DEURO, etc.

4. **Select swap providers**: Check the providers you want to query for rates
   - Use the **"No KYC Providers only"** toggle to filter out providers requiring identity verification

<img width="1175" height="594" alt="image" src="https://github.com/user-attachments/assets/98f50646-a760-4c6d-b243-05565b8e8197" />


5. **Click Search**: The plugin will fetch available swap rates from selected providers

<img width="1269" height="766" alt="image" src="https://github.com/user-attachments/assets/d1ff9a60-76e3-4c2c-bcc1-1bceb8307eea" />


6. **Review and confirm**: Select the best offer and confirm the swap. For each swap offer, you have the amount to send, the amount you will receive in alt (for fixed and float offers) and the values in your fiat currency.
   - A payout will be automatically created in BTCPay Server
   - If you have an automatic payout processor configured, the payment will be sent automatically
   - Otherwise, go to the Payouts section to validate the payout manually
<img width="535" height="695" alt="image" src="https://github.com/user-attachments/assets/5f2f488f-3961-47cf-a1df-174dcf4ce39d" />

### Important notes:
- Please check that there is no previous pending payout in BTCPay before placing a swap
- The swap creates a Pull Payment that needs to be approved (unless auto-approval is configured)

## Swaps list

The **Swaps list** tab displays the history of all swaps performed through the plugin for the current store.

This tab is available for users having **CanCreateNonApprovedPullPayments** rights.

### Information displayed:

| Column | Description |
|--------|-------------|
| **Swap ID** | Unique identifier from the swap provider (clickable link to provider's page) |
| **Provider** | The swap provider used for this transaction |
| **Altcoin** | Target cryptocurrency and network |
| **Created At** | Date and time when the swap was initiated |
| **Amount (BTC)** | Amount of BTC sent |
| **Amount (Alt)** | Amount of altcoin/stablecoin received |
| **Pull Payment** | Link to the associated BTCPay Pull Payment |
| **Status** | Link to view the swap status JSON from the provider |

### Actions:
- Click on **Swap ID** to view the swap details on the provider's website
- Click on **Pull Payment ID** to view the associated payout in BTCPay Server
- Click on **View JSON** to see the current swap status from the provider using B2P Central link

---



What's new in v 1.2.1 :
- Bump to BTCPay v2
- Add NoOnes as onchain provider

What's new in v 1.2.2 :
- Bump to BTCPay v2.0.3

What's new in v 1.2.3 :
- Add Bisq v1 as onchain provider

What's new in v 1.2.4 :
- Fix a potential bug when offchain wallet is not configured

What's new in v 1.2.5 :
- Display correct Lightning balance for stores using Boltz plugin

What's new in v 1.2.6 :
- Small fixes

What's new in v 1.3.1 :
- Add Mostro as Lightning provider

What's new in v 1.3.2 :
- Remove Paxful as onchain provider (not working anymore)
- Minor improvements

What's new in v 2.0.1 :
- Display and minor improvements
- Add onchain Swaps capabilities

What's new in v 2.0.2 :
- Fix issue in startup on new  plugin installation

What's new in v 2.1.0 :
- CCE.Cash and SageSwap added as swap providers
- Bitcoin Lightning can now be a swap destination



