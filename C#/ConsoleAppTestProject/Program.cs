using System;
using System.Text.Json;
using System.Threading.Tasks;
using Breeze;

//.Net Core 3.1
namespace ConsoleAppTestProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                ////////////////////////Initiate////////////////////////

                BreezeConnect breeze = new BreezeConnect("App key");
                breeze.generateSessionAsPerVersion("Secret key", "Session key");
                ////////////////////////WebSocket////////////////////////
                var responseobject = await breeze.wsConnectAsync();
                // Get Customer details by api-session value.
                Console.WriteLine(JsonSerializer.Serialize(breeze.getCustomerDetail(apiSession: "48479684")));

                //Get Demat Holding details of your account.
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
                Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "ICIBAN", exchangeCode: "NFO", productType: "futures", action: "buy", orderType: "limit", stoploss: "0", quantity: "3200", price: "200", validity: "day", validityDate: "2022-08-22T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-08-25T06:00:00.000Z", right: "others", strikePrice: "0", userRemark: "Test", orderTypeFresh: "", orderRateFresh: "")));

                //place an option plus order

                Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "NIFTY", exchangeCode: "NFO", productType: "optionplus", action: "buy", orderType: "limit", stoploss: "15", quantity: "50", price: "11.25", validity: "day", validityDate: "2022-12-02T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2022-12-08T06:00:00.000Z", right: "call", strikePrice: "19000", orderTypeFresh : "Limit", orderRateFresh : "20", userRemark: "Test")));

                
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
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode: "ICIBAN",
                    exchangeCode: "NFO",
                    productType: "futures",
                    expiryDate: "2022-08-25T06:00:00.000Z",
                    right : "others",
                    strikePrice: "0")));

                //Get option-chain of mentioned stock-code for product-type Options where atleast 2 input is required out of expiry-date, right and strike-price
                Console.WriteLine(JsonSerializer.Serialize(breeze.getOptionChainQuotes(stockCode: "ICIBAN",
                    exchangeCode: "NFO",
                    productType: "options",
                    expiryDate: "2022-08-25T06:00:00.000Z",
                    right: "call",
                    strikePrice: "16850")));

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

                //Get Names for NSE codes

               Console.WriteLine(JsonSerializer.Serialize(breeze.getNames(exchange: "NSE", stockCode: "RELIANCE")));
                //Note: Use this method to find ICICI specific stock codes / token

                //Place an order from your account.
               Console.WriteLine(JsonSerializer.Serialize(breeze.placeOrder(stockCode: "NIFTY", exchangeCode: "NFO", productType: "options", action: "buy", orderType: "limit", stoploss: "0", quantity: "25", price: "0.30", validity: "day", validityDate: "2024-10-24T06:00:00.000Z", disclosedQuantity: "0", expiryDate: "2024-10-24T06:00:00.000Z", right: "call", strikePrice: "0", userRemark: "Test", orderTypeFresh : "", orderRateFresh : "")));
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync(
                //        /* exchangeCode: */"NFO",
                //        /* stockCode:*/ "NIFTY",
                //        /* productType:*/ "options",
                //        /* expiryDate: */ "10-Oct-2024",
                //        /* strikePrice: */ "24900",
                //        /* right: */ "Put",
                //        /* getExchangeQuotes:*/ true,
                //        /* getMarketDepth: */ false)
                //    ));

                breeze.ticker((data) =>
                {
                    Console.WriteLine("Ticker Data:" + JsonSerializer.Serialize(data));
                });
                Console.WriteLine(JsonSerializer.Serialize(responseobject));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
