using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace CEWebServePlugin
{
    class RequestHandler
    {
        private Socket socket;
        private Socket s;
        public RequestHandler(Socket socket, Socket s)
        {
            this.socket = socket;
            this.s = s;
        }

        public async void Process()
        {
            try
            {
                // read the incoming HTTP data
                var req = await s.ReceiveHttpRequestAsync();
                if (req.IsWebsocketUpgrade)
                {
                    await s.SendAsync(WebSocket.GetConnectionUpgradeResponse(req.Headers.Get("Sec-WebSocket-Key")));
                    var data = WebSocket.GetFrameFromString("gooooo");
                    await s.SendAsync(data, 0, data.Length);
                }
                else
                {

                    // var table = new AddressList();
                    // var memrecords = new List<MemoryRecord>();
                    //foreach(var mr in table)
                    //{
                    //    memrecords.Add(mr);
                    //}

                    var a = new Dictionary<string, string[]>();
                    a.Add("a", new string[] { "b", "c" });
                    var jsonText = JsonConvert.SerializeObject(a);
                    // "{\"b\":\"c\"}"; // JsonSerializer.SerializeToUtf8Bytes(table);
                    // our headers

                    var headers = "HTTP/1.1 200 OK\nDate: "
                        + DateTime.Now.ToUniversalTime().ToString("r")
                        + "\nContent-Type: text/html\nContent-Length: "
                        + jsonText.Length.ToString()
                        + "\nConnection: Closed\n";
                    // send them asynchronously
                    await s.SendAsync(headers + "\n" + jsonText, Encoding.ASCII);
                }
            } finally
            {
                // disconnect (no keep-alive in demo)
                await s.DisconnectAsync(false);
                s.Close();
            }
        }
    }
}
