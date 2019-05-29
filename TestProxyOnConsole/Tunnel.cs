using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace TestProxyOnConsole
{
    public class Tunnel
    {
        #region Proxy Tunnel

        public Mode Protocol { get; private set; }
        string _host;
        ProxyServer.Mode http;
        ProxyServer.Mode https;
        Socket client;

        public bool TunnelDestroyed { get; private set; } = false;

        public bool sslRead = false;

        public enum Mode : int
        {
            HTTP = 1,
            HTTPs = 2
        }

        public Tunnel(Mode protocolMode, ProxyServer.Mode httpMode, ProxyServer.Mode httpsMode, ref Socket client)
        {
            Protocol = protocolMode;
            http = httpMode;
            https = httpsMode;
            this.client = client;
        }

        public string GetHost()
        {
            return _host;
        }

        public void CreateMinimalTunnel(Request req)
        {
            string host = req.headers["Host"];
            _host = host;

            if (req.method == "CONNECT")
            {
                host = host.Replace(":443", string.Empty);
                Protocol = Mode.HTTPs;
                sslRead = true;
                _host = host;
            }
            else
            {          
                Protocol = Mode.HTTP;
                sslRead = false;
                _host = host;
            }
        }

        public string FormatRequest(Request req)
        {
            if (TunnelDestroyed) return null;

            if (_host == null)
            {
                //Generate404();
                return null;
            }

            string toSend = req.Deserialize();
            List<String> lines = toSend.Split('\n').ToList();
            lines[0] = lines[0].Replace("http://", String.Empty);
            lines[0] = lines[0].Replace("https://", String.Empty);
            lines[0] = lines[0].Replace(_host, String.Empty);
            toSend = "";

            Console.WriteLine("Yupppppppppp");

            foreach (string line in lines)
            {
                toSend += line + "\n";
            }

            return toSend;
        }

        private struct RawObj
        {
            public byte[] data;
            public Socket client;
            public Socket bridge;
        }

        private struct RawSSLObj
        {
            public RawObj rawData;
            public Request request;
            public string fullText;
        }

        private void ForwardRawHTTP(IAsyncResult ar)
        {
            try
            {
                //Console.WriteLine("Step 6");
                RawObj data = (RawObj)ar.AsyncState;

                if (data.client == null || data.bridge == null) return;

                int bytesRead = data.bridge.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //Console.WriteLine("Step 7");
                    byte[] toSend = new byte[bytesRead];
                    Array.Copy(data.data, toSend, bytesRead);

                    data.client.Send(toSend, 0, toSend.Length, SocketFlags.None);
                    Array.Clear(toSend, 0, bytesRead);
                }
                else
                {
                    if (data.client != null)
                    {
                        data.client.Close();
                        data.client.Dispose();
                        data.client = null;
                    }

                    if (data.bridge != null)
                    {
                        data.bridge.Close();
                        data.bridge.Dispose();
                        data.bridge = null;
                    }

                    return;
                }

                data.data = new byte[2048];
                data.bridge.BeginReceive(data.data, 0, 2048, SocketFlags.None, new AsyncCallback(ForwardRawHTTP), data);
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Forawrd Raw HTTP failed: {e.ToString()}");
            }
        }

        private void GenerateVerify(Socket clientSocket = null)
        {
            string verifyResponse = "HTTP/1.1 200 OK Tunnel created\r\n\r\n";
            byte[] resp = Encoding.ASCII.GetBytes(verifyResponse);

            if (clientSocket != null)
            {
                //Console.WriteLine("Step 8");
                clientSocket.Send(resp, 0, resp.Length, SocketFlags.None);
                return;
            }
        }

        private IPAddress GetIPOfHost(string hostname)
        {
            if (!IPAddress.TryParse(hostname, out IPAddress address))
            {
                IPAddress[] ips = Dns.GetHostAddresses(hostname);
                return (ips.Length > 0) ? ips[0] : null;
            }
            else
                return address;
        }

        public void SendHTTP(Request req, Socket browser)
        {
            try
            {
                string code = FormatRequest(req);
                Socket bridge = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = GetIPOfHost(req.headers["Host"]);

                if (ip == null)
                {
                    if (browser != null)
                    {
                        browser.Close();
                        browser.Dispose();
                        browser = null;
                    }

                    return;
                }

                bridge.Connect(ip, 80);
                RawObj ro = new RawObj() { client = browser, data = new byte[2048], bridge = bridge };
                bridge.Send(Encoding.ASCII.GetBytes(code));
                bridge.BeginReceive(ro.data, 0, 2048, SocketFlags.None, new AsyncCallback(ForwardRawHTTP), ro);            
            }
            catch (SocketException socketError)
            {
                //console.Debug($"Failed to tunnel http traffic for {r.headers["Host"]}: {socketError.ToString()}");
                Console.WriteLine(socketError.ToString());
            }
        }

        private void ReadBrowser(IAsyncResult ar)
        {
            try
            {               
                RawSSLObj rso = (RawSSLObj)ar.AsyncState;
                if (rso.rawData.client == null || rso.rawData.bridge == null) return;
                int bytesRead = rso.rawData.client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    byte[] req = new byte[bytesRead];
                    Array.Copy(rso.rawData.data, req, bytesRead);
                    rso.rawData.bridge.Send(req, 0, bytesRead, SocketFlags.None);
                    Array.Clear(req, 0, bytesRead);
                }
                else
                {
                    if (rso.rawData.client != null)
                    {
                        rso.rawData.client.Close();
                        rso.rawData.client.Dispose();
                        rso.rawData.client = null;
                    }
                    if (rso.rawData.bridge != null)
                    {
                        rso.rawData.bridge.Close();
                        rso.rawData.bridge.Dispose();
                        rso.rawData.bridge = null;
                    }

                    return;
                }

                //Console.WriteLine("Read success!");
                rso.rawData.data = new byte[2048];
                rso.rawData.client.BeginReceive(rso.rawData.data, 0, 2048, SocketFlags.None, new AsyncCallback(ReadBrowser), rso);
            }
            catch (Exception)
            {
                //console.Debug($"Failed to read raw http from browser: {ex.ToString()}");
            }
        }

        public void SendHTTPS(Socket browser)
        {
            //if (https == ProxyServer.Mode.MITM) return;

            try
            {
                Socket bridge = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = GetIPOfHost(_host);

                if (ip == null)
                {
                    if (browser != null)
                    {
                        browser.Close();
                        browser.Dispose();
                        browser = null;
                    }

                    return;
                }

                //Console.WriteLine("Step 5");

                bridge.Connect(ip, 443);
                RawSSLObj rso = new RawSSLObj() { fullText = "", request = null, rawData = new RawObj { data = new byte[2048], client = browser, bridge = bridge } };
                RawObj ro = new RawObj() { data = new byte[2048], bridge = bridge, client = browser };
                bridge.BeginReceive(ro.data, 0, 2048, SocketFlags.None, new AsyncCallback(ForwardRawHTTP), ro);
                browser.BeginReceive(rso.rawData.data, 0, 2048, SocketFlags.None, new AsyncCallback(ReadBrowser), rso);
                GenerateVerify(browser);
            }
            catch (SocketException socketError)
            {
                Console.WriteLine(socketError.ToString());
            }
        }

        #endregion
    }
}