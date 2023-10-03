using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SocketIOClient;
using RestSharp;
using System.Security.Cryptography;
using System.Net.Http;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Breeze
{
    public class BreezeConnect
    {
        private string _apiKey;
        ApificationBreeze _apiHandler = new ApificationBreeze();
        SocketEventBreeze _socketHandler = new SocketEventBreeze();
        SocketEventBreeze _socketHandlerOrder = new SocketEventBreeze(); //for order refresh
        SocketEventBreeze _socketHandlerOhlcv = new SocketEventBreeze(); //for ohlc

        private Dictionary<string, string>[] _stockScriptDictList;
        public SocketIO _socket = null;
        public SocketIO _socketOrder = null;
        public SocketIO _socketOhlcv = null;

        public BreezeConnect(string apiKey)
        {
            this._apiKey = apiKey;

        }

        public async void generateSessionUsingHttpHandler(string secretKey, string sessionToken, bool debug = false)
        {
            var body = JsonSerializer.Serialize(new Dictionary<string, string> { { "SessionToken", sessionToken }, { "AppKey", _apiKey } });

            var request = WebRequest.Create(new Uri("https://api.icicidirect.com/breezeapi/api/v1/customerdetails"));

            request.ContentType = "application/json";
            request.Method = "GET";

            var type = request.GetType();
            var currentMethod = type.GetProperty("CurrentMethod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(request);

            var methodType = currentMethod.GetType();
            methodType.GetField("ContentBodyNotAllowed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(currentMethod, false);

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(body);
            }

            var response = request.GetResponse();
            //JsonSerializer.Deserialize<Dictionary<string, object>>(request.GetResponse().ToString()).TryGetValue("session_token", out object sessionTokenObject);

            var responseStream = response.GetResponseStream();
            string sessionTokenObject = "";
            try
            {
                if (responseStream != null)
                {
                    var myStreamReader = new StreamReader(responseStream, Encoding.Default);
                    string resultEntity = myStreamReader.ReadToEnd();
                    JObject json = JObject.Parse(resultEntity);

                    foreach (var e in json)
                    {
                        //Console.WriteLine(e.Value["session_token"]);
                        sessionTokenObject = (string)e.Value["session_token"];
                        break;
                    }
                    string base64SessionToken = sessionTokenObject.ToString();
                    _apiHandler.setSession(_apiKey, secretKey, base64SessionToken);
                    byte[] data = Convert.FromBase64String(base64SessionToken);
                    string decodedString = Encoding.UTF8.GetString(data);
                    _socketHandler.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);
                    _socketHandlerOrder.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);
                    myStreamReader.ReadToEnd();
                    _socketHandlerOhlcv.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);

                }
                responseStream.Close();
                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Customer Detail API call failed");
            }
        }

        public async void generateSession(string secretKey, string sessionToken, bool debug = false)
        {
            RestClient client = new RestClient("https://api.icicidirect.com/breezeapi/api/v1/");
            var request = new RestRequest("customerdetails", Method.Get);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonSerializer.Serialize(new Dictionary<string, string> { { "SessionToken", sessionToken }, { "AppKey", _apiKey } });
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Dictionary<string, object> dictResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content);
                if (dictResponse.TryGetValue("Status", out object statusValue) && int.Parse(JsonSerializer.Serialize(statusValue)) != 200)
                {
                    dictResponse.TryGetValue("Error", out object errorMessage);
                    Console.WriteLine("Fail to generate session.\nError: " + errorMessage.ToString());
                }
                else
                {
                    dictResponse.TryGetValue("Success", out object successObject);
                    JsonSerializer.Deserialize<Dictionary<string, object>>(successObject.ToString()).TryGetValue("session_token", out object sessionTokenObject);
                    string base64SessionToken = sessionTokenObject.ToString();
                    _apiHandler.setSession(_apiKey, secretKey, base64SessionToken);
                    byte[] data = Convert.FromBase64String(base64SessionToken);
                    string decodedString = Encoding.UTF8.GetString(data);
                    _socketHandler.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);
                    _socketHandlerOrder.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);
                    _socketHandlerOhlcv.setSession(userId: decodedString.Split(separator: ':')[0], sessionToken: decodedString.Split(separator: ':')[1], tokenScriptDictList: getStockScriptList(), debug: debug, apiKey: _apiKey);
                }
            }
        } //Need to handle exception


        public void generateSessionAsPerVersion(string secretKey, string sessionToken, bool debug = false)
        {
#if NET472_OR_GREATER
                        this.generateSessionUsingHttpHandler(secretKey, sessionToken);
#else
            this.generateSession(secretKey, sessionToken);
#endif

        }
        private Dictionary<string, string[]>[] getStockScriptList()
        {
            _stockScriptDictList = new Dictionary<string, string>[5]
            {
                new Dictionary<string, string>{},
                new Dictionary<string, string>{},
                new Dictionary<string, string>{},
                new Dictionary<string, string>{},
                new Dictionary<string, string>{}
            };
            var tokenScriptDictList = new Dictionary<string, string[]>[5]
            {
                new Dictionary<string, string[]>{},
                new Dictionary<string, string[]>{},
                new Dictionary<string, string[]>{},
                new Dictionary<string, string[]>{},
                new Dictionary<string, string[]>{}
            }; ;
            using (WebClient web = new WebClient())
            {
                var file = web.DownloadData("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/StockScriptNew.csv");
                var stockDataList = Encoding.UTF8.GetString(file).Split('\n');
                foreach (var rowString in stockDataList)
                {
                    if (rowString == "") continue;
                    var row = rowString.Split(',');
                    if (row[5] == "0" || row[5] == "00" || row[5] == "NA") continue;
                    if (row[2] == "BSE")
                    {
                        _stockScriptDictList[0].Add(row[3], row[5]);
                        string[] values = { row[3], row[1] };
                        tokenScriptDictList[0] = Miscellaneous.tryAddToDictionary(row[5], values, tokenScriptDictList[0]);
                    }
                    else if (row[2] == "NSE")
                    {
                        _stockScriptDictList[1].Add(row[3], row[5]);
                        string[] values = { row[3], row[1] };
                        tokenScriptDictList[1] = Miscellaneous.tryAddToDictionary(row[5], values, tokenScriptDictList[1]);
                    }
                    else if (row[2] == "NDX")
                    {
                        _stockScriptDictList[2].Add(row[7], row[5]);
                        string[] values = { row[7], row[1] };
                        tokenScriptDictList[2] = Miscellaneous.tryAddToDictionary(row[5], values, tokenScriptDictList[2]);
                    }
                    else if (row[2] == "MCX")
                    {
                        _stockScriptDictList[3].Add(row[7], row[5]);
                        string[] values = { row[7], row[1] };
                        tokenScriptDictList[3] = Miscellaneous.tryAddToDictionary(row[5], values, tokenScriptDictList[3]);
                    }
                    else if (row[2] == "NFO")
                    {
                        _stockScriptDictList[4].Add(row[7], row[5]);
                        string[] values = { row[7], row[1] };
                        tokenScriptDictList[4] = Miscellaneous.tryAddToDictionary(row[5], values, tokenScriptDictList[4]);
                    }
                }
            }
            return tokenScriptDictList;
        }

        public async Task<Dictionary<string, object>> wsConnectAsyncOrder()
        {
            _socketOrder = await _socketHandlerOrder.ConnectForOrder();
            if (!_socketHandlerOrder.hasSession())
                return new Dictionary<string, object>
                {
                    { "Success" , null },
                    { "Status" , 500 },
                    { "Error" , "Session not generated. Please generate session." },
                };
            else if (_socketHandlerOrder.isConnected(true))
                return new Dictionary<string, object>() {
                    {"Success",null},
                    {"Status",500},
                    {"Error","Socket connection already established. for order streaming"}
                };
            else
            {

                if (_socketHandlerOrder.isConnected(true))
                    return new Dictionary<string, object>() {
                        {"Success","Socket connection for order streaming established."},
                        {"Status",200},
                        {"Error",null}
                    };
                else
                    return new Dictionary<string, object>() {
                        {"Success",null},
                        {"Status",500},
                        {"Error","Fail to establish Socket connection. for order streaming"}
                    };
            }
        }

        public async Task<Dictionary<string, object>> wsConnectAsyncOhlcv()
        {
            _socketOhlcv = await _socketHandlerOhlcv.ConnectForOhlcv();
            if (!_socketHandlerOhlcv.hasSession())
                return new Dictionary<string, object>
                {
                    { "Success" , null },
                    { "Status" , 500 },
                    { "Error" , "Session not generated. Please generate session." },
                };
            else if (_socketHandlerOhlcv.isConnected(false, true))
                return new Dictionary<string, object>() {
                    {"Success",null},
                    {"Status",500},
                    {"Error","Socket connection already established. for ohlc streaming"}
                };
            else
            {
                if (_socketHandlerOhlcv.isConnected(false, true))
                    return new Dictionary<string, object>() {
                        {"Success","Socket connection for ohlc streaming established."},
                        {"Status",200},
                        {"Error",null}
                    };
                else
                    return new Dictionary<string, object>() {
                        {"Success",null},
                        {"Status",500},
                        {"Error","Fail to establish Socket connection. for ohlc streaming"}
                    };
            }
        }

        public async Task<Dictionary<string, object>> wsConnectAsync()
        {
            Console.WriteLine(JsonSerializer.Serialize(wsConnectAsyncOrder()));
            Console.WriteLine(JsonSerializer.Serialize(wsConnectAsyncOhlcv()));

            _socket = await _socketHandler.Connect();
            if (!_socketHandler.hasSession())
                return new Dictionary<string, object>
                {
                    { "Success" , null },
                    { "Status" , 500 },
                    { "Error" , "Session not generated. Please generate session." },
                };
            else if (_socketHandler.isConnected(false))
                return new Dictionary<string, object>() {
                    {"Success",null},
                    {"Status",500},
                    {"Error","Socket connection already established. FOR rate refresh"}
                };
            else
            {

                if (_socketHandler.isConnected(false))
                    return new Dictionary<string, object>() {
                        {"Success","Socket connection established. for rate refresh"},
                        {"Status",200},
                        {"Error",null}
                    };
                else
                    return new Dictionary<string, object>() {
                        {"Success",null},
                        {"Status",500},
                        {"Error","Fail to establish Socket connection. for rate refresh"}
                    };
            }
        }

        public async Task<Dictionary<string, object>> subscribeFeedsAsync(string stockToken, bool isStrategy = false)
        {
            if (stockToken is null || stockToken.Length == 0)
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Stock-Token cannot be empty" }
                };
            if (!isStrategy)
                await _socketHandler.watch(new string[] { stockToken });
            else
                await _socketHandlerOrder.watchStrategy(stockToken);
            return new Dictionary<string, object>() {
                { "Success", "Stock " + stockToken + " subscribed successfully" },
                { "Status", 200 },
                { "Error", null }
            };
        }

        public async Task<Dictionary<string, object>> subscribeFeedsAsync(string stockToken, string channel)
        {

            await _socketHandlerOhlcv.watchOhlcv(stockToken, channel);
            return new Dictionary<string, object>() {
                { "Success", "Stock " + stockToken + " subscribed successfully" },
                { "Status", 200 },
                { "Error", null }
            };
        }

        public async Task<Dictionary<string, object>> subscribeFeedsAsync(bool getOrderNotification = false)
        {
            if (!getOrderNotification)
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "getOrderNotification should be true" }
                };
            else if (_socketHandlerOrder.isOrderNotificationSubscribed())
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Order Notification already subscribed" }
                };
            else
            {
                _socketHandlerOrder.setOrderNotificationSubscription(getOrderNotification);
                return new Dictionary<string, object>() {
                    { "Success", "Order Notification subscribed successfully" },
                    { "Status", 200 },
                    { "Error", null }
                };
            }
        }

        public async Task<Dictionary<string, object>> subscribeFeedsAsync(string exchangeCode, string stockCode, string productType, string expiryDate, string strikePrice, string right, bool getExchangeQuotes, bool getMarketDepth)
        {
            string[] tokenObject = getStockTokenValue(exchangeCode, stockCode, productType, expiryDate, strikePrice, right, getExchangeQuotes, getMarketDepth);
            if (tokenObject.Length != 2)
            {
                return new Dictionary<string, object>{
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Stock-Token not found for Stock-Code " + stockCode + "." }
                };
            }
            if (!string.IsNullOrEmpty(tokenObject[0]))
                await _socketHandler.watch(new string[] { tokenObject[0] });
            if (!string.IsNullOrEmpty(tokenObject[1]))
                await _socketHandler.watch(new string[] { tokenObject[1] });
            return new Dictionary<string, object>{
                    { "Success", "Stock-Code " + stockCode + " subscribed successfully." },
                    { "Status", 200 },
                    { "Error", null }
                };
        }

        public async Task<Dictionary<string, object>> unsubscribeFeedsAsync(string stockToken, string channel)
        {

            await _socketHandlerOhlcv.unwatchOhlcv(stockToken, channel);
            return new Dictionary<string, object>() {
                { "Success", "Stock " + stockToken + " subscribed successfully" },
                { "Status", 200 },
                { "Error", null }
            };
        }

        public async Task<Dictionary<string, object>> unsubscribeFeedsAsync(string stockToken, bool isStrategy = false)
        {
            if (stockToken is null || stockToken.Length == 0)
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Stock-Token cannot be empty" }
                };
            if (!isStrategy)
                await _socketHandler.unWatch(new string[] { stockToken });
            else
                await _socketHandlerOrder.unWatchStrategy(stockToken);
            return new Dictionary<string, object>() {
                { "Success", "Stock " + stockToken + " unsubscribed successfully" },
                { "Status", 200 },
                { "Error", null }
            };
        }

        public Dictionary<string, object> unsubscribeFeeds(bool getOrderNotification = true)
        {
            if (getOrderNotification)
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "getOrderNotification should be false" }
                };
            else if (!_socketHandlerOrder.isOrderNotificationSubscribed())
                return new Dictionary<string, object>() {
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Order Notification is not subscribed" }
                };
            else
            {
                _socketHandlerOrder.setOrderNotificationSubscription(getOrderNotification);
                return new Dictionary<string, object>() {
                    { "Success", "Order Notification unsubscribed successfully" },
                    { "Status", 200 },
                    { "Error", null }
                };
            }
        }

        public async Task<Dictionary<string, object>> unsubscribeFeedsAsync(string exchangeCode, string stockCode, string productType, string expiryDate, string strikePrice, string right, bool getExchangeQuotes, bool getMarketDepth)
        {
            string[] tokenObject = getStockTokenValue(exchangeCode, stockCode, productType, expiryDate, strikePrice, right, getExchangeQuotes, getMarketDepth);
            if (tokenObject.Length != 2)
            {
                return new Dictionary<string, object>{
                    { "Success", null },
                    { "Status", 500 },
                    { "Error", "Stock-Token not found for Stock-Code " + stockCode + "." }
                };
            }
            if (!string.IsNullOrEmpty(tokenObject[0]))
                await _socketHandler.unWatch(new string[] { tokenObject[0] });
            if (!string.IsNullOrEmpty(tokenObject[1]))
                await _socketHandler.unWatch(new string[] { tokenObject[1] });
            return new Dictionary<string, object>{
                { "Success", "Stock-Code " + stockCode + " unsubscribed successfully." },
                { "Status", 200 },
                { "Error", null }
            };
        }

        private string[] getStockTokenValue(string exchangeCode, string stockCode, string productType, string expiryDate, string strikePrice, string right, bool getExchangeQuotes, bool getMarketDepth)
        {
            if (!getExchangeQuotes && !getMarketDepth)
                throw new Exception("Either getExchangeQuotes must be true or getMarketDepth must be true");
            else
            {
                Dictionary<string, string> exchangeCodeList = new Dictionary<string, string>();
                exchangeCodeList.Add("BSE", "1.");
                exchangeCodeList.Add("NSE", "4.");
                exchangeCodeList.Add("NDX", "13.");
                exchangeCodeList.Add("MCX", "6.");
                exchangeCodeList.Add("NFO", "4.");
                _ = exchangeCodeList.TryGetValue(exchangeCode, out string exchangeCodeName) ? _ = "" : exchangeCodeName = "";
                if (exchangeCodeName is null || exchangeCodeName.Length == 0)
                    throw new Exception("Exchange Code allowed are 'BSE', 'NSE', 'NDX', 'MCX' or 'NFO'.");
                else if (stockCode is null || stockCode.Length == 0)
                    throw new Exception("Stock-Code cannot be empty.");
                else
                {
                    string tokenValue = "";
                    if (exchangeCode.ToLower() == "bse")
                        _stockScriptDictList[0].TryGetValue(stockCode, out tokenValue);
                    else if (exchangeCode.ToLower() == "nse")
                        _stockScriptDictList[1].TryGetValue(stockCode, out tokenValue);
                    else
                    {
                        if (expiryDate is null || expiryDate.Length == 0)
                            throw new Exception("Expiry-Date cannot be empty for given Exchange-Code.");
                        string contractDetailValue = "";
                        if (productType.ToLower() == "futures")
                            contractDetailValue = "FUT";
                        else if (productType.ToLower() == "options")
                            contractDetailValue = "OPT";
                        else
                            throw new Exception("Product-Type should either be Futures or Options for given Exchange-Code.");
                        contractDetailValue = contractDetailValue + "-" + stockCode + "-" + expiryDate;
                        if (productType.ToLower() == "options")
                        {
                            if (strikePrice is null || strikePrice == "")
                                throw new Exception("Strike Price cannot be empty for Product-Type 'Options'.");
                            else
                                contractDetailValue = contractDetailValue + "-" + strikePrice;
                            if (right.ToLower() == "put")
                                contractDetailValue = contractDetailValue + "-" + "PE";
                            else if (right.ToLower() == "call")
                                contractDetailValue = contractDetailValue + "-" + "CE";
                            else
                                throw new Exception("Rights should either be Put or Call for Product-Type 'Options'.");
                        }
                        if (exchangeCode.ToLower() == "ndx")
                            _stockScriptDictList[2].TryGetValue(contractDetailValue, out tokenValue);
                        else if (exchangeCode.ToLower() == "mcx")
                            _stockScriptDictList[3].TryGetValue(contractDetailValue, out tokenValue);
                        else if (exchangeCode.ToLower() == "nfo")
                            _stockScriptDictList[4].TryGetValue(contractDetailValue, out tokenValue);
                    }
                    if (string.IsNullOrEmpty(tokenValue))
                        throw new Exception("Stock-Code not found.");

                    string exchangeQuotesTokenValue = "";
                    if (getExchangeQuotes)
                        exchangeQuotesTokenValue = exchangeQuotesTokenValue + "1!" + tokenValue;

                    string marketDepthTokenValue = "";
                    if (getMarketDepth)
                        marketDepthTokenValue = exchangeCodeName + "2!" + tokenValue;

                    return new string[2] { exchangeQuotesTokenValue, marketDepthTokenValue };
                }
            }
        }



        public void ticker(Action<Object> callback)
        {
            while (true)
            {
                if (_socket.Disconnected)
                {
                    wsConnectAsync();
                    Console.WriteLine("reconnection established");
                    _socketHandler.rewatch();

                }

                if (_socket is null)
                {
                    _socket = _socketHandler.GetSocketIO(false);
                    if (_socket is null)
                        throw new Exception("Socket not connected. Cannot return any ticks.");
                    else if (!_socket.Connected)
                        throw new Exception("Socket not connected. Cannot return any ticks.");
                }
                if (_socketOrder is null)
                {
                    _socketOrder = _socketHandlerOrder.GetSocketIO(true);
                    if (_socketOrder is null)
                        throw new Exception("Socket not connected. Cannot return any ticks for order streaming");
                    else if (!_socketOrder.Connected)
                        throw new Exception("Socket not connected. Cannot return any ticks. for order streaming");
                }

                if (_socketOhlcv is null)
                {
                    _socketOhlcv = _socketHandlerOhlcv.GetSocketIO(false, true);
                    if (_socketOhlcv is null)
                        throw new Exception("Socket not connected. Cannot return any ticks for ohlcv streaming");
                    else if (!_socketOhlcv.Connected)
                        throw new Exception("Socket not connected. Cannot return any ticks. for ohlcv streaming");
                }

                _socket.On("stock", response =>
                {
                    object data = _socketHandler.parseData(response.GetValue());
                    callback(data);
                });

                _socketOrder.On("stock", response =>
                {
                    object data = _socketHandler.parseStrategy(response.GetValue());
                    callback(data);
                });

                _socket.On("connection", response =>
                {
                    Console.WriteLine("connection Triggered");
                });

                _socketOrder.On("order", response =>
                {
                    if (_socketHandlerOrder.isOrderNotificationSubscribed())
                    {
                        object data = _socketHandlerOrder.parseOrderData(response.GetValue());
                        callback(data);
                    }
                });

                _socketOhlcv.On("1second", response =>
                {
                    object data = _socketHandlerOhlcv.parseOhlcv(response.GetValue());
                    callback(data);
                });

                _socketOhlcv.On("1minute", response =>
                {
                    object data = _socketHandlerOhlcv.parseOhlcv(response.GetValue());
                    callback(data);
                });

                _socketOhlcv.On("5minute", response =>
                {
                    object data = _socketHandlerOhlcv.parseOhlcv(response.GetValue());
                    callback(data);
                });

                _socketOhlcv.On("30minute", response =>
                {
                    object data = _socketHandlerOhlcv.parseOhlcv(response.GetValue());
                    callback(data);
                });

                //return;
            }
        }

        public Dictionary<string, object> getCustomerDetail(string apiSession) { return _apiHandler.getCustomerDetail(apiSession); }
        public Dictionary<string, object> getDematHoldings() { return _apiHandler.getDematHoldings(); }
        public Dictionary<string, object> getFunds() { return _apiHandler.getFunds(); }
        public Dictionary<string, object> setFunds(string transactionType, string amount, string segment) { return _apiHandler.setFunds(transactionType, amount, segment); }
        public Dictionary<string, object> getHistoricalData(string interval, string fromDate, string toDate, string stockCode, string exchangeCode, string productType = "", string expiryDate = "", string right = "", string strikePrice = "") { return _apiHandler.getHistoricalData(interval, fromDate, toDate, stockCode, exchangeCode, productType, expiryDate, right, strikePrice); }
        public Dictionary<string, object> addMargin(string exchangeCode, string productType, string stockCode, string coverQuantity, string settlementId, string addAmount, string marginAmount, string openQuantity, string categoryIndexPerStock, string expiryDate, string right, string contractTag, string strikePrice, string segmentCode) { return _apiHandler.addMargin(exchangeCode, productType, stockCode, coverQuantity, settlementId, addAmount, marginAmount, openQuantity, categoryIndexPerStock, expiryDate, right, contractTag, strikePrice, segmentCode); }
        public Dictionary<string, object> getMargin(string exchangeCode) { return _apiHandler.getMargin(exchangeCode); }
        public Dictionary<string, object> placeOrder(string stockCode, string exchangeCode, string productType, string action, string orderType, string stoploss, string quantity, string price, string validity, string validityDate, string disclosedQuantity, string expiryDate, string right, string strikePrice, string userRemark, string orderTypeFresh, string orderRateFresh) { return _apiHandler.placeOrder(stockCode, exchangeCode, productType, action, orderType, stoploss, quantity, price, validity, validityDate, disclosedQuantity, expiryDate, right, strikePrice, userRemark, orderTypeFresh, orderRateFresh); }
        public Dictionary<string, object> getOrderDetail(string exchangeCode, string orderId) { return _apiHandler.getOrderDetail(exchangeCode, orderId); }
        public Dictionary<string, object> getOrderList(string exchangeCode, string fromDate, string toDate) { return _apiHandler.getOrderList(exchangeCode, fromDate, toDate); }
        public Dictionary<string, object> cancelOrder(string exchangeCode, string orderId) { return _apiHandler.cancelOrder(exchangeCode, orderId); }
        public Dictionary<string, object> modifyOrder(string orderId, string exchangeCode, string orderType, string stoploss, string quantity, string price, string validity, string disclosedQuantity, string validityDate) { return _apiHandler.modifyOrder(orderId, exchangeCode, orderType, stoploss, quantity, price, validity, disclosedQuantity, validityDate); }
        public Dictionary<string, object> getPortfolioHoldings(string exchangeCode, string fromDate, string toDate, string stockCode = "", string portfolioType = "") { return _apiHandler.getPortfolioHoldings(exchangeCode, fromDate, toDate, stockCode, portfolioType); }
        public Dictionary<string, object> getPortfolioPositions() { return _apiHandler.getPortfolioPositions(); }
        public Dictionary<string, object> getQuotes(string stockCode, string exchangeCode, string expiryDate, string productType, string right, string strikePrice) { return _apiHandler.getQuotes(stockCode, exchangeCode, expiryDate, productType, right, strikePrice); }
        public Dictionary<string, object> getOptionChainQuotes(string stockCode, string exchangeCode, string expiryDate, string productType, string right, string strikePrice) { return _apiHandler.getOptionChainQuotes(stockCode, exchangeCode, expiryDate, productType, right, strikePrice); }
        public Dictionary<string, object> squareOff(string sourceFlag, string stockCode, string exchangeCode, string quantity, string price, string action, string orderType, string validity, string stoploss, string disclosedQuantity, string protectionPercentage, string settlementId, string marginAmount, string openQuantity, string coverQuantity, string productType, string expiryDate, string right, string strikePrice, string validityDate, string tradePassword, string aliasName)
        { return _apiHandler.squareOff(sourceFlag, stockCode, exchangeCode, quantity, price, action, orderType, validity, stoploss, disclosedQuantity, protectionPercentage, settlementId, marginAmount, openQuantity, coverQuantity, productType, expiryDate, right, strikePrice, validityDate, tradePassword, aliasName); }
        public Dictionary<string, object> getTradeList(string fromDate, string toDate, string exchangeCode, string productType, string action, string stockCode) { return _apiHandler.getTradeList(fromDate, toDate, exchangeCode, productType, action, stockCode); }

        public Dictionary<string, object> getTradeDetail(string exchangeCode, string orderId) { return _apiHandler.getTradeDetail(exchangeCode, orderId); }

        public Dictionary<string, object> getNames(string exchange, string stockCode) { return _apiHandler.getNames(exchange, stockCode); }

        public Dictionary<string, object> previewOrder(string stockCode, string exchangeCode, string productType, string orderType, string price, string action, string quantity, string expiryDate, string right, string strikePrice, string specialFlag, string stoploss, string orderRateFresh) { return _apiHandler.previewOrder(stockCode, exchangeCode, productType, orderType, price, action, quantity, expiryDate, right, strikePrice, specialFlag, stoploss, orderRateFresh); }
    }

    public class ApificationBreeze
    {
        private string _base64SessionToken = null;
        private string _secretKey = null;
        private string _apiKey = null;
        RestClient _client = new RestClient("https://api.icicidirect.com/breezeapi/api/v1/");
        private string[] transactionTypeList = { "debit", "credit" };
        private string[] intervalList = { "1minute", "5minute", "30minute", "1day" };
        private string[] exchangeCodeList = { "nse", "nfo" };
        private string[] nfoProductTypeList = { "futures", "options", "futureplus", "optionplus" };
        private string[] productTypeList = { "futures", "options", "futureplus", "optionplus", "cash", "eatm", "margin" };
        private string[] rightList = { "call", "put", "others" };
        private string[] actionList = { "buy", "sell" };
        private string[] orderTypeList = { "limit", "market", "stoploss" };
        private string[] validityList = { "day", "ioc", "vtc" };

        private bool checkList(string[] listName, string valueToCheck)
        {
            return listName.FirstOrDefault(elementOfList => elementOfList.Contains(valueToCheck.ToLower())) != null;
        }

        public bool hasSession()
        {
            return !(_apiKey is null || _secretKey is null || _base64SessionToken is null);
        }

        public void setSession(string apiKey, string secretKey, string base64SessionToken)
        {
            this._apiKey = apiKey;
            this._secretKey = secretKey;
            this._base64SessionToken = base64SessionToken;
        }

        private static string currentTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.000Z");
        }

        private RestRequest prepRequestHeader(Dictionary<string, object> body, RestRequest request)
        {
            try
            {
                string timestampValue = currentTimestamp();
                request.AddHeader("Content-type", "application/json");
                string hashString = string.Empty;
                byte[] bytes = Encoding.UTF8.GetBytes(timestampValue + JsonSerializer.Serialize(body) + _secretKey);
                byte[] hash = new SHA256Managed().ComputeHash(bytes);
                foreach (byte x in hash)
                    hashString += string.Format("{0:x2}", x);
                request.AddHeader("X-Checksum", "token " + hashString);
                request.AddHeader("X-Timestamp", timestampValue);
                request.AddHeader("X-AppKey", _apiKey);
                request.AddHeader("X-SessionToken", _base64SessionToken);

                return request;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: RequestHeader - " + e);
                throw;
            }
        }

        public Dictionary<string, object> makeRequest(string method, string endpoint, Dictionary<string, object> requestBody)
        {
            RestResponse response;
            RestRequest request;
            if (!hasSession())
                return new Dictionary<string, object>
                {
                    { "Success" , "" },
                    { "Status" , 500 },
                    { "Error" , "Session not generated. Please generate session." },
                };
            switch (method.ToUpper())
            {
                case "GET":
                    request = new RestRequest(endpoint, Method.Get);
                    break;
                case "POST":
                    request = new RestRequest(endpoint, Method.Post);
                    break;
                case "PUT":
                    request = new RestRequest(endpoint, Method.Put);
                    break;
                case "DELETE":
                    request = new RestRequest(endpoint, Method.Delete);
                    break;
                default:
                    return new Dictionary<string, object>()
                    {
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "Invalid Request Method - Must be GET, POST, PUT or DELETE" }
                    };
            }
            request.AddParameter("application/json", JsonSerializer.Serialize(requestBody), ParameterType.RequestBody);
            request = prepRequestHeader(requestBody, request);
            response = _client.Execute(request);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content);
        }

        public Dictionary<string, object> getCustomerDetail(string apiSession)
        {
            if (string.IsNullOrEmpty(apiSession))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "apiSession cannot be empty" }
                };
            else
            {
                var response = makeRequest("GET", "customerdetails", new Dictionary<string, object>()
                {
                    { "SessionToken", apiSession},
                    { "AppKey", _apiKey}
                });
                if (response.ContainsKey("Success"))
                {
                    object successObject;
                    if (response.TryGetValue("Success", out successObject) && successObject.ToString() != "")
                    {
                        var successOutput = JsonSerializer.Deserialize<Dictionary<string, object>>(successObject.ToString());
                        successOutput.Remove("session_token");
                        response.Remove("Success");
                        response.Add("Success", successOutput);
                    }
                }
                return response;
            }
        }

        public Dictionary<string, object> getDematHoldings()
        {
            var response = makeRequest("GET", "dematholdings", new Dictionary<string, object>() { });
            return response;
        }

        public Dictionary<string, object> getFunds()
        {
            var response = makeRequest("GET", "funds", new Dictionary<string, object>() { });
            return response;
        }

        public Dictionary<string, object> setFunds(string transactionType, string amount, string segment)
        {
            if (string.IsNullOrEmpty(transactionType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "transactionType cannot be empty" }
                };
            else if (!checkList(transactionTypeList, transactionType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "transactionType should be either 'debit' or 'credit'" }
                };
            else if (string.IsNullOrEmpty(amount))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "amount cannot be empty" }
                };
            else if (int.TryParse(amount, out int amountValue) && amountValue > 0)
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "amount should be more than 0" }
                };
            else if (string.IsNullOrEmpty(segment))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "segment cannot be empty" }
                };
            var response = makeRequest("POST", "funds", new Dictionary<string, object>()
                {
                    { "transaction_type", transactionType},
                    { "amount", amount},
                    { "segment", segment}
                });
            return response;
        }

        public Dictionary<string, object> getHistoricalData(string interval, string fromDate, string toDate, string stockCode, string exchangeCode, string productType, string expiryDate, string right, string strikePrice)
        {
            if (string.IsNullOrEmpty(interval))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "interval cannot be empty" }
                };
            else if (!checkList(intervalList, interval))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "interval should be either '1minute', '5minute', '30minute', or '1day'" }
                };
            else if (string.IsNullOrEmpty(fromDate))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "fromDate cannot be empty" }
                };
            else if (string.IsNullOrEmpty(toDate))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "toDate cannot be empty" }
                };
            else if (string.IsNullOrEmpty(stockCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "stockCode cannot be empty" }
                };
            else if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (exchangeCode.ToLower() == "nfo")
            {
                if (string.IsNullOrEmpty(productType))
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "productType cannot be empty for exchangeCode 'nfo'" }
                    };
                else if (!checkList(nfoProductTypeList, productType.ToLower()))
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "productType should be either 'futures', 'options', 'futureplus', or 'optionplus' for exchangeCode 'nfo'" }
                    };
                else if (string.IsNullOrEmpty(expiryDate))
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "expiryDate cannot be empty for exchangeCode 'nfo'" }
                    };
                else if (productType.ToLower() == "options" && string.IsNullOrEmpty(strikePrice))
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "strikePrice cannot be empty for productType 'options'" }
                    };
            }
            if (interval == "1minute")
                interval = "minute";
            else if (interval == "1day")
                interval = "day";
            var requestBody = new Dictionary<string, object>()
                {
                    { "interval", interval},
                    { "from_date", fromDate},
                    { "to_date", toDate},
                    { "stock_code", stockCode},
                    { "exchange_code", exchangeCode}
                };
            if (!string.IsNullOrEmpty(productType))
                requestBody.Add("product_type", productType);
            if (!string.IsNullOrEmpty(expiryDate))
                requestBody.Add("expiry_date", expiryDate);
            if (!string.IsNullOrEmpty(strikePrice))
                requestBody.Add("strike_price", strikePrice);
            if (!string.IsNullOrEmpty(right))
                requestBody.Add("right", right);
            var response = makeRequest("GET", "historicalcharts", requestBody);
            return response;
        }

        public Dictionary<string, object> addMargin(string exchangeCode, string productType, string stockCode, string coverQuantity, string settlementId, string addAmount, string marginAmount, string openQuantity, string categoryIndexPerStock, string expiryDate, string right, string contractTag, string strikePrice, string segmentCode)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (!string.IsNullOrEmpty(productType) && !checkList(productTypeList, productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType should be either 'futures', 'options', 'futureplus', 'optionplus', 'cash', 'eatm', or 'margin'" }
                };
            else if (!string.IsNullOrEmpty(right) && !checkList(rightList, right))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "right should be either 'call', 'put', or 'others'" }
                };
            var requestBody = new Dictionary<string, object>() { { "exchange_code", exchangeCode } };
            if (!string.IsNullOrEmpty(productType))
                requestBody.Add("product_type", productType);
            if (!string.IsNullOrEmpty(stockCode))
                requestBody.Add("stock_code", stockCode);
            if (!string.IsNullOrEmpty(coverQuantity))
                requestBody.Add("cover_quantity", coverQuantity);
            if (!string.IsNullOrEmpty(categoryIndexPerStock))
                requestBody.Add("category_index_per_stock", categoryIndexPerStock);
            if (!string.IsNullOrEmpty(contractTag))
                requestBody.Add("contract_tag", contractTag);
            if (!string.IsNullOrEmpty(marginAmount))
                requestBody.Add("margin_amount", marginAmount);
            if (!string.IsNullOrEmpty(expiryDate))
                requestBody.Add("expiry_date", expiryDate);
            if (!string.IsNullOrEmpty(right))
                requestBody.Add("right", right);
            if (!string.IsNullOrEmpty(strikePrice))
                requestBody.Add("strike_price", strikePrice);
            if (!string.IsNullOrEmpty(segmentCode))
                requestBody.Add("segment_code", segmentCode);
            if (!string.IsNullOrEmpty(settlementId))
                requestBody.Add("settlement_id", settlementId);
            if (!string.IsNullOrEmpty(addAmount))
                requestBody.Add("add_amount", addAmount);
            if (!string.IsNullOrEmpty(openQuantity))
                requestBody.Add("open_quantity", openQuantity);
            var response = makeRequest("POST", "margin", requestBody);
            return response;
        }

        public Dictionary<string, object> getMargin(string exchangeCode)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            var response = makeRequest("GET", "margin", new Dictionary<string, object>() { { "exchange_code", exchangeCode } });
            return response;
        }

        public Dictionary<string, object> placeOrder(string stockCode, string exchangeCode, string productType, string action, string orderType, string stoploss, string quantity, string price, string validity, string validityDate, string disclosedQuantity, string expiryDate, string right, string strikePrice, string userRemark, string orderTypeFresh, string orderRateFresh)
        {
            if (string.IsNullOrEmpty(stockCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "stockCode cannot be empty" }
                };
            else if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType cannot be empty" }
                };
            else if (!checkList(productTypeList, productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType should be either 'futures', 'options', 'futureplus', 'optionplus', 'cash', 'eatm', or 'margin'" }
                };
            else if (string.IsNullOrEmpty(action))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "action cannot be empty" }
                };
            else if (!checkList(actionList, action))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "action should be either 'buy', or 'sell'" }
                };
            else if (string.IsNullOrEmpty(orderType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderType cannot be empty" }
                };
            else if (!checkList(orderTypeList, orderType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderType should be either 'limit', 'market', or 'stoploss'" }
                };
            else if (string.IsNullOrEmpty(quantity))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "quantity cannot be empty" }
                };
            else if (string.IsNullOrEmpty(validity))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "validity cannot be empty" }
                };
            else if (!checkList(validityList, validity))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "validity should be either 'day', 'ioc', or 'vtc'" }
                };
            else if (!string.IsNullOrEmpty(right) && !checkList(rightList, right))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "right should be either 'call', 'put', or 'others'" }
                };
            var requestBody = new Dictionary<string, object> {
                {"stock_code", stockCode},
                {"exchange_code", exchangeCode},
                {"product", productType},
                {"action", action},
                {"order_type", orderType},
                {"quantity", quantity},
                {"price", price},
                {"validity", validity}
            };
            if (!string.IsNullOrEmpty(stoploss))
                requestBody.Add("stoploss", stoploss);
            if (!string.IsNullOrEmpty(validityDate))
                requestBody.Add("validity_date", validityDate);
            if (!string.IsNullOrEmpty(disclosedQuantity))
                requestBody.Add("disclosed_quantity", disclosedQuantity);
            if (!string.IsNullOrEmpty(expiryDate))
                requestBody.Add("expiry_date", expiryDate);
            if (!string.IsNullOrEmpty(right))
                requestBody.Add("right", right);
            if (!string.IsNullOrEmpty(strikePrice))
                requestBody.Add("strike_price", strikePrice);
            if (!string.IsNullOrEmpty(userRemark))
                requestBody.Add("user_remark", userRemark);
            if (!string.IsNullOrEmpty(orderTypeFresh))
                requestBody.Add("order_type_fresh", orderTypeFresh);
            if (!string.IsNullOrEmpty(orderRateFresh))
                requestBody.Add("order_rate_fresh", orderRateFresh);
            var response = makeRequest("POST", "order", requestBody);
            return response;
        }

        public Dictionary<string, object> getOrderDetail(string exchangeCode, string orderId)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(orderId))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderId cannot be empty" }
                };
            var response = makeRequest("GET", "order", new Dictionary<string, object>() {
                { "exchange_code", exchangeCode },
                { "order_id", orderId }
            });
            return response;
        }

        public Dictionary<string, object> getOrderList(string exchangeCode, string fromDate, string toDate)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(fromDate))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "fromDate cannot be empty" }
                };
            else if (string.IsNullOrEmpty(toDate))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "toDate cannot be empty" }
                };
            var response = makeRequest("GET", "order", new Dictionary<string, object>() {
                { "exchange_code", exchangeCode },
                { "from_date", fromDate },
                { "to_date", toDate }
            });
            return response;
        }

        public Dictionary<string, object> cancelOrder(string exchangeCode, string orderId)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(orderId))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderId cannot be empty" }
                };
            var response = makeRequest("DELETE", "order", new Dictionary<string, object>() {
                { "exchange_code", exchangeCode },
                { "order_id", orderId }
            });
            return response;
        }

        public Dictionary<string, object> modifyOrder(string orderId, string exchangeCode, string orderType, string stoploss, string quantity, string price, string validity, string disclosedQuantity, string validityDate)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(orderId))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderId cannot be empty" }
                };
            else if (!string.IsNullOrEmpty(orderType) && !checkList(orderTypeList, orderType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderType should be either 'limit', 'market', or 'stoploss'" }
                };
            else if (!string.IsNullOrEmpty(validity) && !checkList(validityList, validity))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "validity should be either 'day', 'ioc', or 'vtc'" }
                };
            var requestBody = new Dictionary<string, object> {
                {"order_id", orderId},
                {"exchange_code", exchangeCode}
            };
            if (!string.IsNullOrEmpty(orderType))
                requestBody.Add("order_type", orderType);
            if (!string.IsNullOrEmpty(stoploss))
                requestBody.Add("stoploss", stoploss);
            if (!string.IsNullOrEmpty(quantity))
                requestBody.Add("quantity", quantity);
            if (!string.IsNullOrEmpty(price))
                requestBody.Add("price", price);
            if (!string.IsNullOrEmpty(validity))
                requestBody.Add("validity", validity);
            if (!string.IsNullOrEmpty(disclosedQuantity))
                requestBody.Add("disclosed_quantity", disclosedQuantity);
            if (!string.IsNullOrEmpty(validityDate))
                requestBody.Add("validity_date", validityDate);
            var response = makeRequest("PUT", "order", requestBody);
            return response;
        }

        public Dictionary<string, object> getPortfolioHoldings(string exchangeCode, string fromDate, string toDate, string stockCode, string portfolioType)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            var requestBody = new Dictionary<string, object> { { "exchange_code", exchangeCode } };
            if (!string.IsNullOrEmpty(fromDate))
                requestBody.Add("from_date", fromDate);
            if (!string.IsNullOrEmpty(toDate))
                requestBody.Add("to_date", toDate);
            if (!string.IsNullOrEmpty(stockCode))
                requestBody.Add("stock_code", stockCode);
            if (!string.IsNullOrEmpty(portfolioType))
                requestBody.Add("portfolio_type", portfolioType);
            var response = makeRequest("GET", "portfolioholdings", requestBody);
            return response;
        }

        public Dictionary<string, object> getPortfolioPositions()
        {
            var response = makeRequest("GET", "portfoliopositions", new Dictionary<string, object>() { });
            return response;
        }

        public Dictionary<string, object> getQuotes(string stockCode, string exchangeCode, string expiryDate, string productType, string right, string strikePrice)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(stockCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "stockCode cannot be empty" }
                };
            else if (!string.IsNullOrEmpty(productType) && !checkList(productTypeList, productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType should be either 'futures', 'options', 'futureplus', 'optionplus', 'cash', 'eatm', or 'margin'" }
                };
            else if (!string.IsNullOrEmpty(right) && !checkList(rightList, right))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "right should be either 'call', 'put', or 'others'" }
                };
            var requestBody = new Dictionary<string, object> {
                {"stock_code", stockCode},
                {"exchange_code", exchangeCode}
            };
            if (!string.IsNullOrEmpty(expiryDate))
                requestBody.Add("expiry_date", expiryDate);
            if (!string.IsNullOrEmpty(productType))
                requestBody.Add("product_type", productType);
            if (!string.IsNullOrEmpty(right))
                requestBody.Add("right", right);
            if (!string.IsNullOrEmpty(strikePrice))
                requestBody.Add("strike_price", strikePrice);
            var response = makeRequest("GET", "quotes", requestBody);
            return response;
        }

        public Dictionary<string, object> getOptionChainQuotes(string stockCode, string exchangeCode, string expiryDate, string productType, string right, string strikePrice)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty it should be nfo" }
                };
            else if (string.IsNullOrEmpty(productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "Product-Type cannot be empty for Exchange-Code value as 'nfo'." }
                };
            else if (productType.ToLower() != "futures" && productType.ToLower() != "options")
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "Product-type should be either 'futures' or 'options' for Exchange-Code value as 'nfo'."}
                };
            else if (string.IsNullOrEmpty(stockCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "Stock-code cannot be empty." }
                };
            else if (!string.IsNullOrEmpty(right) && !checkList(rightList, right))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "right should be either 'call', 'put', or 'others'" }
                };
            else if (productType.ToLower() == "options")
            {
                if (string.IsNullOrEmpty(expiryDate) && string.IsNullOrEmpty(strikePrice) && string.IsNullOrEmpty(right))
                {
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "Atleast two inputs are required out of Expiry-Date, Right & Strike-Price. All three cannot be empty'." }
                    };
                }
                else if (string.IsNullOrEmpty(strikePrice) && string.IsNullOrEmpty(right) && !string.IsNullOrEmpty(expiryDate))
                {
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "Either Right or Strike-Price cannot be empty." }
                    };
                }
                else if (string.IsNullOrEmpty(expiryDate) && string.IsNullOrEmpty(right) && !string.IsNullOrEmpty(strikePrice))
                {
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "Either Expiry-Date or Right cannot be empty." }
                    };
                }
                else if (string.IsNullOrEmpty(expiryDate) && string.IsNullOrEmpty(strikePrice) && !string.IsNullOrEmpty(right))
                {
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error", "Either Expiry-Date or Strike-Price cannot be empty."}
                    };
                }
                else if (!string.IsNullOrEmpty(right) && right.ToLower() != "call" && right.ToLower() != "put" && right.ToLower() != "others")
                {
                    return new Dictionary<string, object>{
                        { "Success", ""},
                        { "Status", 500},
                        { "Error","Right should be either 'call', 'put', or 'others'."}
                    };
                }
            }
            var requestBody = new Dictionary<string, object> {
                {"stock_code", stockCode},
                {"exchange_code", exchangeCode}
            };
            if (!string.IsNullOrEmpty(expiryDate))
                requestBody.Add("expiry_date", expiryDate);
            if (!string.IsNullOrEmpty(productType))
                requestBody.Add("product_type", productType);
            if (!string.IsNullOrEmpty(right))
                requestBody.Add("right", right);
            if (!string.IsNullOrEmpty(strikePrice))
                requestBody.Add("strike_price", strikePrice);
            var response = makeRequest("GET", "optionchain", requestBody);
            return response;
        }

        public Dictionary<string, object> squareOff(string sourceFlag, string stockCode, string exchangeCode, string quantity, string price, string action, string orderType, string validity, string stoploss, string disclosedQuantity, string protectionPercentage, string settlementId, string marginAmount, string openQuantity, string coverQuantity, string productType, string expiryDate, string right, string strikePrice, string validityDate, string tradePassword, string aliasName)
        {
            var response = makeRequest("POST", "squareoff", new Dictionary<string, object> {
                {"source_flag", sourceFlag},
                {"stock_code", stockCode},
                {"exchange_code", exchangeCode},
                {"quantity", quantity},
                {"price", price},
                {"action", action},
                {"order_type", orderType},
                {"validity", validity},
                {"stoploss_price", stoploss},
                {"disclosed_quantity", disclosedQuantity},
                {"protection_percentage", protectionPercentage},
                {"settlement_id", settlementId},
                {"margin_amount", marginAmount},
                {"open_quantity", openQuantity},
                {"cover_quantity", coverQuantity},
                {"product_type", productType},
                {"expiry_date", expiryDate},
                {"right", right},
                {"strike_price", strikePrice},
                {"validity_date", validityDate},
                {"alias_name", aliasName},
                {"trade_password", tradePassword}
            });
            return response;
        }

        public Dictionary<string, object> getTradeList(string fromDate, string toDate, string exchangeCode, string productType, string action, string stockCode)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (!string.IsNullOrEmpty(productType) && !checkList(productTypeList, productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType should be either 'futures', 'options', 'futureplus', 'optionplus', 'cash', 'eatm', or 'margin'" }
                };
            else if (!string.IsNullOrEmpty(action) && !checkList(actionList, action))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "action should be either 'buy', or 'sell'" }
                };
            var requestBody = new Dictionary<string, object>() { { "exchange_code", exchangeCode } };
            if (!string.IsNullOrEmpty(fromDate))
                requestBody.Add("from_date", fromDate);
            if (!string.IsNullOrEmpty(toDate))
                requestBody.Add("to_date", toDate);
            if (!string.IsNullOrEmpty(productType))
                requestBody.Add("product_type", productType);
            if (!string.IsNullOrEmpty(action))
                requestBody.Add("action", action);
            if (!string.IsNullOrEmpty(stockCode))
                requestBody.Add("stock_code", stockCode);
            var response = makeRequest("GET", "trades", requestBody);
            return response;
        }

        public Dictionary<string, object> previewOrder(string stockCode, string exchangeCode, string productType, string orderType, string price, string action, string quantity, string expiryDate, string right, string strikePrice, string specialFlag, string stoploss, string orderRateFresh)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };

            if (string.IsNullOrEmpty(stockCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "stockCode cannot be empty" }
                };

            if (string.IsNullOrEmpty(productType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "productType cannot be empty" }
                };

            if (string.IsNullOrEmpty(orderType))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderType cannot be empty" }
                };

            if (string.IsNullOrEmpty(action))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "action cannot be empty" }
                };

            var requestBody = new Dictionary<string, object> {

                {"stock_code", stockCode},
                {"exchange_code",exchangeCode},
                {"product",productType },
                {"order_type",orderType},
                { "price",price},
                {"action",action},
                {"quantity",quantity },
                {"expiry_date",expiryDate},
                {"right", right},
                {"strike_price",strikePrice},
                {"specialflag", specialFlag},
                {"stoploss", stoploss },
                {"order_rate_fresh",orderRateFresh }
            };

            var response = makeRequest("GET", "preview_order", requestBody);
            return response;


        }
        public Dictionary<string, object> getTradeDetail(string exchangeCode, string orderId)
        {
            if (string.IsNullOrEmpty(exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode cannot be empty" }
                };
            else if (!checkList(exchangeCodeList, exchangeCode))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "exchangeCode should be either 'nse', or 'nfo'" }
                };
            else if (string.IsNullOrEmpty(orderId))
                return new Dictionary<string, object>{
                    { "Success", ""},
                    { "Status", 500},
                    { "Error", "orderId cannot be empty" }
                };
            var response = makeRequest("GET", "trades", new Dictionary<string, object>() {
                { "exchange_code", exchangeCode },
                { "order_id", orderId }
            });
            return response;
        }

        public Dictionary<string, object> getNames(string exchange, string stockCode)
        {
            Dictionary<string, object> nameMapper =
                       new Dictionary<string, object>();
            exchange = exchange.ToLower();
            stockCode = stockCode.ToUpper();
            Uri url;
            switch (exchange)
            {
                case "nse":
                    //Console.WriteLine("idar aaye");
                    url = new Uri("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/NSEScripMaster.txt");
                    break;
                case "bse":
                    url = new Uri("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/BSEScripMaster.txt");
                    break;
                case "cdnse":
                    url = new Uri("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/CDNSEScripMaster.txt");
                    break;
                case "fonse":
                    url = new Uri("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/FONSEScripMaster.txt");
                    break;
                default:
                    url = new Uri("https://traderweb.icicidirect.com/Content/File/txtFile/ScripFile/NSEScripMaster.txt");
                    break;
            }

            using (WebClient client = new WebClient())
            {
                using (Stream stream = client.OpenRead(url))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line = reader.ReadLine();

                        while ((line = reader.ReadLine()) != null)
                        {
                            //Console.WriteLine(line);
                            string[] words = line.Split(',');
                            //Console.WriteLine(words[1].Length);
                            string shortName = words[1].Substring(1);
                            shortName = shortName.Substring(0, shortName.Length - 1);

                            string exchangeCode = words[60].Substring(1);
                            exchangeCode = exchangeCode.Substring(0, exchangeCode.Length - 1);

                            string token = words[0].Substring(1);
                            token = token.Substring(0, token.Length - 1);

                            string Isec_Stock_Code = words[1].Substring(1);
                            Isec_Stock_Code = Isec_Stock_Code.Substring(0, Isec_Stock_Code.Length - 1);

                            string Company_Name = words[3].Substring(1);
                            Company_Name = Company_Name.Substring(0, Company_Name.Length - 1);

                            String Exchange_Stock_Code = words[60].Substring(1);
                            Exchange_Stock_Code = Exchange_Stock_Code.Substring(0, Exchange_Stock_Code.Length - 1);

                            if (shortName.Equals(stockCode))
                            {

                                nameMapper.Add("status ", "SUCCESS");
                                nameMapper.Add("Isec_Token ", token);
                                nameMapper.Add("Isec_Stock_Code ", Isec_Stock_Code);
                                nameMapper.Add("Company_Name ", Company_Name);
                                nameMapper.Add("Exchange_Stock_Code ", Exchange_Stock_Code);
                                nameMapper.Add("Exchange ", exchange);
                                nameMapper.Add("isec_token_level1 ", "4.1!" + token);
                                nameMapper.Add("isec_token_level2 ", "4.2!" + token);
                                return (nameMapper);

                            }
                            else if (exchangeCode.Equals(stockCode))
                            {
                                nameMapper.Add("status", "SUCCESS");
                                nameMapper.Add("Isec_Token ", token);
                                nameMapper.Add("Isec_Stock_Code ", Isec_Stock_Code);
                                nameMapper.Add("Company_Name ", Company_Name);
                                nameMapper.Add("Exchange_Stock_Code ", Exchange_Stock_Code);
                                nameMapper.Add("Exchange ", exchange);
                                nameMapper.Add("isec_token_level1 ", "4.1!" + token);
                                nameMapper.Add("isec_token_level2 ", "4.2!" + token);
                                return (nameMapper);
                            }

                        }
                    }
                }
            }
            nameMapper.Add("status code : ", "404");
            nameMapper.Add("response : ", "Data not found");
            return (nameMapper);

        }

    }

    public class SocketEventBreeze
    {
        private string _apiKey;
        private string _userId;
        private string _sessionToken;
        private Uri _hostname = new Uri("https://livestream.icicidirect.com/");
        private Uri _hostnameOrder = new Uri("https://livefeeds.icicidirect.com/");
        private Uri _hostnameOhlcv = new Uri("https://breezeapi.icicidirect.com");

        private SocketIO _socket = null;
        private SocketIO _socketOrder = null;
        private SocketIO _socketOhlcv = null;
        private bool _orderNotificationSubscribed = false;
        private Dictionary<string, Dictionary<string, string>> _tuxToUserValue;
        private Dictionary<string, string[]>[] _tokenScriptDictList;
        private string[] subscribedStocks = new string[0];
        HashSet<String> tokenlist = new HashSet<String>();

        public bool hasSession() { return !(_apiKey is null || _userId is null || _sessionToken is null); }

        public bool isConnected(bool orderflag, bool isOhlcv = false)
        {
            if (isOhlcv == true)
            {
                return _socketOhlcv.Connected;
            }
            if (orderflag == true)
            {
                return _socketOrder.Connected;
            }
            else
            {
                return _socket.Connected;
            }

        }

        public bool isOrderNotificationSubscribed() { return _orderNotificationSubscribed; }

        private static void Socket_OnReconnecting(object sender, int e)
        {

        }

        public void setOrderNotificationSubscription(bool orderNotificationSubscribed)
        {
            _orderNotificationSubscribed = orderNotificationSubscribed;
        }

        public void setSession(string userId, string sessionToken, Dictionary<string, string[]>[] tokenScriptDictList, bool debug, string apiKey = null)
        {
            try
            {
                if (userId is null || sessionToken is null || userId.Length <= 0 || sessionToken.Length <= 0)
                    throw new Exception("UserId or SessionToken is empty. BreezeConnect Object not created.");
                _apiKey = apiKey;
                _userId = userId;
                _sessionToken = sessionToken;
                _tokenScriptDictList = tokenScriptDictList;
                _tuxToUserValue = setTuxToUserValue();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private SocketIOOptions initiateSocketIOOptions(bool isOhlcv)
        {
            try
            {
                if (_userId is null || _sessionToken is null || _userId.Length <= 0 || _sessionToken.Length <= 0)
                    throw new Exception("Cannot initiate connection to server. UserId or SessionToken is empty.");
                var socketIOOptions = new SocketIOOptions();
                socketIOOptions.EIO = 4;
                if (isOhlcv == true)
                {
                    socketIOOptions.Path = "/ohlcvstream/";
                }
                socketIOOptions.Auth = new Dictionary<string, string> { { "user", _userId }, { "token", _sessionToken }, };
                socketIOOptions.ConnectionTimeout = TimeSpan.FromSeconds(10);
                socketIOOptions.Reconnection = true;
                //socketIOOptions.ReconnectionAttempts = 10;
                socketIOOptions.Transport = SocketIOClient.Transport.TransportProtocol.WebSocket;
                socketIOOptions.ExtraHeaders = new Dictionary<string, string> { { "User_Agent", "dotnet-socketio[client]/socket" } };
                return socketIOOptions;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        private Dictionary<string, Dictionary<string, string>> setTuxToUserValue()
        {
            var returnDictionary = new Dictionary<string, Dictionary<string, string>>();
            returnDictionary.Add("orderFlow", new Dictionary<string, string>()
            {
                {"B", "Buy"},
                {"S", "Sell"},
                {"N", "NA"}
            });
            returnDictionary.Add("limitMarketFlag", new Dictionary<string, string>()
            {
                {"L", "Limit"},
                {"M", "Market"},
                {"S", "StopLoss"}
            });
            returnDictionary.Add("orderType", new Dictionary<string, string>()
            {
                {"T", "Day"},
                {"I", "IoC"},
                {"V", "VTC"}
            });
            returnDictionary.Add("productType", new Dictionary<string, string>()
            {
                { "F", "Futures"},
                { "O", "Options"},
                { "P", "FuturePlus"},
                { "U", "FuturePlus_sltp"},
                { "I", "OptionPlus"},
                { "C", "Cash"},
                { "Y", "eATM"},
                { "B", "BTST"},
                { "M", "Margin"},
                { "T", "MarginPlus"}
            });
            returnDictionary.Add("orderStatus", new Dictionary<string, string>()
            {
                {"A", "All"},
                {"R", "Requested"},
                {"Q", "Queued"},
                {"O", "Ordered"},
                {"P", "Partially Executed"},
                {"E", "Executed"},
                {"J", "Rejected"},
                {"X", "Expired"},
                {"B", "Partially Executed And Expired"},
                {"D", "Partially Executed And Cancelled"},
                {"F", "Freezed"},
                {"C", "Cancelled"}
            });
            returnDictionary.Add("optionType", new Dictionary<string, string>()
            {
                { "C", "Call"},
                { "P", "Put"},
                { "*", "Others"}
            });
            return returnDictionary;
        }

        private Dictionary<string, object> getDataFromStockTokenValue(string stockTokenInput)
        {
            Dictionary<string, object> outputData = new Dictionary<string, object>();
            string exchangeType = stockTokenInput.Split('.')[0];
            string stockToken = stockTokenInput.Split('!')[1];
            Dictionary<string, string> exchangeCodeList = new Dictionary<string, string>()
            {
                {"1", "BSE"},
                {"4", "NSE"},
                {"13", "NDX"},
                {"6", "MCX"}
            };
            exchangeCodeList.TryGetValue(exchangeType, out string exchangeCodeName);
            string[] stockData = new string[0];
            if (exchangeCodeName == "")
                throw new Exception("Stock-Token cannot be found due to wrong exchange-code.");
            else if (exchangeCodeName.ToLower() == "bse")
            {
                _tokenScriptDictList[0].TryGetValue(stockToken, out stockData);
                if (stockData is null || stockData.Length != 2)
                    throw new Exception("Stock-Data does not exist in exchange-code BSE for Stock-Token " + stockTokenInput);
            }
            else if (exchangeCodeName.ToLower() == "nse")
            {
                _tokenScriptDictList[1].TryGetValue(stockToken, out stockData);
                if (stockData is null || stockData.Length != 2)
                {
                    _tokenScriptDictList[4].TryGetValue(stockToken, out stockData);
                    if (stockData is null || stockData.Length != 2)
                        throw new Exception("Stock-Data does not exist in both exchange-code NSE & NFO for Stock-Token " + stockTokenInput);
                    else
                        exchangeCodeName = "NFO";
                }
            }
            else if (exchangeCodeName.ToLower() == "ndx")
            {
                _tokenScriptDictList[2].TryGetValue(stockToken, out stockData);
                if (stockData is null || stockData.Length != 2)
                    throw new Exception("Stock-Data does not exist in exchange-code NDX for Stock-Token " + stockTokenInput);
            }
            else if (exchangeCodeName.ToLower() == "mcx")
            {
                _tokenScriptDictList[3].TryGetValue(stockToken, out stockData);
                if (stockData is null || stockData.Length != 2)
                    throw new Exception("Stock-Data does not exist in exchange-code MCX for Stock-Token " + stockTokenInput);
            }
            outputData.Add("stock_name", stockData[1]);
            if (exchangeCodeName.ToLower() != "nse" && exchangeCodeName.ToLower() != "bse")
            {
                string productType = stockData[0].Split('-')[0];
                if (productType.ToLower() == "fut")
                    outputData.Add("product_type", "Futures");
                else if (productType.ToLower() == "opt")
                    outputData.Add("product_type", "Options");
                string dateString = "";
                foreach (var date in new ArraySegment<string>(stockData[0].Split('-'), 2, 5))
                {
                    dateString += date + "-";
                }
                if (!outputData.ContainsKey("expiry_date")) outputData.Add("expiry_date", dateString.Remove(dateString.Length - 1, 1));
                if (stockData[0].Split('-').Length > 5)
                {
                    outputData.Add("strike_price", stockData[0].Split('-')[5]);
                    string right = stockData[0].Split('-')[6];
                    if (right.ToLower() == "pe")
                    {
                        outputData.Add("right", "Put");
                    }
                    else if (right.ToLower() == "ce")
                    {
                        outputData.Add("right", "Call");
                    }
                }
            }
            return outputData;
        }

        public async Task<SocketIO> Connect()
        {
            if (_socket == null)
            {
                _socket = new SocketIO(_hostname, initiateSocketIOOptions(false));
                await _socket.ConnectAsync();
                if (_socket.Disconnected)
                {
                    int count = 0;
                    while (_socket.Disconnected)
                    {
                        count += 1;
                        _socket.OnReconnectAttempt += Socket_OnReconnecting;
                    }
                    Console.WriteLine($"Reconnected after {count} attempts. to hosturl {_hostname}");
                }
                if (_socket.Connected) { Console.WriteLine("Socket-Id: " + _socket.Id); }
            }
            return _socket;
        }
        public async Task<SocketIO> ConnectForOrder()
        {
            if (_socketOrder == null)
            {
                _socketOrder = new SocketIO(_hostnameOrder, initiateSocketIOOptions(false));
                await _socketOrder.ConnectAsync();
                // Console.WriteLine("Connection done.........");
                if (_socketOrder.Disconnected)
                {
                    int count = 0;
                    while (_socketOrder.Disconnected)
                    {
                        count += 1;
                        _socketOrder.OnReconnectAttempt += Socket_OnReconnecting;
                    }
                    Console.WriteLine($"Reconnected after {count} attempts. to hosturl {_hostnameOrder}");
                }
                if (_socketOrder.Connected) { Console.WriteLine("Socket-Id: " + _socketOrder.Id); }
            }
            return _socketOrder;
        }

        public async Task<SocketIO> ConnectForOhlcv()
        {
            if (_socketOhlcv == null)
            {
                _socketOhlcv = new SocketIO(_hostnameOhlcv, initiateSocketIOOptions(true));
                await _socketOhlcv.ConnectAsync();
                // Console.WriteLine("Connection done.........");
                if (_socketOhlcv.Disconnected)
                {
                    int count = 0;
                    while (_socketOhlcv.Disconnected)
                    {
                        count += 1;
                        _socketOhlcv.OnReconnectAttempt += Socket_OnReconnecting;
                    }
                    Console.WriteLine($"Reconnected after {count} attempts. to hosturl {_hostnameOhlcv}");
                }
                if (_socketOhlcv.Connected) { Console.WriteLine("Socket-Id: " + _socketOhlcv.Id); }
            }
            return _socketOhlcv;
        }

        public SocketIO GetSocketIO(bool isOrder, bool isohlcv = false)
        {
            if (isOrder == false)
            {
                if (_socket.Connected)
                    return _socket;
                else
                    return null;
            }
            if (isOrder == true)
            {

                if (_socketOrder.Connected)
                    return _socketOrder;
                else
                    return null;
            }
            if (isohlcv == true)
            {
                if (_socketOhlcv.Connected)
                    return _socketOhlcv;
                else
                    return null;
            }
            return null;

        }

        public async Task rewatch()
        {
            Console.WriteLine("Rewatch successfull");
            foreach (string entry in tokenlist)
            {
                await _socket.EmitAsync("join", entry);
            }
        }

        public async Task watchOhlcv(string token, string channel)
        {
            try
            {
                await _socketOhlcv.EmitAsync("join", token);

                Console.WriteLine("Socket subscribed:" + token);
            }

            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task watchStrategy(String token)
        {
            await _socketOrder.EmitAsync("join", token);
        }

        public async Task watch(String[] stockList)
        {
            try
            {
                List<string> list = new List<string>(subscribedStocks);
                foreach (string stockName in stockList)
                {

                    await _socket.EmitAsync("join", stockName);

                    Console.WriteLine("Socket subscribed:" + stockName);
                    if (!list.Contains(stockName))
                        list.Add(stockName);
                    if (!tokenlist.Contains(stockName))
                        tokenlist.Add(stockName);

                }

                _socket.On("disconnect", response =>
                {
                    Console.WriteLine("Reconnection Triggered");
                });

                _socket.On("connect", response =>
                {
                    rewatch();
                });

                subscribedStocks = list.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task unwatchOhlcv(string token, string channel)
        {
            try
            {
                await _socketOhlcv.EmitAsync("leave", token);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task unWatchStrategy(String token)
        {
            await _socketOrder.EmitAsync("leave", token);
        }

        public async Task unWatch(String[] stockList)
        {
            try
            {
                List<string> list = new List<string>(subscribedStocks);
                foreach (string stockName in stockList)
                {
                    await _socket.EmitAsync("leave", stockName);
                    //await _socketOrder.EmitAsync("leave", stockName);
                    Console.WriteLine("Socket unsubscribed:" + stockName);
                    list.Remove(stockName);
                    if (tokenlist.Contains(stockName))
                    {
                        tokenlist.Remove(stockName);
                    }
                }
                subscribedStocks = list.ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string epoch2string(int epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((epoch + 19800)).ToString("ddd MMM dd HH:mm:ss yyyy");
        }

        public Dictionary<string, object> parseOhlcv(JsonElement data)
        {
            Dictionary<string, string> feedIntervalMap = new Dictionary<string, string>()
            {
                {"1MIN","1minute"},
                {"5MIN", "5minute" },
                {"30MIN", "30minute" },
                {"1SEC", "1second" }
            };

            Dictionary<string, string> channelIntervalMap = new Dictionary<string, string>()
            {
                {"1minute","1MIN"},
                {"5minute","5MIN"},
                {"30minute","30MIN"},
                {"1second","1SEC"}
            };

            if (data[0].ToString().Equals("NSE") || data[0].ToString().Equals("BSE"))
            {
                Dictionary<string, object> candleData = new Dictionary<string, object>()
                {
                    {"interval",feedIntervalMap[data[8].ToString()] },
                    {"exchange_code",data[0]},
                    {"stock_code",data[1] },
                    {"low",data[2]},
                    {"high",data[3]},
                    {"open",data[4]},
                    {"close",data[5]},
                    {"volume",data[6]},
                    {"datetime",data[7]}
                };
                return (candleData);
            }

            else if (data[0].ToString().Equals("NFO") || data[0].ToString().Equals("MCX") || data[0].ToString().Equals("NDX"))
            {
                if (data.GetArrayLength() == 13)
                {
                    Dictionary<string, object> candleData = new Dictionary<string, object>()
                    {
                        {"interval",feedIntervalMap[data[12].ToString()] },
                        {"exchange_code",data[0]},
                        {"stock_code",data[1] },
                        {"expiry_date",data[2]},
                        {"strike_price",data[3]},
                        {"right_type",data[4]},
                        {"low",data[5]},
                        {"high",data[6]},
                        {"open",data[7]},
                        {"close",data[8]},
                        {"volume",data[9]},
                        {"oi",data[10]},
                        {"datetime",data[11]}

                    };
                    return (candleData);
                }
                else
                {
                    Dictionary<string, object> candleData = new Dictionary<string, object>()
                    {
                        {"interval",feedIntervalMap[data[10].ToString()] },
                        {"exchange_code",data[0]},
                        {"stock_code",data[1] },
                        {"expiry_date",data[2]},
                        {"low",data[3]},
                        {"high",data[4]},
                        {"open",data[5]},
                        {"close",data[6]},
                        {"volume",data[7]},
                        {"oi",data[8]},
                        {"datetime",data[9]}


                    };
                    return (candleData);
                }
            }
            return (null);

        }

        public Object parseStrategy(JsonElement data)
        {
            if(data.GetArrayLength() == 19)
            {
                Dictionary<string, string> iclickData = new Dictionary<string, string>(){
                {"stock_name",data[0].ToString()},
                {"stock_code",data[1].ToString()},
                {"action_type",data[2].ToString()},
                {"expiry_date",data[3].ToString()},
                {"strike_price",data[4].ToString()},
                {"option_type",data[5].ToString()},
                {"stock_description",data[6].ToString()},
                {"recommended_price_and_date",data[7].ToString()},
                {"recommended_price_from",data[8].ToString()},
                {"recommended_price_to",data[9].ToString()},
                {"recommended_date",data[10].ToString()},
                {"target_price",data[11].ToString()},
                {"sltp_price",data[12].ToString()},
                {"part_profit_percentage",data[13].ToString()},
                {"profit_price",data[14].ToString()},
                {"exit_price",data[15].ToString()},
                {"recommended_update",data[16].ToString()},
                {"iclick_status",data[17].ToString()},
                {"subscription_type",data[18].ToString()},
              };
                return (iclickData);
            }
            else
            {
                Dictionary<string, string> strategyData = new Dictionary<string, string>(){
                {"strategy_date",data[0].ToString()},
                {"modification_date",data[1].ToString()},
                {"portfolio_id",data[2].ToString()},
                {"call_action",data[3].ToString()},
                {"portfolio_name",data[4].ToString()},
                {"exchange_code",data[5].ToString()},
                {"product_type",data[6].ToString()},
                {"underlying",data[8].ToString()},
                {"expiry_date",data[9].ToString() },
                {"option_type",data[11].ToString()},
                {"strike_price",data[12].ToString ()},
                {"action",data[13].ToString()},
                {"recommended_price_from",data[14].ToString()},
                {"recommended_price_to",data[15].ToString ()},
                {"minimum_lot_quantity",data[16].ToString()},
                {"last_traded_price",data[17].ToString() },
                {"best_bid_price",data[18].ToString()},
                {"best_offer_price",data[19].ToString()},
                {"last_traded_quantity",data[20].ToString()},
                {"target_price",data[21].ToString()},
                {"expected_profit_per_lot",data[22].ToString()},
                {"stop_loss_price",data[23].ToString()},
                {"expected_loss_per_lot",data[24].ToString()},
                {"total_margin",data[25].ToString()},
                {"leg_no",data[26].ToString()},
                {"status",data[27].ToString()}
              };
              return (strategyData);

            }
            
        }


        public Object parseData(JsonElement data)
        {
            var exchange = data[0].GetString().Split('!')[0].Split('.')[0];
            if (exchange.Equals("1"))
                exchange = "BSE";
            else if (exchange.Equals("4"))
            {
                if (data.GetArrayLength() == 21)
                    exchange = "NSE Equity";
                else if (data.GetArrayLength() == 23)
                    exchange = "NSE Futures & Options";
            }
            else if (exchange.Equals("13"))
                exchange = "NSE Currency";
            else if (exchange.Equals("6"))
            {
                exchange = "Commodity";
                return parseCommodityData(data, exchange);
            }

            if (data.GetArrayLength() == 21 || data.GetArrayLength() == 23)
                return parseExchangeQuoteData(data, exchange);
            else if (data.GetArrayLength() == 3)
                return parseMarketDepthData(data, exchange);
            else
                return null;
        }
        private Dictionary<string, object> parseExchangeQuoteData(JsonElement data, string exchange)
        {
            if (data.GetArrayLength() == 21 || data.GetArrayLength() == 23)
            {
                Dictionary<string, object> feedData = new Dictionary<string, object>()
                {
                    {"exchange", exchange},
                    {"quotes", "Quotes Data"},
                    {"symbol", data[0]},
                    {"open", data[1]},
                    {"last", data[2]},
                    {"high", data[3]},
                    {"low", data[4]},
                    {"change", data[5]},
                    {"bPrice", data[6]},
                    {"bQty", data[7]},
                    {"sPrice", data[8]},
                    {"sQty", data[9]},
                    {"ltq", data[10]},
                    {"avgPrice", data[11]},
                };
                if (data.GetArrayLength() == 21)
                {
                    feedData.Add("ttq", data[12]);
                    feedData.Add("totalBuyQt", data[13]);
                    feedData.Add("totalSellQ", data[14]);
                    feedData.Add("ttv", data[15]);
                    feedData.Add("trend", data[16]);
                    feedData.Add("lowerCktLm", data[17]);
                    feedData.Add("upperCktLm", data[18]);
                    feedData.Add("ltt", epoch2string(data[19].GetInt32())); // date
                    feedData.Add("close", data[20]);
                }
                else if (data.GetArrayLength() == 23)
                {
                    feedData.Add("OI", data[12]);
                    feedData.Add("CHNGOI", data[13]);
                    feedData.Add("ttq", data[14]);
                    feedData.Add("totalBuyQt", data[15]);
                    feedData.Add("totalSellQ", data[16]);
                    feedData.Add("ttv", data[17]);
                    feedData.Add("trend", data[18]);
                    feedData.Add("lowerCktLm", data[19]);
                    feedData.Add("upperCktLm", data[20]);
                    feedData.Add("ltt", epoch2string(data[21].GetInt32()));
                    feedData.Add("close", data[22]);
                }
                feedData.Append(getDataFromStockTokenValue(data[0].ToString()));
                return feedData;
            }
            else
                return new Dictionary<string, object>();
        }

        private Dictionary<string, object> parseMarketDepthData(JsonElement data, string exchange)
        {
            Dictionary<string, object>[] nestedMarketDepthDataArray = new Dictionary<string, object>[data[2].GetArrayLength()];
            for (int i = 0; i < data[2].GetArrayLength(); i++)
            {
                Dictionary<string, object> nestedMarketDepthData = new Dictionary<string, object>();
                if (exchange.Equals("BSE"))
                {
                    nestedMarketDepthData.Add("BestBuyRate-" + (i + 1).ToString(), data[2][i][0]);
                    nestedMarketDepthData.Add("BestBuyQty-" + (i + 1).ToString(), data[2][i][1]);
                    nestedMarketDepthData.Add("BestSellRate-" + (i + 1).ToString(), data[2][i][2]);
                    nestedMarketDepthData.Add("BestSellQty-" + (i + 1).ToString(), data[2][i][3]);
                }
                else
                {
                    nestedMarketDepthData.Add("BestBuyRate-" + (i + 1).ToString(), data[2][i][0]);
                    nestedMarketDepthData.Add("BestBuyQty-" + (i + 1).ToString(), data[2][i][1]);
                    nestedMarketDepthData.Add("BuyNoOfOrders-" + (i + 1).ToString(), data[2][i][2]);
                    nestedMarketDepthData.Add("BuyFlag-" + (i + 1).ToString(), data[2][i][3]);
                    nestedMarketDepthData.Add("BestSellRate-" + (i + 1).ToString(), data[2][i][4]);
                    nestedMarketDepthData.Add("BestSellQty-" + (i + 1).ToString(), data[2][i][5]);
                    nestedMarketDepthData.Add("SellNoOfOrders-" + (i + 1).ToString(), data[2][i][6]);
                    nestedMarketDepthData.Add("SellFlag-" + (i + 1).ToString(), data[2][i][7]);
                }
                nestedMarketDepthDataArray[i] = nestedMarketDepthData;
            }
            Dictionary<string, object> feedData = new Dictionary<string, object>()
            {
                {"exchange", exchange},
                {"quotes", "Market Depth"},
                {"symbol", data[0]},
                {"time", epoch2string(data[1].GetInt32())}, //date
                {"depth",nestedMarketDepthDataArray}
            };
            feedData.Append(getDataFromStockTokenValue(data[0].ToString()));
            return feedData;
        }

        private Dictionary<string, object> parseCommodityData(JsonElement data, string exchange)
        {
            Dictionary<string, object> feedData = new Dictionary<string, object>()
            {
                {"exchange", exchange},
                {"symbol", data[0]},
                {"AndiOPVolume", data[1]},
                {"Reserved", data[2]},
                {"IndexFlag", data[3]},
                {"ttq", data[4]},
                {"last", data[5]},
                {"ltq", data[6]},
                {"ltt", epoch2string(data[7].GetInt32())},
                {"AvgTradedPrice", data[8]},
                {"TotalBuyQnt", data[9]},
                {"TotalSellQnt", data[10]},
                {"ReservedStr", data[11]},
                {"ClosePrice", data[12]},
                {"OpenPrice", data[13]},
                {"HighPrice", data[14]},
                {"LowPrice", data[15]},
                {"ReservedShort", data[16]},
                {"CurrOpenInterest", data[17]},
                {"TotalTrades", data[18]},
                {"HightestPriceEver", data[19]},
                {"LowestPriceEver", data[20]},
                {"TotalTradedValue", data[21]}
            };
            int marketDepthIndex = 0;
            for (int i = 22; i < data.GetArrayLength(); i++)
            {
                feedData.Add("Quantity-" + marketDepthIndex.ToString(), data[i][0]);
                feedData.Add("OrderPrice-" + marketDepthIndex.ToString(), data[i][1]);
                feedData.Add("TotalOrders-" + marketDepthIndex.ToString(), data[i][2]);
                feedData.Add("Reserved-" + marketDepthIndex.ToString(), data[i][3]);
                feedData.Add("SellQuantity-" + marketDepthIndex.ToString(), data[i][4]);
                feedData.Add("SellOrderPrice-" + marketDepthIndex.ToString(), data[i][5]);
                feedData.Add("SellTotalOrders-" + marketDepthIndex.ToString(), data[i][6]);
                feedData.Add("SellReserved-" + marketDepthIndex.ToString(), data[i][7]);
                marketDepthIndex++;
            }
            feedData.Append(getDataFromStockTokenValue(data[0].ToString()));
            return feedData;
        }

        public Dictionary<string, object> parseOrderData(JsonElement data)
        {
            try
            {
                Dictionary<string, object> orderData = new Dictionary<string, object>();
                orderData = Miscellaneous.tryAddToDictionary("sourceNumber", data[0], orderData);                            //Source Number
                orderData = Miscellaneous.tryAddToDictionary("group", data[1], orderData);                                   //Group
                orderData = Miscellaneous.tryAddToDictionary("userId", data[2], orderData);                                  //User_id
                orderData = Miscellaneous.tryAddToDictionary("key", data[3], orderData);                                     //Key
                orderData = Miscellaneous.tryAddToDictionary("messageLength", data[4], orderData);                           //Message Length
                orderData = Miscellaneous.tryAddToDictionary("requestType", data[5], orderData);                             //Request Type
                orderData = Miscellaneous.tryAddToDictionary("messageSequence", data[6], orderData);                         //Message Sequence
                orderData = Miscellaneous.tryAddToDictionary("messageDate", data[7], orderData);                             //Date
                orderData = Miscellaneous.tryAddToDictionary("messageTime", data[8], orderData);                             //Time
                orderData = Miscellaneous.tryAddToDictionary("messageCategory", data[9], orderData);                         //Message Category
                orderData = Miscellaneous.tryAddToDictionary("messagePriority", data[10], orderData);                        //Priority
                orderData = Miscellaneous.tryAddToDictionary("messageType", data[11], orderData);                            //Message Type
                orderData = Miscellaneous.tryAddToDictionary("orderMatchAccount", data[12], orderData);                      //Order Match Account
                orderData = Miscellaneous.tryAddToDictionary("orderExchangeCode", data[13], orderData);                      //Exchange Code
                if (data[11].ToString().Contains("4") || data[11].ToString().Contains("5"))
                {
                    orderData = Miscellaneous.tryAddToDictionary("stockCode", data[14], orderData);                          //Stock Code
                    _tuxToUserValue.TryGetValue("orderFlow", out Dictionary<string, string> orderFlowDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderFlow", orderFlowDict.TryGetValue(data[15].ToString().ToUpper(), out string orderFlow) ? orderFlow : "", orderData);                          // Order Flow
                    _tuxToUserValue.TryGetValue("limitMarketFlag", out Dictionary<string, string> limitMarketFlagDict);
                    orderData = Miscellaneous.tryAddToDictionary("limitMarketFlag", limitMarketFlagDict.TryGetValue(data[16].ToString().ToUpper(), out string limitMarketFlag) ? limitMarketFlag : "", orderData);                    //Limit Market Flag
                    _tuxToUserValue.TryGetValue("orderType", out Dictionary<string, string> orderTypeDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderType", orderTypeDict.TryGetValue(data[17].ToString().ToUpper(), out string orderType) ? orderType : "", orderData);                          //OrderType
                    orderData = Miscellaneous.tryAddToDictionary("orderLimitRate", data[18], orderData);                     //Limit Rate
                    _tuxToUserValue.TryGetValue("productType", out Dictionary<string, string> productTypeDict);
                    orderData = Miscellaneous.tryAddToDictionary("productType", productTypeDict.TryGetValue(data[19].ToString().ToUpper(), out string productType) ? productType : "", orderData);                        //Product Type
                    _tuxToUserValue.TryGetValue("orderStatus", out Dictionary<string, string> orderStatusDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderStatus", orderStatusDict.TryGetValue(data[20].ToString().ToUpper(), out string orderStatus) ? orderStatus : "", orderData);                        // Order Status
                    orderData = Miscellaneous.tryAddToDictionary("orderDate", data[21], orderData);                          //Order  Date
                    orderData = Miscellaneous.tryAddToDictionary("orderTradeDate", data[22], orderData);                     //Trade Date
                    orderData = Miscellaneous.tryAddToDictionary("orderReference", data[23], orderData);                     //Order Reference
                    orderData = Miscellaneous.tryAddToDictionary("orderQuantity", data[24], orderData);                      //Order Quantity
                    orderData = Miscellaneous.tryAddToDictionary("openQuantity", data[25], orderData);                       //Open Quantity
                    orderData = Miscellaneous.tryAddToDictionary("orderExecutedQuantity", data[26], orderData);              //Order Executed Quantity
                    orderData = Miscellaneous.tryAddToDictionary("cancelledQuantity", data[27], orderData);                  //Cancelled Quantity
                    orderData = Miscellaneous.tryAddToDictionary("expiredQuantity", data[28], orderData);                    //Expired Quantity
                    orderData = Miscellaneous.tryAddToDictionary("orderDisclosedQuantity", data[29], orderData);             // Order Disclosed Quantity
                    orderData = Miscellaneous.tryAddToDictionary("orderStopLossTrigger", data[30], orderData);               //Order Stop Loss Triger
                    orderData = Miscellaneous.tryAddToDictionary("orderSquareFlag", data[31], orderData);                    //Order Square Flag
                    orderData = Miscellaneous.tryAddToDictionary("orderAmountBlocked", data[32], orderData);                 // Order Amount Blocked
                    orderData = Miscellaneous.tryAddToDictionary("orderPipeId", data[33], orderData);                        //Order PipeId
                    orderData = Miscellaneous.tryAddToDictionary("channel", data[34], orderData);                            //Channel
                    orderData = Miscellaneous.tryAddToDictionary("exchangeSegmentCode", data[35], orderData);                //Exchange Segment Code
                    orderData = Miscellaneous.tryAddToDictionary("exchangeSegmentSettlement", data[36], orderData);          //Exchange Segment Settlement 
                    orderData = Miscellaneous.tryAddToDictionary("segmentDescription", data[37], orderData);                 //Segment Description
                    orderData = Miscellaneous.tryAddToDictionary("marginSquareOffMode", data[38], orderData);                //Margin Square Off Mode
                    orderData = Miscellaneous.tryAddToDictionary("orderValidDate", data[40], orderData);                     //Order Valid Date
                    orderData = Miscellaneous.tryAddToDictionary("orderMessageCharacter", data[41], orderData);              //Order Message Character
                    orderData = Miscellaneous.tryAddToDictionary("averageExecutedRate", data[42], orderData);                //Average Exited Rate
                    orderData = Miscellaneous.tryAddToDictionary("orderPriceImprovementFlag", data[43], orderData);          //Order Price Flag
                    orderData = Miscellaneous.tryAddToDictionary("orderMBCFlag", data[44], orderData);                       //Order MBC Flag
                    orderData = Miscellaneous.tryAddToDictionary("orderLimitOffset", data[45], orderData);                   //Order Limit Offset
                    orderData = Miscellaneous.tryAddToDictionary("systemPartnerCode", data[46], orderData);                  //System Partner Code
                }
                else if (data[11].ToString().Contains("6") || data[11].ToString().Contains("7"))
                {
                    orderData = Miscellaneous.tryAddToDictionary("stockCode", data[14], orderData);                         //stockCode
                    _tuxToUserValue.TryGetValue("productType", out Dictionary<string, string> productTypeDict);
                    orderData = Miscellaneous.tryAddToDictionary("productType", productTypeDict.TryGetValue(data[15].ToString(), out string productType) ? productType : "", orderData);                        //Product Type
                    _tuxToUserValue.TryGetValue("optionType", out Dictionary<string, string> optionTypeDict);
                    orderData = Miscellaneous.tryAddToDictionary("optionType", optionTypeDict.TryGetValue(data[16].ToString(), out string optionType) ? optionType : "", orderData);                          //Option Type
                    orderData = Miscellaneous.tryAddToDictionary("exerciseType", data[17], orderData);                       //Exercise Type
                    orderData = Miscellaneous.tryAddToDictionary("strikePrice", data[18], orderData);                        //Strike Price
                    orderData = Miscellaneous.tryAddToDictionary("expiryDate", data[19], orderData);                         //Expiry Date
                    orderData = Miscellaneous.tryAddToDictionary("orderValidDate", data[20], orderData);                     //Order Valid Date
                    _tuxToUserValue.TryGetValue("orderFlow", out Dictionary<string, string> orderFlowDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderFlow", orderFlowDict.TryGetValue(data[21].ToString(), out string orderFlow) ? orderFlow : "", orderData);                          //Order  Flow
                    _tuxToUserValue.TryGetValue("limitMarketFlag", out Dictionary<string, string> limitMarketFlagDict);
                    orderData = Miscellaneous.tryAddToDictionary("limitMarketFlag", limitMarketFlagDict.TryGetValue(data[22].ToString(), out string limitMarketFlag) ? limitMarketFlag : "", orderData);                    //Limit Market Flag
                    _tuxToUserValue.TryGetValue("orderType", out Dictionary<string, string> orderTypeDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderType", orderTypeDict.TryGetValue(data[23].ToString(), out string orderType) ? orderType : "", orderData);                          //Order Type
                    orderData = Miscellaneous.tryAddToDictionary("limitRate", data[24], orderData);                          //Limit Rate
                    _tuxToUserValue.TryGetValue("orderStatus", out Dictionary<string, string> orderStatusDict);
                    orderData = Miscellaneous.tryAddToDictionary("orderStatus", orderStatusDict.TryGetValue(data[25].ToString(), out string orderStatus) ? orderStatus : "", orderData);                        //Order Status
                    orderData = Miscellaneous.tryAddToDictionary("orderReference", data[26], orderData);                     //Order Reference
                    orderData = Miscellaneous.tryAddToDictionary("orderTotalQuantity", data[27], orderData);                 //Order Total Quantity
                    orderData = Miscellaneous.tryAddToDictionary("executedQuantity", data[28], orderData);                   //Executed Quantity
                    orderData = Miscellaneous.tryAddToDictionary("cancelledQuantity", data[29], orderData);                  //Cancelled Quantity
                    orderData = Miscellaneous.tryAddToDictionary("expiredQuantity", data[30], orderData);                    //Expired Quantity
                    orderData = Miscellaneous.tryAddToDictionary("stopLossTrigger", data[31], orderData);                    //Stop Loss Trigger
                    orderData = Miscellaneous.tryAddToDictionary("specialFlag", data[32], orderData);                        //Special Flag
                    orderData = Miscellaneous.tryAddToDictionary("pipeId", data[33], orderData);                             //PipeId
                    orderData = Miscellaneous.tryAddToDictionary("channel", data[34], orderData);                            //Channel
                    orderData = Miscellaneous.tryAddToDictionary("modificationOrCancelFlag", data[35], orderData);           //Modification or Cancel Flag
                    orderData = Miscellaneous.tryAddToDictionary("tradeDate", data[36], orderData);                          //Trade Date
                    orderData = Miscellaneous.tryAddToDictionary("acknowledgeNumber", data[37], orderData);                  //Acknowledgement Number
                    orderData = Miscellaneous.tryAddToDictionary("stopLossOrderReference", data[37], orderData);             //Stop Loss Order Reference
                    orderData = Miscellaneous.tryAddToDictionary("totalAmountBlocked", data[38], orderData);                 // Total Amount Blocked
                    orderData = Miscellaneous.tryAddToDictionary("averageExecutedRate", data[39], orderData);                //Average Executed Rate
                    orderData = Miscellaneous.tryAddToDictionary("cancelFlag", data[40], orderData);                         //Cancel Flag
                    orderData = Miscellaneous.tryAddToDictionary("squareOffMarket", data[41], orderData);                    //SquareOff Market
                    orderData = Miscellaneous.tryAddToDictionary("quickExitFlag", data[42], orderData);                      //Quick Exit Flag
                    orderData = Miscellaneous.tryAddToDictionary("stopValidTillDateFlag", data[43], orderData);              //Stop Valid till Date Flag
                    orderData = Miscellaneous.tryAddToDictionary("priceImprovementFlag", data[44], orderData);               //Price Improvement Flag
                    orderData = Miscellaneous.tryAddToDictionary("conversionImprovementFlag", data[45], orderData);          //Conversion Improvement Flag
                    orderData = Miscellaneous.tryAddToDictionary("trailUpdateCondition", data[45], orderData);               //Trail Update Condition
                    orderData = Miscellaneous.tryAddToDictionary("systemPartnerCode", data[46], orderData);                  //System Partner Code
                }
                return orderData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public static class Extensions
    {
        public static void Append<K, V>(this Dictionary<K, V> first, Dictionary<K, V> second)
        {
            foreach (KeyValuePair<K, V> item in second)
            {
                first[item.Key] = item.Value;
            }
        }
    }

    public class Miscellaneous
    {
        public static Dictionary<string, object> tryAddToDictionary(string keyName, object valueObject, Dictionary<string, object> dictionaryObject)
        {
            if (!dictionaryObject.ContainsKey(keyName)) dictionaryObject.Add(keyName, valueObject);
            return dictionaryObject;
        }

        public static Dictionary<string, string> tryAddToDictionary(string keyName, string valueObject, Dictionary<string, string> dictionaryObject)
        {
            if (!dictionaryObject.ContainsKey(keyName)) dictionaryObject.Add(keyName, valueObject);
            return dictionaryObject;
        }

        public static Dictionary<string, string[]> tryAddToDictionary(string keyName, string[] valueObject, Dictionary<string, string[]> dictionaryObject)
        {
            if (!dictionaryObject.ContainsKey(keyName)) dictionaryObject.Add(keyName, valueObject);
            return dictionaryObject;
        }
    }
}