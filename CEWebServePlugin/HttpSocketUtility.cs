using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CEWebServePlugin
{
	delegate void ProcessHttpRequestBody(Socket socket, string headers, byte[] data);
	static class HttpSocketUtility
	{
		/// <summary>
		/// Asynchronously and minimally processes an http request on the specified socket. Reads the data, including any headers and the request body, if present, and then leaves the socket in a state ready to send a response.
		/// </summary>
		/// <param name="socket">The socket connected to the remote client that made the request</param>
		/// <param name="requestBodyCallback">An optional callback that will substitute large data handling in the request body. If null, the data sent with the request will be available in the RequestBody field.</param>
		/// <returns>The info associated with the request, encapsulated in a task.</returns>
		public static async Task<HttpRequest> ReceiveHttpRequestAsync(this Socket socket, ProcessHttpRequestBody requestBodyCallback = null)
		{
			StringBuilder reqheaders = null;
			Stream body = null;
			var bytesRead = -2;
			byte[] recv = new byte[1024];
			var args = new SocketAsyncEventArgs();
			args.SetBuffer(recv, 0, recv.Length);
			var saw = new SocketAwaitable(args);
			var i = 0;
			for (; i < 5 && 0 == args.BytesTransferred; ++i)
			{
				await socket.ReceiveAsync(saw);
				if (0 == args.BytesTransferred)
					await Task.Delay(50);
			}

			bytesRead = args.BytesTransferred;
			if (0 != bytesRead)
			{
				reqheaders = new StringBuilder();
				string s = Encoding.ASCII.GetString(recv, 0, bytesRead);
				reqheaders.Append(s);
				i = reqheaders.ToString().IndexOf("\r\n\r\n");
				while (0 > i && 0 != bytesRead)
				{
					await socket.ReceiveAsync(saw);
					bytesRead = args.BytesTransferred;
					if (0 != bytesRead)
					{
						s = Encoding.ASCII.GetString(recv, 0, bytesRead);
						reqheaders.Append(s);
						i = reqheaders.ToString().IndexOf("\r\n\r\n");
					}
				}
				if (0 > i)
					throw new Exception("Bad Request");
				long rr = 0;
				if (i + 4 < reqheaders.Length)
				{
					byte[] data = Encoding.ASCII.GetBytes(reqheaders.ToString(i + 4, reqheaders.Length - (i + 4)));
					rr = data.Length;
					// process request body data
					if (null != requestBodyCallback)
					{
						requestBodyCallback(socket, reqheaders.ToString(), data);
					}
					else
					{
						if (null == body)
							body = new MemoryStream();
						body.Write(data, 0, data.Length);
					}
				}
				int ci = reqheaders.ToString().IndexOf("\nContent-Length:", StringComparison.InvariantCultureIgnoreCase);
				if (-1 < ci)
				{
					// we have more post data
					ci += 15;
					while (ci < reqheaders.Length && char.IsWhiteSpace(reqheaders[ci]))
						++ci;
					long cl = 0;
					while (ci < reqheaders.Length && char.IsDigit(reqheaders[ci]))
					{
						cl *= 10;
						cl += reqheaders[ci] - '0';
						++ci;
					}
					if ('\r' != reqheaders[ci] && '\n' != reqheaders[ci])
						throw new Exception("Invalid Request");
					long l = rr;
					while (l < cl)
					{
						await socket.ReceiveAsync(saw);
						bytesRead = args.BytesTransferred;
						if (0 < bytesRead)
						{
							l += bytesRead;
							byte[] data = new byte[bytesRead];
							Buffer.BlockCopy(recv, 0, data, 0, bytesRead);
							// process request body data
							if (null != requestBodyCallback)
							{
								requestBodyCallback(socket, reqheaders.ToString(), data);
							}
							else
							{
								if (null == body)
									body = new MemoryStream();
								body.Write(data, 0, data.Length);
							}
						}
					}
				}
				reqheaders.Length = i + 2;
				return new HttpRequest(reqheaders.ToString(), body);
			}
			socket.Close();
			return null;
		}
	}
}
