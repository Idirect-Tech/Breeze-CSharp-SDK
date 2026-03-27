# Table Of Content

<ul>
 <li><a href="#client">Breeze API C# Client</a></li>
 <li><a href="#regulatoryChanges">Regulatory Changes</a></li>
 <li><a href="#docslink">API Documentation</a></li>
 <li><a href="#apiusage">API Usage</a></li>
 <li><a href="#websocket">Websocket Usage</a></li>
 <li><a href="#index_title">List Of Other SDK methods</a></li>
</ul>


<h3>Usage</h3>

This is a package to integrate streaming stocks for subscribed users & call APIs through which you can fetch live/historical data, automate your trading strategies, and monitor your portfolio in real time.

<h3 id="client">Breeze API C# Client</h3>

breezeapi@icicisecurities.com

The official C# client library for the ICICI Securities trading APIs. BreezeConnect is a set of REST-like APIs that allows one to build a complete investment and trading platform. Following are some notable features of Breeze APIs:

1. Execute orders in real time
2. Manage Portfolio
3. Access to 10 years of historical market data including 1 sec OHLCV
4. Streaming live OHLC (websockets)
5. Option Chain API

<h3 id="regulatoryChanges">Regulatory Changes</h3>

1) Orders must be placed only from the static IP address registered with ICICI Direct while procuring API key.
2) Primary or secondary static IP provided by the client can be updated only once per week.
3) Each client can have multiple API keys as per the circular, however for unregistered algos (Breeze API) the client is restricted to route orders via single API key.
4) A maximum combined limit of 10 orders per second is allowed, which includes order placement, cancellation, modification, and square-off requests.
5) Market orders are not permitted.
6) Placement, modification, or cancelation of Margin and Option Plus orders via the Breeze API is prohibited. 

<h3 id="docslink">API Documentation</h3>

<div class="sticky" >
<ul>
 <li><a href="https://api.icicidirect.com/breezeapi/documents/index.html">Breeze HTTP API Documentation</a></li>
</ul>
</div>

<h3 id="apiusage"> API Usage</h3>

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

                }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }  
        
        }
        
    }
}
```
<br>

<h3 id ="websocket"> Websocket Usage</h3>

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

                // Subscribe stocks feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync((exchangeCode: "NFO", stockCode: "ICIBAN", productType: "options", expiryDate: "24-Mar-2026", strikePrice: "650", right: "Put", getExchangeQuotes: true, getMarketDepth: false))));

                // Subscribe stocks feeds by stock-token
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("4.1!49937")));

                // Subscribe order notification feeds to get order data
                Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync(true)));

                // UnSubscribe order notification feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync(true)));

                // Unsubscribe stocks feeds
                Console.WriteLine(JsonSerializer.Serialize(await breeze.unsubscribeFeedsAsync((exchangeCode: "NFO", stockCode: "ICIBAN", productType: "options", expiryDate: "24-Mar-2026", strikePrice: "650", right: "Put", getExchangeQuotes: true, getMarketDepth: false))));

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


                // Callback to receive ticks.
                breeze.ticker((data) =>
                {
                    Console.WriteLine("Ticker Data:" + JsonSerializer.Serialize(data));
                });
                 }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

```
<br>

---

**NOTE**

Examples for stock_token are "4.1!38071" or "1.1!500780".

Template for stock_token : X.Y!<token>
X : exchange code
Y : Market Level data
Token : ISEC stock code

Value of X can be :
1 for BSE,
4 for NSE,
13 for NDX,
6 for MCX,
4 for NFO,

Value of Y can be :
1 for Level 1 data,
4 for Level 2 data

Token number can be obtained via get_names() function or downloading master security file via 
https://api.icicidirect.com/breezeapi/documents/index.html#instruments


exchange_code must be 'BSE', 'NSE', 'NDX', 'MCX' or 'NFO'.

stock_code should not be an empty string. Examples for stock_code are "WIPRO" or "ZEEENT".

product_type can be either 'Futures', 'Options' or an empty string. 
Product_type can not be an empty string for exchange_code 'NDX', 'MCX' and 'NFO'. 

strike_date can be in DD-MMM-YYYY(Ex.: 01-Jan-2022) or an empty string. 
strike_date can not be an empty string for exchange_code 'NDX', 'MCX' and 'NFO'.

strike_price can be float-value in string or an empty string. 
strike_price can not be an empty string for product_type 'Options'.

right can be either 'Put', 'Call' or an empty string. right can not be an empty string for product_type 'Options'.

Either get_exchange_quotes must be True or get_market_depth must be True. 

Both get_exchange_quotes and get_market_depth can be True, But both must not be False.

For Streaming OHLCV, interval must not be empty and must be equal to either of the following "1second","1minute", "5minute", "30minute"

---

<h3> List of other SDK Methods:</h3>

