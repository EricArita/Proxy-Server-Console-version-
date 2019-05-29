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
using System.IO.Compression;
using System.Net.Security;

namespace TestProxyOnConsole
{
    public class ProxyServer
    {
        public enum Mode : int
        {
            Forward = 0,
            Undefined = 1
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
           
            IPEndPoint IPEndpoint = null;
            byte[] buffer = new byte[1024];

            if (ipv4Addr != "")
                IPEndpoint = CreateEndPoint(ipv4Addr);

            if (IPEndpoint != null)
            {
                //Console.WriteLine("Step 0");   

                started = true;
                server.Bind(IPEndpoint);
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

            Console.WriteLine("Server shutdown...");

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

        public void CloseAllSockets()
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
                Console.WriteLine("All clients are disconnected from server");
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
            CloseAllSockets();
        }

        private string ServerDateTime()
        {
            string dayOfWeek = "", month = "";
            string day = DateTime.UtcNow.Day.ToString();
            string year = DateTime.UtcNow.Year.ToString();
            string hour = DateTime.UtcNow.Hour <= 9 ? "0" + DateTime.UtcNow.Hour.ToString() : DateTime.UtcNow.Hour.ToString();
            string minute = DateTime.UtcNow.Minute <= 9 ? "0" + DateTime.UtcNow.Minute.ToString() : DateTime.UtcNow.Minute.ToString();
            string second = DateTime.UtcNow.Second <= 9 ? "0" + DateTime.UtcNow.Second.ToString() : DateTime.UtcNow.Second.ToString();

            switch (DateTime.UtcNow.DayOfWeek.ToString())
            {
                case "Monday":
                    dayOfWeek = "Mon";
                    break;
                case "Tuesday":
                    dayOfWeek = "Tue";
                    break;
                case "Wednesday":
                    dayOfWeek = "Wed";
                    break;
                case "Thusday":
                    dayOfWeek = "Thu";
                    break;
                case "Friday":
                    dayOfWeek = "Fri";
                    break;
                case "Saturday":
                    dayOfWeek = "Sat";
                    break;
                case "Sunday":
                    dayOfWeek = "Sun";
                    break;
            }

            switch (DateTime.UtcNow.Month)
            {
                case 1:
                    month = "January";
                    break;
                case 2:
                    month = "February";
                    break;
                case 3:
                    month = "Match";
                    break;
                case 4:
                    month = "April";
                    break;
                case 5:
                    month = "May";
                    break;
                case 6:
                    month = "June";
                    break;
                case 7:
                    month = "July";
                    break;
                case 8:
                    month = "August";
                    break;
                case 9:
                    month = "September";
                    break;
                case 10:
                    month = "October";
                    break;
                case 11:
                    month = "November";
                    break;
                case 12:
                    month = "December";
                    break;
            }

            return dayOfWeek + ", " + day + " " + month + " " + year + " " + hour + ":" + minute + ":" + second + " GMT";
        }

        private void Response403Forbidden(Socket client)
        {
            string html = "<!DOCTYPE HTML>\r\n<html>\r\n<body>\r\n<h1 align='center'>403 Forbidden</h1>\r\n</body>\r\n</html>";
            string resHeader = "";
            
            resHeader += "HTTP/1.1 403 Forbidden\r\n";
            resHeader += "Date: " + ServerDateTime() + "\r\n";
            resHeader += "Server: Apache\r\n";
            resHeader += "Content-Encoding:\r\n";
            resHeader += "Content-Length: " + html.Length + "\r\n";
            resHeader += "Content-Type: text/html; charset = iso - 8859 - 1\r\n\r\n";
            resHeader += html;

            //byte[] uncompressedBytes = Encoding.ASCII.GetBytes(resHeader);
            //byte[] encode = null;
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    using (GZipStream gs = new GZipStream(ms, CompressionMode.Compress))
            //    {
            //        gs.Write(uncompressedBytes, 0, uncompressedBytes.Length);
            //    }

            //    encode = ms.ToArray();
            //}         

            byte[] encode = Encoding.ASCII.GetBytes(resHeader);
            client.Send(encode, 0, encode.Length, SocketFlags.None);
        }

        private void AcceptClient(IAsyncResult ar)
        {
            Socket client = null;
            bool allow;

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
          
            if (!autoAllow)
            { 
                Console.WriteLine("\n[IN] Connection " + remoteAddress + ":" + remotePort + "\nDo you want to allow this connection?");
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
                KillSocket(client);
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
                Console.WriteLine("[DISCONNECT] Client disconnected from server");
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

            #region Check blacklist

            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (requestHeader.Contains(ConfigurationManager.AppSettings[key].ToString()))
                {
                    Response403Forbidden(client);
                    return;
                }
            }
            #endregion

            Request req = new Request(requestHeader); 
            //bool sslHandlerStarted = false;
                

            //Console.WriteLine("Step 3");

            Tunnel tunnel = new Tunnel(Tunnel.Mode.HTTPs, httpMode, httpsMode, ref client);
            tunnel.CreateMinimalTunnel(req);

            if (tunnel.sslRead && httpsMode == Mode.Forward) //Handle HTTPS normal
            {
               //Console.WriteLine("Step 4");
                 tunnel.SendHTTPS(client);               
                 return;
            }
            else if (httpMode == Mode.Forward) //Handle HTTP normal
            {
                tunnel.SendHTTP(req, client);               
                return;               
            }          

            Array.Clear(buffer, 0, buffer.Length);

            //try {
            //    if (client.Connected && !sslHandlerStarted)
            //        client.BeginReceive(obj.buffer, 0, obj.buffer.Length, SocketFlags.None, new AsyncCallback(ReadPackets), obj);
            //}
            //catch (Exception e)
            //{
            //    KillSocket(client, !stopping);
            //    Console.WriteLine("Client aborted session!" + Environment.NewLine + e.Message);
            //}
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
    }
}
