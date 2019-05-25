using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace TestProxyOnConsole
{
    public class ProxyServer : IDisposable
    {
        //ISettings Implementation

        public void LoadSettings(KeyValuePair<string, string> kvp)
        {
            string key = kvp.Key.ToLower();
            string value = kvp.Value.ToLower();

            if (key == "auto_allow") autoAllow = (value == "true") ? true : false;
            if (key == "http_mode") SetMode(StringToMode(value), "http");
            if (key == "https_mode") SetMode(StringToMode(value), "https");
        }

        public void WriteSettings(System.Xml.XmlWriter xml)
        {
            xml.WriteStartElement("settings_start");
            xml.WriteElementString("auto_allow", (autoAllow) ? "true" : "false");
            xml.WriteElementString("http_mode", ModeToString(httpMode));
            xml.WriteElementString("https_mode", ModeToString(httpsMode));
            xml.WriteEndElement();
        }

        //IDisposable implementation

        bool disposed = false;
        //SafeFileHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                //handle.Dispose();

                if (started)
                {
                    StopServer();
                    server.Dispose();
                }
     
                clientList = null;
            }

            disposed = true;
        }

        //Proxy Server

        public enum Mode : int
        {
            forward = 0,
            MITM = 1,
            Undefined = 2
        }

        Socket server;
        string ipv4Addr;
        int port;
        int pclimit;
        List<Socket> clientList = new List<Socket>();
        bool stopping = false;
        bool started = false;
        Mode httpMode;
        Mode httpsMode;

        public bool autoAllow = true;
        public bool autoClean = false;

        struct ReadObj
        {
            public Socket s;
            public byte[] buffer;
            public Request request;
        }

        #region Public methods

        public ProxyServer(string ipAddress, int portNumber, int pendingLimit)
        {
            ipv4Addr = ipAddress;
            port = portNumber;
            pclimit = pendingLimit;
        }

        public void Setup(string ipAddress, int portNumber, int pendingLimit)
        {
            ipv4Addr = ipAddress;
            port = portNumber;
            pclimit = pendingLimit;
        }

        public void StartServer()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           
            IPEndPoint ep = null;
            byte[] buffer = new byte[1024];

            if (ipv4Addr != "")
                ep = CreateEndPoint(ipv4Addr);

            if (ep != null)
            {
                //Console.WriteLine("Step 0");   

                started = true;
                server.Bind(ep);
                server.Listen(pclimit);
                server.BeginAccept(new AsyncCallback(AcceptClient), null);
            }
        }

        public void StopServer()
        {
            stopping = true;

            foreach (Socket socket in clientList)
            {
                KillSocket(socket, false);
            }

            clientList.Clear();

            Console.WriteLine("Server shutdown Ok");

            if (started)
            {
                if (server.Connected) server.Shutdown(SocketShutdown.Both);
                server.Close();
                server.Dispose();
            }

            Console.WriteLine("Server Stopped!");

            stopping = false;
            started = false;
        }

        public void KillSocket(Socket client, bool autoRemove = true)
        {
            if (autoRemove && clientList != null) clientList.Remove(client);

            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Disconnect(false);
            }
            catch (Exception)
            {
                Console.WriteLine("Killsocket failed!");
            }

            client.Close();
            client.Dispose();
        }

        public void CleanSockets()
        {
            List<Socket> copy = CopyList(clientList);
            bool result = true;

            foreach (Socket socket in copy)
            {
                try
                {
                    KillSocket(socket);
                }
                catch (Exception)
                {
                    Console.WriteLine("Clean Sockets failed!");
                    result = false;
                }
            }

            if (result)
            {
                Console.WriteLine("All clients disconnected from server");
            }
            else
            {
                Console.WriteLine("Some clients failed to disconnect from server!");
            }

            Array.Clear(copy.ToArray(), 0, copy.Count);
        }

        public void SetMode(Mode mode, string protocol)
        {
            if (protocol == "http") httpMode = mode;
            if (protocol == "https") httpsMode = mode;
        }

        public Mode GetMode(string protocolName)
        {
            protocolName = protocolName.ToLower();

            if (protocolName == "http") return httpMode;
            else if (protocolName == "https") return httpsMode;
            else return Mode.Undefined;
        }

        public void PrintModes()
        {
            Console.WriteLine("==Proxy Server Protocol Modes==");
            Console.WriteLine("HTTP: " + ModeToString(httpMode));
            Console.WriteLine("HTTPs: " + ModeToString(httpsMode));
            Console.WriteLine("");
        }

        #endregion

        #region Private medthods

        public List<Socket> CopyList(List<Socket> input)
        {
            var result = new List<Socket>();

            foreach (Socket item in input)
            {
                result.Add(item);
            }

            return result;
        }

        private void AutoClean(object sender, EventArgs e)
        {
            CleanSockets();
        }

        private void AcceptClient(IAsyncResult ar)
        {
            Socket client = null;

            try
            {
                client = server.EndAccept(ar);
            }
            catch (Exception)
            {
                return;
            }

            IPEndPoint client_ep = (IPEndPoint)client.RemoteEndPoint;
            string remoteAddress = client_ep.Address.ToString();
            string remotePort = client_ep.Port.ToString();
            //Console.Write(remoteAddress + " " + remotePort);

            //TODO: Implement block command -> keep the server and existing connections alive, but drop new connections

            bool allow;

            if (!autoAllow)
            { 
                Console.WriteLine("\n[IN] Connection " + remoteAddress + ":" + remotePort + "\nDo you want to allow connection?");
                string answer = Console.ReadLine();

                allow = answer == "yes" ? true : false;
            }
            else
                allow = true;

            if (allow)
            {
                //Console.WriteLine("Step 1");
                clientList.Add(client);

                ReadObj obj = new ReadObj
                {
                    buffer = new byte[1024],
                    s = client
                };

                client.BeginReceive(obj.buffer, 0, obj.buffer.Length, SocketFlags.None, new AsyncCallback(ReadPackets), obj);
            }
            else
            {
                KillSocket(client, !stopping);
                Console.WriteLine("[REJECT] " + remoteAddress + ":" + remotePort);
            }

            if (!stopping) server.BeginAccept(new AsyncCallback(AcceptClient), null);
        }

        private void ReadPackets(IAsyncResult ar)
        {
            ReadObj obj = (ReadObj)ar.AsyncState;
            Socket client = obj.s;
            byte[] buffer = obj.buffer;
            int read = -1;

            try
            {
                //Console.WriteLine("Step 2");
                read = client.EndReceive(ar);     
            }
            catch (Exception)
            {
                KillSocket(client, !stopping);
                Console.WriteLine("[DISCONNECT] Client Disconnected from server");
                return;
            }

            if (read == 0)
            {
                try
                {
                    if (client.Connected)
                    {
                        client.BeginReceive(obj.buffer, 0, obj.buffer.Length, SocketFlags.None, new AsyncCallback(ReadPackets), obj);
                    }                    
                }
                catch (Exception e)
                {
                    KillSocket(client, !stopping);
                    Console.WriteLine("Client aborted session!" + Environment.NewLine + e.Message);
                }

                return;
            }

            string requestHeader = Encoding.ASCII.GetString(buffer, 0, read);
            //Console.WriteLine(requestHeader);

            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                
                if (requestHeader.Contains(ConfigurationManager.AppSettings[key].ToString()))
                {                   
                    string ForbiddenHtmlPageFilePath = Directory.GetCurrentDirectory() + @"\403Forbidden.html";
                    Console.WriteLine(ForbiddenHtmlPageFilePath);

                    if (File.Exists(ForbiddenHtmlPageFilePath))
                    {
                        //Console.WriteLine(ConfigurationManager.AppSettings[key].ToString());
                        Console.WriteLine(ForbiddenHtmlPageFilePath);
                        Process.Start(ForbiddenHtmlPageFilePath);
                    }

                    return;                 
                }
            }
             
         
            Request req;
            bool sslHandlerStarted = false;

            if (obj.request != null)
            {
                if (obj.request.notEnded)
                {
                    string des = obj.request.full;
                    des += requestHeader;
                    req = new Request(des);
                }
                else req = new Request(requestHeader);
            }
            else
                req = new Request(requestHeader);

            //Console.WriteLine("Step 3");
            //Console.WriteLine(requestHeader);

            if (!req.notEnded && !req.bogus)
            {
                Tunnel t = new Tunnel(Tunnel.Mode.HTTP, httpMode, httpsMode, ref client);
                t.CreateMinimalTunnel(req);

                if (t.Forbidden)
                {
                    //Console.WriteLine("Success forbidden");
                    return;
                }

                if (t.sslRead && httpsMode == Mode.forward) //Handle HTTPS normal
                {
                    //Console.WriteLine("I'm HTTPS");
                    //Console.WriteLine(requestHeader);
                    //Console.WriteLine("Step 4");
                    t.InitHTTPS(client);
                  
                    return;
                }
                else if (httpMode == Mode.forward) //Handle HTTP normal
                {
                    //Console.WriteLine("I'm NOT HTTPS");
                    //Console.WriteLine(requestHeader); 
                    t.SendHTTP(req, client);
                   
                    return;
                }
            }
            else if (req.notEnded) obj.request = req;


            Array.Clear(buffer, 0, buffer.Length);

            try {
                if (client.Connected && !sslHandlerStarted)
                    client.BeginReceive(obj.buffer, 0, obj.buffer.Length, SocketFlags.None, new AsyncCallback(ReadPackets), obj);
            }
            catch (Exception e)
            {
                KillSocket(client, !stopping);
                Console.WriteLine("Client aborted session!" + Environment.NewLine + e.Message);
            }
        }

        private IPEndPoint CreateEndPoint(string ep_addr)
        {
            IPEndPoint result;

            switch (ep_addr)
            {
                case "loopback":
                    result = new IPEndPoint(IPAddress.Loopback, port);
                    break;
                case "any":
                    result = new IPEndPoint(IPAddress.Any, port);
                    break;
                case "localhost":
                    result = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                    break;
                default:
                    result = new IPEndPoint(IPAddress.Parse(ipv4Addr), port);
                    break;
            }

            return result;
        }

        #endregion

        #region Public static methods

        public static Mode StringToMode(string input)
        {
            input = input.ToLower();

            if (input == "mitm" || input == "man-in-the-middle") return Mode.MITM;
            else if (input == "forward" || input == "normal") return Mode.forward;
            return Mode.Undefined;
        }

        public static string ModeToString(Mode mode)
        {
            if (mode == Mode.forward) return "forward";
            else if (mode == Mode.MITM) return "mitm";
            else return "undefined";
        }

        #endregion
    }
}