<h5 id="index_title" >Index</h5>

<div class="sticky" id="index">
<ul>
 <li><a href="#customer_detail">getCustomerDetails</a></li>
 <li><a href="#demat_holding">getDematHoldings</a></li>
 <li><a href="#get_funds">getFunds</a></li>
 <li><a href="#set_funds">setFunds</a></li>
 <li><a href="#historical_data1">getHistoricalData</a></li>
 <li><a href="#historical_data_v21">getHistoricalDatav2</a></li>
 <li><a href="#add_margin">addMargin</a></li>
 <li><a href="#get_margin">getMargin</a></li>
 <li><a href="#place_order">placeOrder</a></li>
 <li><a href="#order_detail">orderDetail</a></li>
 <li><a href="#order_list">orderList</a></li>
 <li><a href="#cancel_order">cancelOrder</a></li>
 <li><a href="#modify_order">modifyOrder</a></li>
 <li><a href="#portfolio_holding">getPortfolioHolding</a></li>
 <li><a href="#portfolio_position">getPortfolioPosition</a></li>
 <li><a href="#get_quotes">getQuotes</a></li>
 <li><a href="#get_option_chain">getOptionChainQuotes</a></li>
 <li><a href="#square_off1">squareOff</a></li>
 <li><a href="#modify_order">modifyOrder</a></li>
 <li><a href="#trade_list">getTradeList</a></li>
 <li><a href="#trade_detail">getTradeDetail</a></li>
 <li><a href="#get_names"> getNames </a></li>
 <li><a href="#preview_order"> previewOrder </a></li>
 <li><a href="#limit_calculator"> limitCalculator </a></li>
 <li><a href="#margin_calculator"> marginCalculator </a></li>

</ul>
</div>


<h3 id="customer_detail" > Get Customer details by api-session value.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getCustomerDetail(apiSession: "API_Session")));
```

<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id="demat_holding"> Get Demat Holding details of your account.</h3>

```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getDematHoldings()));
```
<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id="get_funds"> Get Funds details of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getFunds()));
```

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="set_funds"> Set Funds of your account</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.setFunds(transactionType: "debit", amount: "200", segment: "Equity")));
```

<p> Note: Set Funds of your account by transaction-type as "Credit" or "Debit" with amount in numeric string as rupees and segment-type as "Equity" or "FNO".</p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="historical_data1">Get Historical Data for Futures</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalData(interval: "1minute", fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", expiryDate: "2022-08-25T07:00:00.000Z", right: "others", strikePrice: "0")));
```

<a href="#index">Back to Index</a>

<h3 id="historical_data2">Get Historical Data for Equity</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalData(fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "ITC", exchangeCode: "NSE", productType: "cash")));
```

<a href="#index">Back to Index</a>


<h3 id="historical_data3">Get Historical Data for Options</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalData(interval: "1minute", fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "CNXBAN", exchangeCode: "NFO", productType: "options", expiryDate: "2022-08-25T07:00:00.000Z", right: "call", strikePrice: "38000")));
```


<p> Note : Get Historical Data for specific stock-code by mentioned interval either as "1minute", "5minute", "30minute" or as "1day"</p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="historical_data_v21">Get Historical Data (version 2) for Futures</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalDatav2(interval: "1minute", fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", expiryDate: "2022-08-25T07:00:00.000Z", right: "others", strikePrice: "0")));     
```

<a href="#index">Back to Index</a>

<h3 id="historical_data_v22">Get Historical Data (version 2) for Equity</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalDatav2(fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "ITC", exchangeCode: "NSE", productType: "cash")));
```

<a href="#index">Back to Index</a>
<h3 id="historical_data_v23">Get Historical Data (version 2) for Options</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getHistoricalDatav2(interval: "1minute", fromDate: "2022-08-15T07:00:00.000Z", toDate: "2022-08-17T07:00:00.000Z", stockCode: "CNXBAN", exchangeCode: "NFO", productType: "options", expiryDate: "2022-08-25T07:00:00.000Z", right: "call", strikePrice: "38000")));
```


<p> 
Note : 

1) Get Historical Data (version 2) for specific stock-code by mentioning interval either as "1second","1minute", "5minute", "30minute" or as "1day". 

2) Maximum candle intervals in one single request is 1000

</p>
<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id="add_margin">Add Margin to your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.addMargin(productType: "margin", stockCode: "ICIBAN", exchangeCode: "BSE", settlementId: "2021220", addAmount: "100", marginAmount: "3817.10", openQuantity: "10", coverQuantity: "0", categoryIndexPerStock: "", expiryDate: "", right: "", contractTag: "", strikePrice: "", segmentCode: "")));
```


