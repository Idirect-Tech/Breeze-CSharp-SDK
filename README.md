## Breeze Connect SDK

This is a package to integrate streaming stocks for subscribed users & call APIs through which you can fetch live/historical data, automate your trading strategies, and monitor your portfolio in real time.

## Websocket Usage

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Breeze;

//.Net Core 3.1
namespace ConsoleAppTestProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Initialize SDK 
                BreezeConnect breeze = new BreezeConnect("AppKey");
                
                // Generate Session
                breeze.generateSessionAsPerVersion("SecretKey", "API_Session");

                // Connect to WebSocket
                var responseObject = await breeze.wsConnectAsync();
                Console.WriteLine(JsonSerializer.Serialize(responseObject));

                // Callback to receive ticks.
                breeze.ticker((data) =>
                {
                    Console.WriteLine("Ticker Data:" + JsonSerializer.Serialize(data));
                });
                
                // Subscribe stocks feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync((exchangeCode: "NFO", stockCode: "ICIBAN", productType: "options", expiryDate: "25-Aug-2022", strikePrice: "650", right: "Put", getExchangeQuotes: true, getMarketDepth: false))));

                // Subscribe stocks feeds by stock-token
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("4.1!49937")));

                // Subscribe order notification feeds to get order data
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync(true)));

                // UnSubscribe order notification feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync(true)));

                // Unsubscribe stocks feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync((exchangeCode: "NFO", stockCode: "ICIBAN", productType: "options", expiryDate: "25-Aug-2022", strikePrice: "650", right: "Put", getExchangeQuotes: true, getMarketDepth: false))));

                // Unsubscribe stocks feeds by stock-token
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync("4.1!49937")));

                // subscribe to oneclick strategy
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("one_click_fno",true)));

                // unsubscribe to oneclick strategy
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync("one_click_fno",true)));

                // subscribe to ohlc streaming
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("1.1!SENSEX","1second")));

                // unsubscribe to ohlc streaming
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync("1.1!SENSEX","1second")));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
```
---
**NOTE**

Template for stock_token : X.Y!<token><br>

X : exchange code Value of X can be :<br> 1 for BSE <br>4 for NSE <br>13 for NDX <br>6 for MCX<br> 4 for NFO<br>

Y : Market Level data Value of Y can be :<br> 1 for Level 1 data <br>2 for Level 2 data<br>

Token : ISEC stock code Token number can be obtained via get_names() function or downloading master security file via https://api.icicidirect.com/breezeapi/documents/index.html#instruments

Examples for stock_token are "4.1!38071" or "1.1!500780".

exchangeCode must be 'BSE', 'NSE', 'NDX', 'MCX' or 'NFO'.

stockCode should not be an empty string. Examples for stockCode are "NIFTY" or "ICIBAN".

productType can be either 'Futures', 'Options' or an empty string. productType can not be an empty string for exchangeCode 'NDX', 'MCX' and 'NFO'. 

strikeDate can be in DD-MMM-YYYY(Ex.: 01-Jan-2022) or an empty string. strikeDate can not be an empty string for exchangeCode 'NDX', 'MCX' and 'NFO'.

strikePrice can be float-value in string or an empty string. strikePrice can not be an empty string for productType 'Options'.

right can be either 'Put', 'Call' or an empty string. right can not be an empty string for productType 'Options'.

Either getExchangeQuotes must be True or getMarketDepth must be True. Both getExchangeQuotes and getMarketDepth can be True, But both must not be False.

---

## API Usage

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Breeze;

//.Net Core 3.1
namespace ConsoleAppTestProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Initialize SDK 
                BreezeConnect breeze = new BreezeConnect("AppKey");

                // Generate Session
                breeze.generateSession("SecretKey", "API_Session");

                // Following are the complete list of API method:

                // Get Customer details by api-session value.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getCustomerDetail(apiSession: "API_Session")));

                // Get Demat Holding details of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getDematHoldings()));

                // Get Funds details of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getFunds()));

                // Set Funds of your account by transaction-type as "Credit" or "Debit" with amount in numeric string as rupees and segment-type as "Equity" or "FNO".
                Console.WriteLine(JsonSerializer.Serialize(breeze.setFunds(transactionType: "debit", amount: "200", segment: "Equity")));

                // Get Historical Data for specific stock-code by mentioned interval either as "minute", "5minute", "30minutes" or as "day".
                Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalData(interval: "1minute", fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", expiryDate: "2022-08-25T07:00:00.000Z", right: "others", strikePrice: "0")));

                // Add Margin to your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.addMargin(productType: "margin", stockCode: "ICIBAN", exchangeCode: "BSE", settlementId: "2021220", addAmount: "100", marginAmount: "3817.10", openQuantity: "10", coverQuantity: "0", categoryIndexPerStock: "", expiryDate: "", right: "", contractTag: "", strikePrice: "", segmentCode: "")));

                // Get Margin of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getMargin(exchangeCode: "NSE")));

                // Place an order from your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", action: "buy", orderType: "limit", stoploss: "0", quantity: "3200", price: "200", validity: "day", validityDate: "2022-08-22T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-08-25T06:00:00.000Z", right: "others", strikePrice: "0", userRemark: "Test")));

                //place an option plus order

                Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "NIFTY", exchangeCode: "NFO", productType: "optionplus", action: "buy", orderType: "limit", stoploss: "15", quantity: "50", price: "11.25", validity: "day", validityDate: "2022-12-02T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-12-08T06:00:00.000Z", right: "call", strikePrice: "19000", orderTypeFresh = "Limit",
                    orderRateFresh = "20", userRemark: "Test")));

                //place the  future plus order

                Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "NIFTY", exchangeCode: "NFO", productType: "futureplus", action: "buy", orderType: "limit", stoploss: "18720", quantity: "50", price: "18725", validity: "day" , disclosedQuantity: "0", expiryDate: "29-DEC-2022")));

                // Get an order details by exchange-code and order-id from your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOrderDetail(exchangeCode: "NSE", orderId: "20220819N100000001")));

                // Get order list of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOrderList(exchangeCode: "NSE", fromDate: "2022-08-01T10:00:00.000Z", toDate: "2022-08-19T10:00:00.000Z")));

                // Cancel an order from your account whose status are not Executed. 
                Console.WriteLine(JsonSerializer.Serialize(breeze.cancelOrder(exchangeCode: "NSE", orderId: "20220819N100000001")));

                // Modify an order from your account whose status are not Executed. 
                Console.WriteLine(JsonSerializer.Serialize(breeze.modifyOrder(orderId: "202208191100000001", exchangeCode: "NFO", orderType: "limit", stoploss: "0", quantity: "250", price: "290100", validity: "day", disclosedQuantity: "0", validityDate: "2022-08-22T06:00:00.000Z")));

                // Get Portfolio Holdings of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getPortfolioHoldings(exchangeCode: "NFO", fromDate: "2022-08-01T06:00:00.000Z", toDate: "2022-08-19T06:00:00.000Z", stockCode: "", portfolioType: "")));

                // Get Portfolio Positions from your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getPortfolioPositions()));

                // Get quotes of mentioned stock-code
                Console.WriteLine(JsonSerializer.Serialize(breeze.getQuotes(stockCode: "ICIBAN", exchangeCode: "NFO", expiryDate: "2022-08-25T06:00:00.000Z", productType: "futures", right: "others", strikePrice: "0")));

                // Get option-chain of mentioned stock-code for product-type Futures where input of expiry-date is not  compulsory
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode:"ICIBAN",
                    exchangeCode:"NFO",
                    productType:"futures",
                    expiryDate:"2022-08-25T06:00:00.000Z")));

                //Get option-chain of mentioned stock-code for product-type Options where atleast 2 input is required out of expiry-date, right and strike-price
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode:"ICIBAN",
                    exchangeCode:"NFO",
                    productType:"options",
                    expiryDate:"2022-08-25T06:00:00.000Z",
                    right:"call",
                    strikePrice:"16850")));

                // Square off an Equity Margin Order
                Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NSE", productType: "margin", stockCode: "NIFTY", quantity: "10", price: "0", action: "sell", orderType: "market", validity: "day", stoploss: "0", disclosedQuantity: "0", protectionPercentage: "", settlementId: "", coverQuantity: "", openQuantity: "", marginAmount: "", sourceFlag: "", expiryDate: "", right: "", strikePrice: "", validityDate: "", tradePassword: "", aliasName: "")));
                // Note: Please refer getPortfolioPositions() for settlementId and marginAmount

                // Square off an FNO Futures Order
                Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NFO", productType: "futures", stockCode: "ICIBAN", expiryDate: "2022-08-25T06:00:00.000Z", action: "sell", orderType: "market", validity: "day", stoploss: "0", quantity: "50", price: "0", validityDate: "2022-08-12T06:00:00.000Z", tradePassword: "", disclosedQuantity: "0", sourceFlag: "", protectionPercentage: "", settlementId: "", marginAmount: "", openQuantity: "", coverQuantity: "", right: "", strikePrice: "", aliasName: "")));

                // Square off an FNO Options Order
                Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NFO", productType: "options", stockCode: "ICIBAN", expiryDate: "2022-08-25T06:00:00.000Z", right: "Call", strikePrice: "16850", action: "sell", orderType: "market", validity: "day", stoploss: "0", quantity: "50", price: "0", validityDate: "2022-08-12T06:00:00.000Z", tradePassword: "", disclosedQuantity: "0", sourceFlag: "", protectionPercentage: "", settlementId: "", marginAmount: "", openQuantity: "", coverQuantity: "", aliasName: "")));

                // Get trade list of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getTradeList(fromDate: "2022-08-01T06:00:00.000Z", toDate: "2022-08-19T06:00:00.000Z", exchangeCode: "NSE", productType: "", action: "", stockCode: "")));

                // Get trade detail of your account.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getTradeDetail(exchangeCode: "NSE", orderId: "20220819N100000005")));

                // Get Names for NSE codes
                Console.WriteLine(JsonSerializer.Serialize(breeze.getNames(exchange : "NSE", stockCode : "RELIANCE")));
                // Note: Use this method to find ICICI specific stock codes / token

                // preview order sdk
                 Console.WriteLine(JsonSerializer.Serialize(breeze.previewOrder(stockCode:"ICIBAN",exchangeCode:"NSE",productType:"margin",orderType :"limit", price:"907.05", action:"buy", quantity:"1", expiryDate:"", right:"", strikePrice:"", specialFlag:"N", stoploss:"", orderRateFresh:"")));
                
                // limit calculator
                Console.WriteLine(JsonSerializer.Serialize(breeze.limitCalculator(strikePrice:"19200", productType : "optionplus", expiryDate : "06-JUL-2023", underlying:"NIFTY", exchangeCode : "NFO", orderFlow : "Buy", stopLossTrigger : "200.00", optionType : "Call", sourceFlag : "P", limitRate : "", orderReference : "", availableQuantity : "", marketType:"limit", freshOrderLimit:"177.70")));

                //margin calculator
                

        List<Dictionary<string, object>> listOfPositions = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                 { "strike_price", "0" },
                { "quantity", "15" },
                { "right", "others" },
                { "product", "futures" },
                { "action", "buy" },
                { "price", "46230.85" },
                { "expiry_date", "31-Aug-2023" },
                { "stock_code", "CNXBAN" },
                { "cover_order_flow", "N" },
                { "fresh_order_type", "N" },
                { "cover_limit_rate", "0" },
                { "cover_sltp_price", "0" },
                { "fresh_limit_rate", "0" },
                { "open_quantity", "0" }
            },
        new Dictionary<string, object>
        {
            { "strike_price", "37000" },
            { "quantity", "15" },
            { "right", "Call" },
            { "product", "options" },
            { "action", "buy" },
            { "price", "9100" },
            { "expiry_date", "27-Jul-2023" },
            { "stock_code", "CNXBAN" },
            { "cover_order_flow", "N" },
            { "fresh_order_type", "N" },
            { "cover_limit_rate", "0" },
            { "cover_sltp_price", "0" },
            { "fresh_limit_rate", "0" },
            { "open_quantity", "0" }
        }
    }

    Console.WriteLine(JsonSerializer.Serialize(breeze.marginCalculator(listOfPositions : listOfPositions,             exchangeCode:"NFO")));
    
    
    
    

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

        
        
        
        }
        
    }
}
```