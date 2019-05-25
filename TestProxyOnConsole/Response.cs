using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


//namespace TestProxyOnConsole
//{
//    public class Response
//    {
//        //IDisposable Implementation

//        bool disposed = false;

//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (disposed) return;
//            if (disposing)
//            {
//                FullText = null;
//                Array.Clear(FullBytes, 0, FullBytes.Length);
//                FullBytes = null;
//                version = null;
//                statusCode = 0;
//                httpMessage = null;
//                headers.Clear();
//                headers = null;
//                Array.Clear(body, 0, body.Length);
//                bodyText = null;
//            }

//            disposed = true;
//        }

//        //public string PushBindInfo()
//        //{
//        //    string info = "";

//        //    foreach (KeyValuePair<string, object> kvp in filterNames)
//        //    {
//        //        string part2 = kvp.Value.ToString();
//        //        info += kvp.Key + ":" + part2 + ";";
//        //    }

//        //    if (info.Length > 0) info = info.Substring(0, info.Length - 1);

//        //    return info;
//        //}

//        //public void PullBindInfo(string info)
//        //{
//        //    if (info == "") return;
//        //    String[] kvp = info.Split(';');
//        //    foreach (String pairs in kvp)
//        //    {
//        //        string[] kvp2 = pairs.Split(':');
//        //        string level = kvp2[1].ToString();
//        //        string name = kvp2[0];
//        //        filterNames.Add(name, level);
//        //    }
//        //}

//        //public bool BindFilter(string validFilterName, object input)
//        //{
//        //    string op = (string)input;
//        //    if (op != "mime_white_list" && op != "mime_skip_list") return false;
//        //    filterNames.Add(validFilterName, op);
//        //    return true;
//        //}

//        //public bool SearchFilter(string sMethod, object searchParam, string input)
//        //{
//        //    string p = (string)searchParam;
//        //    string targetFilterName = "";
//        //    foreach (KeyValuePair<string, object> pair in filterNames)
//        //    {
//        //        string comp = (string)pair.Value;
//        //        if (comp == p)
//        //        {
//        //            targetFilterName = pair.Key;
//        //            break;
//        //        }
//        //    }

//        //    if (targetFilterName == "")
//        //    {
//        //        return true; // if target filter is not found output the text, perhaps there is no filter for a specific object
//        //    }

//        //    if (sMethod == "and")
//        //    {
//        //        return Manager.RunAllCompareAnd(targetFilterName, input);
//        //    }
//        //    else if (sMethod == "or")
//        //    {
//        //        return Manager.RunAllCompareOr(targetFilterName, input);
//        //    }
//        //    else
//        //    {
//        //        console.WriteLine("[ERROR] Invalid SearchFilter option sMethod", console.GetIntercativeGroup());
//        //        return true;
//        //    }
//        //}

//        //public bool UnBindFilter(string validFilterName)
//        //{
//        //    if (!FilterName.ContainsKey(validFilterName)) return false;
//        //    FilterName.Remove(validFilterName);
//        //    return true;
//        //}

//        //public void BindList()
//        //{
//        //    WriteLine("=========Start Of bind list=========");
//        //    foreach (KeyValuePair<string, object> kvp in filterNames)
//        //    {
//        //        string ll = (string)kvp.Value;
//        //        WriteLine(kvp.Key + ":\t" + ll);
//        //    }
//        //    WriteLine("==========End Of bind list==========");
//        //}

//        //public void SetManager(VFilter fman)
//        //{
//        //    Manager = fman;
//        //}

//        //Main response parser class

//        public string FullText { get; private set; } = "";
//        public byte[] FullBytes { get; private set; }
//        public string version = "";
//        public int statusCode = 0;
//        public string httpMessage = "";
//        public VDictionary headers = new VDictionary();
//        public byte[] body = new byte[2048];
//        public string bodyText = "";
//        public bool notEnded = false;
//        public bool bogus = false;
//        public bool skip = false;

//        public Response(int statusCode, string httpMessage, string version, VDictionary headers, string body, byte[] fullBytes)
//        {
//            this.statusCode = statusCode;
//            this.httpMessage = httpMessage;
//            this.version = version;
//            this.bodyText = body;
//            this.body = fullBytes;
//            this.headers = headers;
//        }

//        //public void CheckMimeAndSetBody()
//        //{
//        //    if (headers.ContainsKey("Content-Length") && headers["Content-Length"] == "0") return;

//        //    if (!headers.ContainsKey("Content-Type"))
//        //    {
//        //        body = new byte[0];
//        //        return;
//        //    }

//        //    if (headers.ContainsKey("Content-Type"))
//        //    {
//        //        string cType = headers["Content-Type"];
//        //        if (cType.Contains(";")) cType = cType.Substring(0, cType.IndexOf(';'));

//        //        if (SearchFilter("or", "mime_skip_list", cType))
//        //        {
//        //            skip = true;
//        //            bodyText = "";
//        //        }

//        //        if (!SearchFilter("or", "mime_white_list", cType))
//        //        {
//        //            bodyText = "";
//        //        }
//        //    }

//        //    DecodeArray();
//        //}


//        //private void DecodeArray()
//        //{
//        //    string cType = headers["Content-Type"];
//        //    if (cType.Contains(";")) cType = cType.Substring(0, cType.IndexOf(';'));