<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="get_margin">Get Margin of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getMargin(exchangeCode: "NSE")));
```

<p> Note: Please change exchange_code=“NFO” to get F&O margin details </p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="place_order">Placing a Futures Order from your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", action: "buy", orderType: "limit", stoploss: "", quantity: "3200", price: "200", validity: "day", validityDate: "2022-08-22T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-08-25T06:00:00.000Z", right: "others", strikePrice: "0", userRemark: "Test")));
```                    


<h3 id="place_order2">Placing an Option Order from your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "NIFTY", exchangeCode: "NFO", productType: "option", action: "buy", orderType: "limit", stoploss: "", quantity: "50", price: "11.25", validity: "day", validityDate: "2022-12-02T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-12-08T06:00:00.000Z", right: "call", strikePrice: "19000", userRemark: "Test")));
```

<h3 id="place_order3">Place a cash order from your account.</h3>

```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode:"ITC", exchangeCode: "NSE", productType: "cash",action: "buy", orderType: "limit", stoploss: "", quantity: "1", price: "305", validity: "day")));

```   
<h4> NOTE: </h4>
<p><ol><li>Order Type should be "limit"</li>
       <li>The validity_date parameter has no impact on the order execution and even if you pass it while placing the order, it will be excluded from order processing.</li>
       <li> As per SEBI circular, "Safer participation of retail investors in Algorithmic trading", placing market orders through the Breeze API is not permitted. You are required to place limit orders instead of market orders.</li></ol></p>             

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="order_detail">Get an order details by exchange-code and order-id from your account.</h3>

```csharp
breezeConnect.getOrderDetail("NSE","20220819N100000001");
```                        

<p> Note: Please change exchange_code=“NFO” to get details about F&O</p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="order_list">Get order list of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getOrderList(exchangeCode: "NSE", fromDate: "2022-08-01T10:00:00.000Z", toDate: "2022-08-19T10:00:00.000Z")));
```

<p> Note: Please change exchange_code=“NFO” to get details about F&O</p>
<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id="order_list">Get order detail of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getOrderDetail(exchangeCode: "NSE", orderId: "20220819N100000001")));
```

<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id="cancel_order">Cancel an order from your account whose status are not Executed.</h3> 


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.cancelOrder(exchangeCode: "NSE", orderId: "20220819N100000001")));
```                    

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="modify_order">Modify an order from your account whose status are not Executed.</h3> 


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.modifyOrder(orderId: "202208191100000001", exchangeCode: "NFO", orderType: "limit", stoploss: "0", quantity: "250", price: "290100", validity: "day", disclosedQuantity: "0", validityDate: "2022-08-22T06:00:00.000Z")));
```
<h4> NOTE: </h4>
<p><ol><li>The validity_date parameter has no impact on the modification of the order and even if you pass it while modifying the order, it will be excluded from order modification processing.</li>
<li>As per SEBI circular, "Safer participation of retail investors in Algorithmic trading", modifying market orders through the Breeze API is not permitted. You are required to modify limit orders instead of market orders.</li></ol></p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="portfolio_holding">Get Portfolio Holdings of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getPortfolioHoldings(exchangeCode: "NFO", fromDate: "2022-08-01T06:00:00.000Z", toDate: "2022-08-19T06:00:00.000Z", stockCode: "", portfolioType: "")));
```

<p> Note: Please change exchange_code=“NSE” to get Equity Portfolio Holdings</p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="portfolio_position">Get Portfolio Positions from your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getPortfolioPositions()));
```

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="get_quotes">Get quotes of mentioned stock-code </h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getQuotes(stockCode: "ICIBAN", exchangeCode: "NFO", expiryDate: "2022-08-25T06:00:00.000Z", productType: "futures", right: "others", strikePrice: "0")));
```

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="get_option_chain">Get option-chain of mentioned stock-code for product-type Futures where input of expiry-date is not compulsory</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode:"ICIBAN",
                    exchangeCode:"NFO",
                    productType:"futures",
                    expiryDate:"2022-08-25T06:00:00.000Z")));
```                    

<br>
<a href="#index">Back to Index</a>

<h3 id="get_option_chain2">Get option-chain of mentioned stock-code for product-type Options where atleast 2 input is required out of expiry-date, right and strike-price</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode:"ICIBAN",
                    exchangeCode:"NFO",
                    productType:"options",
                    expiryDate:"2022-08-25T06:00:00.000Z",
                    right:"call",
                    strikePrice:"16850")));
```

