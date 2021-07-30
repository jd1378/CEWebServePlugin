using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CESDK;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CEWebServePlugin
{


    class WebServer
    {

        private Thread thread;
        private readonly IPAddress listenOn;
        private readonly int port;
        private Boolean serving = false;
        private Socket socket;
        public Boolean IsServing { get { return serving; } }


        public WebServer(int port = 3000)
        {
            this.listenOn = IPAddress.Loopback;
            this.port = port;
        }
        public WebServer(IPAddress listenOn, int port = 3000)
        {
            this.listenOn = listenOn;
            this.port = port;
        }

        public void Start()
        {
            this.thread = new Thread(new ThreadStart(this.Listen));
            this.thread.Start();
        }

        public void Stop()
        {
            serving = false;
        }


        private async void Listen()
        {
            if (serving) return;
            serving = true;

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".

            if (socket != null)
            {
                socket.Close(1);
            }
            // Create a TCP/IP socket.
            using (socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP))
            {
                // bind to localhost:8080
                socket.Bind(new IPEndPoint(listenOn, port));
                socket.Listen(16); // listen

                try
                {
                    // begin our accept task
                    var s = await socket.AcceptTaskAsync();
                    // when we're done accepting, process the request
                    await _ProcessRequest(socket, s);
                }
                catch { }
            }
        }

        async Task _ProcessRequest(Socket socket, Socket s)
        {

            var t = Task.Run(async () =>
            {
                // spawn another waiter
                var sss = await socket.AcceptTaskAsync();
                await _ProcessRequest(socket, sss);
            });

            try
            {
                if (!serving) return;
                // we need to execute this part concurrently


                // read the incoming HTTP data
                var req = await s.ReceiveHttpRequestAsync();
                if (req.IsWebsocketUpgrade)
                {
                    await s.SendAsync(WebSocket.GetConnectionUpgradeResponse(req.Headers.Get("Sec-WebSocket-Key")));
                    // var data = WebSocket.GetFrameFromString("gooooo");
                    // await s.SendAsync(data, 0, data.Length);
                    // TODO add table value change detection and send changes in form of websocket messages
                }
                else
                {
                    var memrecords = new List<MemoryRecord>(new AddressList());

                    var tableJson = JsonConvert.SerializeObject(memrecords);
                    

                   var headers = "HTTP/1.1 200 OK\nDate: "
                        + DateTime.Now.ToUniversalTime().ToString("r")
                        + "\nContent-Type: application/json\nContent-Length: "
                        + tableJson.Length.ToString()
                        + "\nConnection: Closed\n";
                    // send them asynchronously
                    await s.SendAsync(headers + "\n" + tableJson, Encoding.UTF8);
                }
            }
            finally
            {
                try
                {
                    // disconnect (no keep-alive in demo)
                    await s.DisconnectAsync(false);
                    s.Close();
                }
                catch { }

                // finally wait for our accepting task if it's still running
                if (!t.IsCompleted && !t.IsFaulted && !t.IsCanceled)
                    await t;
            }
        }

    }

}
