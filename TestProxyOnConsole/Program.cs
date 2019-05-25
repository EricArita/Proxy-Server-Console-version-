using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Collections;
using System.Configuration;

namespace TestProxyOnConsole
{
    class Program
    {
        public static void startProxyServer()
        {
            string cmd = Console.ReadLine();

            if (cmd == "start")
            {
                ProxyServer server = null;
                int port = 8888;
                string ip = "any";
                int pendingConnectionLimit = 50;
                bool isStarted = false;

                if (server == null)
                {
                    server = new ProxyServer(ip, port, pendingConnectionLimit);
                }
                else if (!isStarted && server != null)
                {
                    server.Setup(ip, port, pendingConnectionLimit);
                }

                if (server == null)
                {
                    server.SetMode(ProxyServer.Mode.forward, "http");
                    server.SetMode(ProxyServer.Mode.forward, "https");
                }

                server.StartServer();
                isStarted = true;
            }
        }

        static void Main(string[] args)
        {
            Task t = new Task(new Action(() =>
            {
                string cmd = Console.ReadLine();

                if (cmd == "start")
                {
                    ProxyServer server = null;
                    int port = 8888;
                    string ip = "any";
                    int pendingConnectionLimit = 0;
                    bool isStarted = false;

                    if (server == null)
                    {
                        server = new ProxyServer(ip, port, pendingConnectionLimit);
                    }
                    else if (!isStarted && server != null)
                    {
                        server.Setup(ip, port, pendingConnectionLimit);
                    }

                    if (server == null)
                    {
                        server.SetMode(ProxyServer.Mode.forward, "http");
                        server.SetMode(ProxyServer.Mode.forward, "https");
                    }

                    server.StartServer();
                    isStarted = true;
                }
            }));

            t.Start();
            Task.WaitAll(t);

            Thread.Sleep(100000000);
            Console.WriteLine("Server is started!");

        }
    }
}


    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        StockItems PS = new StockItems();
    //        List<string> Pizzas = (List<string>)PS.GetAvailableStocks();
    //    }
    //}

    //public class StockItems
    //{
    //    private const string CacheKey = "availableStocks";

    //    public IEnumerable GetAvailableStocks()
    //    {
    //        ObjectCache cache = MemoryCache.Default;

    //        if (cache.Contains(CacheKey))
    //            return (IEnumerable)cache.Get(CacheKey);
    //        else
    //        {
    //            IEnumerable availableStocks = this.GetDefaultStocks();

    //            // Store data in the cache    
    //            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
    //            cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(1.0);
    //            cache.Add(CacheKey, availableStocks, cacheItemPolicy);

    //            return availableStocks;
    //        }
    //    }
    //    public IEnumerable GetDefaultStocks()
    //    {
    //        return new List<string>() { "Pen", "Pencil", "Eraser" };
    //    }
    //}
 