<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="square_off1">Square off an Equity Margin Order</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NSE", productType: "margin", stockCode: "NIFTY", quantity: "10", price: "0", action: "sell", orderType: "limit", validity: "day", stoploss: "0", disclosedQuantity: "0", protectionPercentage: "", settlementId: "", coverQuantity: "", openQuantity: "", marginAmount: "", sourceFlag: "", expiryDate: "", right: "", strikePrice: "", validityDate: "", tradePassword: "", aliasName: "")));
```

<h4> NOTE: </h4>
<p><ol><li>The validity_date parameter has no impact on the square off order execution and even if you pass it while squaring off the position, it will be excluded from square off order processing.</li>
<li>As per SEBI circular, "Safer participation of retail investors in Algorithmic trading", squaring off orders to market orders through the Breeze API is not permitted. You are required to place limit or stoploss order instead of market orders.</li>
<li>Please refer getPortfolioPositions() for settlement id and margin_amount</li></ol></p>
<br>
<a href="#index">Back to Index</a>

<h3 id="square_off2">Square off an FNO Futures Order</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NFO", productType: "futures", stockCode: "ICIBAN", expiryDate: "2022-08-25T06:00:00.000Z", action: "sell", orderType: "limit", validity: "day", stoploss: "0", quantity: "50", price: "0", validityDate: "2022-08-12T06:00:00.000Z", tradePassword: "", disclosedQuantity: "0", sourceFlag: "", protectionPercentage: "", settlementId: "", marginAmount: "", openQuantity: "", coverQuantity: "", right: "", strikePrice: "", aliasName: "")));
```
<h4> NOTE: </h4>
<p><ol><li>The validity_date parameter has no impact on the square off order execution and even if you pass it while squaring off the position, it will be excluded from square off order processing.</li>
<li>As per SEBI circular, "Safer participation of retail investors in Algorithmic trading", squaring off orders to market orders through the Breeze API is not permitted. You are required to place limit or stoploss order instead of market orders.</li>
</ol></p>
<br>
<a href="#index">Back to Index</a>

<h3 id="square_off3">Square off an FNO Options Order</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.squareOff(exchangeCode: "NFO", productType: "options", stockCode: "ICIBAN", expiryDate: "2022-08-25T06:00:00.000Z", right: "Call", strikePrice: "16850", action: "sell", orderType: "limit", validity: "day", stoploss: "0", quantity: "50", price: "0", validityDate: "2022-08-12T06:00:00.000Z", tradePassword: "", disclosedQuantity: "0", sourceFlag: "", protectionPercentage: "", settlementId: "", marginAmount: "", openQuantity: "", coverQuantity: "", aliasName: "")));
```                    
<h4> NOTE: </h4>
<p><ol><li>The validity_date parameter has no impact on the square off order execution and even if you pass it while squaring off the position, it will be excluded from square off order processing.</li>
<li>As per SEBI circular, "Safer participation of retail investors in Algorithmic trading", squaring off orders to market orders through the Breeze API is not permitted. You are required to place limit or stoploss order instead of market orders.</li></ol></p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="trade_list">Get trade list of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getTradeList(fromDate: "2022-08-01T06:00:00.000Z", toDate: "2022-08-19T06:00:00.000Z", exchangeCode: "NSE", productType: "", action: "", stockCode: "")));
```                        

<p> Note: Please change exchange_code=“NFO” to get details about F&O</p>
<br>
<a href="#index">Back to Index</a>
<hr>

<h3 id="trade_detail">Get trade detail of your account.</h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getTradeDetail(exchangeCode: "NSE", orderId: "20220819N100000005")));
```

<p> Note: Please change exchange_code=“NFO” to get details about F&O</p>
<br>
<a href="#index">Back to Index</a>
<hr>


<h3 id = "get_names">Get Names </h3>


```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.getNames(exchange : "NSE", stockCode : "RELIANCE")));
```
<p>Note: Use this method to find ICICI specific stock codes / token </p>

<a href="#index">Back to Index</a>

<hr>

<h3 id = "preview_order">Preview Order </h3>

```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.previewOrder(stockCode:"ICIBAN",exchangeCode:"NSE",productType:"margin",orderType :"limit", price:"907.05", action:"buy", quantity:"1", expiryDate:"", right:"", strikePrice:"", specialFlag:"N", stoploss:"", orderRateFresh:"")));
```
<a href="#index">Back to Index</a>

<hr>

<h3 id = "limit_calculator">Limit Calculator </h3>

```csharp
Console.WriteLine(JsonSerializer.Serialize(breeze.limitCalculator(strikePrice:"19200", productType : "option", expiryDate : "06-JUL-2023", underlying:"NIFTY", exchangeCode : "NFO", orderFlow : "Buy", stopLossTrigger : "200.00", optionType : "Call", sourceFlag : "P", limitRate : "", orderReference : "", availableQuantity : "", marketType:"limit", freshOrderLimit:"177.70")));
```
<a href="#index">Back to Index</a>

<hr>

<h3 id = "margin_calculator">Margin Calculator </h3>

```csharp
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

    Console.WriteLine(JsonSerializer.Serialize(breeze.marginCalculator(listOfPositions : listOfPositions,exchangeCode:"NFO")));
    
```
<a href="#index">Back to Index</a>