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
                    int pendingConnectionLimit = 5;
                    bool isStarted = false;

                    if (server == null)
                    {
                        server = new ProxyServer(ip, port, pendingConnectionLimit);
                    }
                    else if (!isStarted && server != null)
                    {
                        server.Setup(ip, port, pendingConnectionLimit);
                    }

                    //if (server == null)
                    //{
                    //    server.SetMode(ProxyServer.Mode.forward, "http");
                    //    server.SetMode(ProxyServer.Mode.forward, "https");
                    //}

                    server.StartServer();
                    isStarted = true;
                }
            }));

            t.Start();
            Task.WaitAll(t);

            Thread.Sleep(100000000);

        }
    }
}
 