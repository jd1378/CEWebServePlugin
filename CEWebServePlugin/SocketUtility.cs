using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// taken from https://www.codeproject.com/Articles/5274512/How-to-Implement-and-Use-Awaitable-Sockets-in-Csha


namespace CEWebServePlugin
{
    /// <summary>
    /// An awaiter for asynchronous socket operations
    /// </summary>
    // adapted from Stephen Toub's code at
    // https://blogs.msdn.microsoft.com/pfxteam/2011/12/15/awaiting-socket-operations/
    public sealed class SocketAwaitable : INotifyCompletion
    {
        // placeholder for when we don't have an actual continuation. does nothing
        readonly static Action _sentinel = () => { };
        // the continuation to use
        Action _continuation;

        /// <summary>
        /// Creates a new instance of the class for the specified <paramref name="eventArgs"/>
        /// </summary>
        /// <param name="eventArgs">The socket event args to use</param>
        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (null == eventArgs) throw new ArgumentNullException("eventArgs");
            EventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                var prev = _continuation ?? Interlocked.CompareExchange(
                    ref _continuation, _sentinel, null);
                if (prev != null) prev();
            };
        }
        /// <summary>
        /// Indicates the event args used by the awaiter
        /// </summary>
        public SocketAsyncEventArgs EventArgs { get; internal set; }
        /// <summary>
        /// Indicates whether or not the operation is completed
        /// </summary>
        public bool IsCompleted { get; internal set; }

        internal void Reset()
        {
            _continuation = null;
        }
        /// <summary>
        /// This method supports the async/await framework
        /// </summary>
        /// <returns>Itself</returns>
        public SocketAwaitable GetAwaiter() { return this; }

        // for INotifyCompletion
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (_continuation == _sentinel ||
                Interlocked.CompareExchange(
                    ref _continuation, continuation, null) == _sentinel)
            {
                Task.Run(continuation);
            }
        }
        /// <summary>
        /// Checks the result of the socket operation, throwing if unsuccessful
        /// </summary>
        /// <remarks>This is used by the async/await framework</remarks>
        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }

	/// <summary>
	/// Provides a set of extension methods to use the TAP pattern with sockets
	/// </summary>
#if SOCKASYNCLIB
	public 
