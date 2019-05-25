using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace TestProxyOnConsole
{
    public class Tunnel : IDisposable
    {
        bool disposed = false;

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
                _host = null;
                client = null;
                TunnelDestroyed = true;
            }

            disposed = true;
        }

        #region Proxy Tunnel

        public Mode Protocol { get; private set; }
        string _host;
        public bool Forbidden { get; private set; }
        ProxyServer.Mode http = ProxyServer.Mode.MITM;
        ProxyServer.Mode https = ProxyServer.Mode.MITM;
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
            Forbidden = false;
        }

        /*public static void Send(string data, Mode Protocol, Request r = null, NetworkStream targetHttp = null, VSslHandler targetHttps = null)
        {
            //ConMod.Debug("Send string");
            BISend(r, targetHttp, targetHttps, Protocol;
        }

        private static void BISend(Request req, NetworkStream ns, VSslHandler vSsl, Mode Protocol)
        {
            Task getPage = new Task(new Action(() => {

                //if (ctx.mitmHttp.started) ctx.mitmHttp.DumpRequest(r);

                string hostString = req.headers["Host"];
                string target = req.target.Replace(hostString, string.Empty);

                if (Protocol == Tunnel.Mode.HTTPs)
                    hostString = "https://" + hostString + target;
                else
                    hostString = "http://" + hostString + target;

                HttpClientHandler handler = new HttpClientHandler() { UseProxy = false, Proxy = null };
                HttpClient client = new HttpClient(handler);
                HttpRequestMessage hrm = new HttpRequestMessage
                {
                    Method = new HttpMethod(req.method),
                    RequestUri = new Uri(hostString)
                };

                foreach (KeyValuePair<string, string> kvp in req.headers.Items)
                {
                    hrm.Headers.Add(kvp.Key, kvp.Value);
                }

                if (req.htmlBody != null) hrm.Content = new StringContent(r.htmlBody);

                client.SendAsync(hrm).ContinueWith(responseTask => {

                    try
                    {
                        HttpResponseMessage resp = responseTask.Result;
                        byte[] content = new byte[0];
                        string strContent = "";
                        int statusCode = 0;
                        string statusDescription = "";
                        string version = "";
                        VDictionary headers = new VDictionary();

                        Task getContent = new Task(() =>
                        {

                            content = resp.Content.ReadAsByteArrayAsync().Result;

                            foreach (KeyValuePair<string, IEnumerable<string>> x in resp.Content.Headers)
                            {
                                string name = x.Key;
                                //if (name == "Content-Length") ctx.ConMod.Debug("Got content length");
                                string value = "";

                                foreach (string val in x.Value)
                                {
                                    value += val + ";";
                                }

                                value = value.Substring(0, value.Length - 1); // do not get the last semicolon
                                headers.Add(name, value);
                            }

                            ctx.ConMod.Debug("Headers in content" + resp.Content.Headers.Count());

                            strContent = Encoding.ASCII.GetString(content);

                        });

                        Task getHeaders = new Task(() =>
                        {

                            foreach (KeyValuePair<string, IEnumerable<string>> x in resp.Headers)
                            {
                                string name = x.Key;
                                string value = "";
                                foreach (string val in x.Value)
                                {
                                    value += val + ";";
                                }

                                value = value.Substring(0, value.Length - 1);
                                headers.Add(name, value);
                            }

                        });

                        // Example: HTTP/1.1 200 OK
                        Task getRest = new Task(() =>
                        {
                            statusCode = (int)resp.StatusCode;
                            statusDescription = resp.ReasonPhrase;
                            version = "HTTP/" + resp.Version.ToString();
                        });

                        getContent.Start();
                        getHeaders.Start();
                        getRest.Start();

                        Task.WaitAll(getContent, getHeaders, getRest);

                        Response _r = new Response(statusCode, statusDescription, version, headers, strContent, content, ctx.ConMod, ctx.mitmHttp);
                        _r.SetManager(ctx.vf);
                        _r.BindFilter("resp_mime", "mime_white_list");
                        _r.BindFilter("resp_mime_block", "mime_skip_list");
                        _r.CheckMimeAndSetBody();

                        if (ctx.mitmHttp.started)
                        {
                            string _target = r.target;
                            if (_target.Contains("?")) _target = _target.Substring(0, _target.IndexOf("?"));
                            ctx.mitmHttp.DumpResponse(_r, _target);
                        }

                        //ConMod.Debug("Before sending to client");
                        if (Protocol == Tunnel.Mode.HTTPs) _r.Deserialize(null, r, vSsl);
                        else _r.Deserialize(ns, r);
                    }
                    catch (Exception)
                    {
                        //ctx.ConMod.Debug("Error: " + ex.ToString() + "\r\nStackTrace:\r\n" + ex.StackTrace);
                        //ctx.ConMod.Debug($"On resource: {r.target}");
                    }

                });

            }));

            getPage.Start();
        }*/

        public string GetHost()
        {
            return _host;
        }

        public void CreateMinimalTunnel(Request req)
        {
            string host = req.headers["Host"];
            _host = host;
            //Console.WriteLine(host);

            if (req.method == "CONNECT")
            {
                host = host.Replace(":443", string.Empty);
                Protocol = Mode.HTTP;
                sslRead = true;
                _host = host;
                GenerateVerify();
            }
            else
            {          
                Protocol = Mode.HTTP;
                sslRead = false;
                _host = host;
            }
        }

        private void GenerateVerify(Socket clientSocket = null)
        {
            string verifyResponse = "HTTP/1.1 200 OK Tunnel Created\r\nTimestamp: " + DateTime.Now + "\r\nProxy-Agent: ah101\r\n\r\n";
            byte[] resp = Encoding.ASCII.GetBytes(verifyResponse);

            if (clientSocket != null)
            {
                //Console.WriteLine("Step 8");
                clientSocket.Send(resp, 0, resp.Length, SocketFlags.None);
                return;
            }

            if (https == ProxyServer.Mode.MITM) client.Send(resp, 0, resp.Length, SocketFlags.None);
            //console.Debug("verify request sent!");
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
                    //string text = Encoding.ASCII.GetString(data.data);
                    //Console.WriteLine(text);
                    Array.Copy(data.data, toSend, bytesRead);
                    data.client.Send(toSend, 0, bytesRead, SocketFlags.None);
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
            catch (Exception)
            {
                //console.Debug($"Forawrd RAW HTTP failed: {ex.ToString()}");
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
                bridge.BeginReceive(ro.data, 0, 2048, SocketFlags.None, new AsyncCallback(ForwardRawHTTP), ro);
                bridge.Send(Encoding.ASCII.GetBytes(code));
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

                rso.rawData.data = new byte[2048];
                rso.rawData.client.BeginReceive(rso.rawData.data, 0, 2048, SocketFlags.None, new AsyncCallback(ReadBrowser), rso);
            }
            catch (Exception)
            {
                //console.Debug($"Failed to read raw http from browser: {ex.ToString()}");
            }
        }

        public void InitHTTPS(Socket browser)
        {
            if (https == ProxyServer.Mode.MITM) return;

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