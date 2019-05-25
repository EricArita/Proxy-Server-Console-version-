using System;

namespace TestProxyOnConsole
{
    public class Request : IDisposable
    {
        public string full;
        public bool bogus = false;
        public bool notEnded = false;
        public string target;
        public string method;
        public string version;
        public string htmlBody;
        public VDictionary headers = new VDictionary();
        private bool disposed = false;

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
                full = null;
                target = null;
                method = null;
                version = null;
                htmlBody = null;
                headers.Clear();
                headers.Dispose();
                headers = null;
            }

            disposed = true;
        }

        public Request(string req, bool sslMode = false)
        {
            full = req;
            Serialize(sslMode);
        }

        public void Serialize(bool fromSslStream = false)
        {
            if (full == "")
            {
                bogus = true;
                return;
            }

            if (!full.EndsWith("\r\n\r\n") && fromSslStream) notEnded = true; //setting only when requests are marked to allow normal (not MITM) https packets even if they are not ending with \r\n\r\n

            try
            {
                string infoLine = full.Split('\n')[0].Replace("\r", String.Empty);
                //Console.WriteLine(infoLine);
                string[] iParts = infoLine.Split(' ');

                // request line of request message header
                method = iParts[0];
                target = iParts[1];
                version = iParts[2];

                headers = new VDictionary();
                string[] data = full.Split('\n');
                bool isBody = false;

                for (int i = 1; i < data.Length; i++)
                {
                    string line = data[i].Replace("\r", String.Empty);

                    if (line == "")
                    {
                        isBody = true;
                        continue;
                    }

                    if (!isBody)
                    {
                        //Add headers
                        string hName = line.Substring(0, line.IndexOf(':'));
                        string hValue = line.Substring(line.IndexOf(':') + 2, line.Length - line.IndexOf(':') - 2);
                        headers.Add(hName, hValue);
                    }
                    else
                    {
                        if ((i + 1) < data.Length) htmlBody += line + Environment.NewLine;
                        else if ((i + 1) == data.Length) htmlBody += line;
                    }
                }

                //Add ssl packet filter
                if (!version.Contains("HTTP")) bogus = true;
            }
            catch (Exception)
            {
                bogus = true;
            }
        }

        public string Deserialize()
        {
            string request = method + " " + target + " " + version + Environment.NewLine;

            for (int i = 0; i < headers.Count; i++)
            {
                string hName = headers.Keys.ToArray()[i];
                string hValue = headers.Values.ToArray()[i];
                string line = hName + ": " + hValue;
                request += line + Environment.NewLine;
            }

            request += Environment.NewLine;
            request += htmlBody;
            return request;
        }
    }
}