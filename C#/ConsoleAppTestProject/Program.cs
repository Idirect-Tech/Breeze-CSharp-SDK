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
                BreezeConnect breeze = new BreezeConnect("appkey");
                breeze.generateSessionAsPerVersion("secret key", "api session");
                ////////////////////////WebSocket////////////////////////
                //var responseObject = await breeze.wsConnectAsync();
                Console.WriteLine(JsonSerializer.Serialize(breeze.getFunds()));
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("4.1!2885")));
                //breeze.ticker((data) =>
                //{
                //    Console.WriteLine("Ticker Data:" + JsonSerializer.Serialize(data));
                //});
                //Console.WriteLine(JsonSerializer.Serialize(responseObject));
                
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("6.1!235761")));
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("6.1!231993")));
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("4.1!49937")));
                //Console.WriteLine(JsonSerializer.Serialize(await breeze.subscribeFeedsAsync("13.1!2614")));
                
                //////////////////////WebSocket End//////////////////////
                Console.WriteLine(JsonSerializer.Serialize(breeze.getMargin("NSE")));
                ///////////////////////////API///////////////////////////
                //Console.WriteLine(JsonSerializer.Serialize(breeze.getCustomerDetail("API_Session")));
                //Console.WriteLine(JsonSerializer.Serialize(breeze.getFunds()));
                //Console.WriteLine(JsonSerializer.Serialize(breeze.getDematHoldings()));
                //Console.WriteLine(JsonSerializer.Serialize(breeze.getCustomerDetail("1644683")));
                
               // Console.WriteLine(JsonSerializer.Serialize)
                //Console.WriteLine(JsonSerializer.Serialize(breeze.getDematHoldings()));
                
                /////////////////////////API End////////////////////////
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