#endif
	static partial class SocketUtility
	{
		// for TaskCompletionSource<_Void> so we can use it with no result
		private sealed class _Void
		{
			private _Void() { }
		}
		/// <summary>
		/// Receive data using the specified awaitable class
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="awaitable">An instance of <see cref="SocketAwaitable"/></param>
		/// <returns><paramref name="awaitable"/></returns>
		public static SocketAwaitable ReceiveAsync(this Socket socket,
		SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.ReceiveAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			return awaitable;
		}
		/// <summary>
		/// Sends data using the specified awaitable class
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="awaitable">An instance of <see cref="SocketAwaitable"/></param>
		/// <returns><paramref name="awaitable"/></returns>
		public static SocketAwaitable SendAsync(this Socket socket,
			SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.SendAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			return awaitable;
		}
		/// <summary>
		/// Asynchronously receives data on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="buffer">The buffer to hold the data to receive</param>
		/// <param name="offset">An offset into <paramref name="buffer"/> to use</param>
		/// <param name="length">The length of the data to be received into <paramref name="buffer"/></param>
		/// <param name="socketFlags">The socket flags</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes received</returns>
		public static Task<int> ReceiveAsync(
	this Socket socket, byte[] buffer, int offset, int length,
	SocketFlags socketFlags)
		{
			var tcs = new TaskCompletionSource<int>(socket);
			socket.BeginReceive(buffer, offset, length, socketFlags, iar =>
			{
				var t = (TaskCompletionSource<int>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					t.TrySetResult(s.EndReceive(iar));
				}
				catch (Exception exc)
				{
					t.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}

		/// <summary>
		/// Asynchronously sends data on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="buffers">The buffers of data to send</param>
		/// <param name="socketFlags">The socket flags</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task<int> SendAsync(this Socket socket,
			IList<ArraySegment<byte>> buffers,
			SocketFlags socketFlags = 0)
		{
			var tcs = new TaskCompletionSource<int>(socket);
			socket.BeginSend(buffers, socketFlags, iar =>
			{
				var t = (TaskCompletionSource<int>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					t.TrySetResult(s.EndSend(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}

		/// <summary>
		/// Asynchronously sends data on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="buffer">The buffer of data to send</param>
		/// <param name="offset">An offset into the <paramref name="buffer"/> to use</param>
		/// <param name="length">The length of the data in <paramref name="buffer"/> to send</param>
		/// <param name="socketFlags">The socket flags</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task<int> SendAsync(
	this Socket socket, byte[] buffer, int offset, int length,
	SocketFlags socketFlags = 0)
		{
			var tcs = new TaskCompletionSource<int>(socket);
			socket.BeginSend(buffer, offset, length, socketFlags, iar =>
			{
				var t = (TaskCompletionSource<int>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					t.TrySetResult(s.EndSend(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}

		/// <summary>
		/// Asynchronously sends data on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="buffer">The buffer of data to send</param>
		/// <param name="offset">An offset into the <paramref name="buffer"/> to use</param>
		/// <param name="length">The length of the data in <paramref name="buffer"/> to send</param>
		/// <param name="socketFlags">The socket flags</param>
		/// <param name="remoteEP">The remote <see cref="EndPoint"/></param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task<int> SendToAsync(
	this Socket socket, byte[] buffer, int offset, int length,
	SocketFlags socketFlags, EndPoint remoteEP)
		{
			var tcs = new TaskCompletionSource<int>(socket);
			socket.BeginSendTo(buffer, offset, length, socketFlags, remoteEP, iar =>
			{
				var t = (TaskCompletionSource<int>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					t.TrySetResult(s.EndSendTo(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously sends a file on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="fileName">The file name</param>
		/// <param name="preBuffer">A buffer to prepend</param>
		/// <param name="postBuffer">A buffer to append</param>
		/// <param name="flags">The transmission options</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task<int> SendFileAsync(
	this Socket socket, string fileName, byte[] preBuffer, byte[] postBuffer,
	TransmitFileOptions flags)
		{
			var tcs = new TaskCompletionSource<int>(socket);
			socket.BeginSendFile(fileName, preBuffer, postBuffer, flags, iar =>
			{
				var t = (TaskCompletionSource<int>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					t.TrySetResult(s.EndSend(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously sends a file on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="fileName">The file name</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task SendFileAsync(
	this Socket socket, string fileName)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginSendFile(fileName, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndSendFile(iar);
					t.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously sends data on a <see cref="Socket"/>
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="text">The text to send</param>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="socketFlags">The socket flags</param>
		/// <returns>A <see cref="Task{Int32}"/> containing the number of bytes sent</returns>
		public static Task<int> SendAsync(this Socket socket, string text, Encoding encoding = null, SocketFlags socketFlags = 0)
		{
			if (null == encoding)
				encoding = Encoding.UTF8;
			byte[] ba = encoding.GetBytes(text);
			return SendAsync(socket, ba, 0, ba.Length, socketFlags);
		}
		/// <summary>
		/// Asynchronously connects a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="remoteEP">The remote endpoint</param>
		/// <returns>A <see cref="Task"/> for performing the operation</returns>
		public static Task ConnectAsync(this Socket socket, EndPoint remoteEP)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginConnect(remoteEP, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndConnect(iar);
					tcs.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously connects a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="address">The remote endpoint IP address</param>
		/// <param name="port">The remote port</param>
		/// <returns>A <see cref="Task"/> for performing the operation</returns>
		public static Task ConnectAsync(this Socket socket, IPAddress address, int port)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginConnect(address, port, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndConnect(iar);
					tcs.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously connects a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="addresses">The remote endpoint IP addresses to connect to</param>
		/// <param name="port">The remote port</param>
		/// <returns>A <see cref="Task"/> for performing the operation</returns>
		public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginConnect(addresses, port, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndConnect(iar);
					tcs.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously connects a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="host">The remote host</param>
		/// <param name="port">The remote port</param>
		/// <returns>A <see cref="Task"/> for performing the operation</returns>
		public static Task ConnectAsync(this Socket socket, string host, int port)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginConnect(host, port, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndConnect(iar);
					tcs.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously disconnect a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="reuseSocket">True if the socket is to be reused, otherwise false</param>
		/// <returns>A <see cref="Task"/> representing the operation</returns>
		public static Task DisconnectAsync(this Socket socket, bool reuseSocket)
		{
			var tcs = new TaskCompletionSource<_Void>(socket);
			socket.BeginDisconnect(reuseSocket, iar =>
			{
				var t = (TaskCompletionSource<_Void>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					s.EndDisconnect(iar);
					tcs.TrySetResult(null);
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously accepts a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <returns>A <see cref="Task{Socket}"/> for performing the operation</returns>
		public static Task<Socket> AcceptTaskAsync(this Socket socket)
		{
			var tcs = new TaskCompletionSource<Socket>(socket);
			socket.BeginAccept(iar =>
			{
				var t = (TaskCompletionSource<Socket>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					tcs.TrySetResult(s.EndAccept(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously accepts a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="receiveSize">The receive buffer size</param>
		/// <returns>A <see cref="Task{Socket}"/> for performing the operation</returns>
		public static Task<Socket> AcceptTaskAsync(this Socket socket, int receiveSize)
		{
			var tcs = new TaskCompletionSource<Socket>(socket);
			socket.BeginAccept(receiveSize, iar =>
			{
				var t = (TaskCompletionSource<Socket>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					tcs.TrySetResult(s.EndAccept(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
		/// <summary>
		/// Asynchronously accepts a socket
		/// </summary>
		/// <param name="socket">The socket</param>
		/// <param name="acceptSocket">The accept socket</param>
		/// <param name="receiveSize">The receive buffer size</param>
		/// <returns>A <see cref="Task{Socket}"/> for performing the operation</returns>
		public static Task<Socket> AcceptTaskAsync(this Socket socket, Socket acceptSocket, int receiveSize)
		{
			var tcs = new TaskCompletionSource<Socket>(socket);
			socket.BeginAccept(acceptSocket, receiveSize, iar =>
			{
				var t = (TaskCompletionSource<Socket>)iar.AsyncState;
				var s = (Socket)t.Task.AsyncState;
				try
				{
					tcs.TrySetResult(s.EndAccept(iar));
				}
				catch (Exception exc)
				{
					tcs.TrySetException(exc);
				}
			}, tcs);
			return tcs.Task;
		}
	}

}