//        //    notEnded = false;
//        //    VDecoder vd = new VDecoder();
//        //    bool isConvertable = false;

//        //    if (filterNames.Count > 0)
//        //    {
//        //        isConvertable = SearchFilter("or", "mime_white_list", cType);
//        //    }

//        //    if (isConvertable && !headers.ContainsKey("Content-Encoding"))
//        //    {
//        //        bodyText = vd.DecodeCharset(headers["Content-Type"], body, body.Length);
//        //    }
//        //    else if (isConvertable && headers.ContainsKey("Content-Encoding"))
//        //    {
//        //        string enc = headers["Content-Encoding"];

//        //        if (enc == "gzip") body = vd.DecodeGzipToBytes(body);
//        //        else if (enc == "deflate") body = vd.DecodeDeflate(body);
//        //        else if (enc == "br") body = vd.DecodeBrotli(body);

//        //        bodyText = vd.DecodeCharset(headers["Content-Type"], body, body.Length);
//        //        //IMPORTANT: Use push end -- the data is converted to text correctly
//        //    }
//        //    else if (!isConvertable && headers.ContainsKey("Content-Encoding"))
//        //    {

//        //        //Decode contents to byte array
//        //        string enc = headers["Content-Encoding"];

//        //        if (enc == "gzip") body = vd.DecodeGzipToBytes(body);
//        //        else if (enc == "deflate") body = vd.DecodeDeflate(body);
//        //        else if (enc == "br") body = vd.DecodeBrotli(body);
//        //    }
//        //    else
//        //    {
//        //        //Data is in clearText, not convertable to printable (text) format for ex. image file, exe file
//        //        bodyText = "";
//        //    }
//        //}

//        public void Deserialize(NetworkStream ns, Request req, VSslHandler vsh = null)
//        {
//            string sResult = version + " " + statusCode + " " + httpMessage + "\r\n";
//            int ctLength = 0;

//            //edit bodyText here

//            VDecoder vd = new VDecoder();

//            if (headers.ContainsKey("Content-Length") && headers["Content-Length"] != "0" && headers["Content-Length"] != null)
//            {
//                if (mitm != null && mitm.started) //MITM Media and Text injection
//                {
//                    if (bodyText != "")
//                    {
//                        if (mitm.CheckBody(bodyText)) return;
//                        string cType = (headers.ContainsKey("Content-Type")) ? headers["Content-Type"] : null;
//                        if (cType != null)
//                        {
//                            string nt = "";
//                            nt = mitm.Inject(bodyText, headers["Content-Type"]);
//                            if (nt != null) bodyText = nt;
//                        }
//                    }
//                    else
//                    {
//                        byte[] n = mitm.MediaRewrite(this, req);
//                        if (n != null) body = n;
//                    }
//                }

//                if (bodyText != "" && headers.ContainsKey("Content-Encoding"))
//                {
//                    Array.Clear(body, 0, body.Length);
//                    byte[] toCode = vd.EncodeCharset(headers["Content-Type"], bodyText);
//                    string enc = headers["Content-Encoding"];
//                    if (enc == "gzip") body = vd.EncodeGzip(toCode);
//                    else if (enc == "deflate") body = vd.EncodeDeflate(toCode);
//                    else if (enc == "br") body = vd.EncodeBrotli(toCode);
//                    Array.Clear(toCode, 0, toCode.Length);
//                }
//                else if (bodyText == "" && headers.ContainsKey("Content-Encoding"))
//                {
//                    string enc = headers["Content-Encoding"];
//                    if (enc == "gzip") body = vd.EncodeGzip(body);
//                    else if (enc == "deflate") body = vd.EncodeDeflate(body);
//                    else if (enc == "br") body = vd.EncodeBrotli(body);
//                }
//                else if (bodyText != "" && !headers.ContainsKey("Content-Encoding"))
//                {
//                    body = vd.EncodeCharset(headers["Content-Type"], bodyText);
//                }

//                ctLength = body.Length;
//            }

//            foreach (KeyValuePair<string, string> kvp in headers.Items)
//            {
//                string line = "";
//                if (kvp.Key == "Content-Length" && ctLength > 0) line = "Content-Length: " + ctLength + "\r\n";
//                else if (kvp.Key == "Transfer-Encoding" && kvp.Value == "chunked" && ctLength > 0)
//                {
//                    // insert the content-length and skip the transfer-encoding header, because we concatanated it.
//                    line = "Content-Length: " + ctLength.ToString() + "\r\n";
//                }
//                else line = kvp.Key + ": " + kvp.Value + "\r\n";

//                sResult += line;
//            }

//            //console.Debug($"{req.target} - responded with content-type: {headers["Content-Type"]}");

//            sResult += "\r\n";
//            byte[] text = Encoding.ASCII.GetBytes(sResult);
//            if (vsh == null)
//            {
//                ns.Write(text, 0, text.Length);
//                if (ctLength > 0) ns.Write(body, 0, body.Length);
//                ns.Flush();
//            }
//            else
//            {
//                //console.Debug("Handler " + vsh.HandlerID + " receiving " + (headers.ContainsKey("Content-Type") ? headers["Content-Type"] : "No content type sent"));
//                vsh.WriteSslStream(text);
//                if (ctLength > 0) vsh.WriteSslStream(body);
//                vsh.FlushSslStream();
//            }
//        }
//    }
//}
