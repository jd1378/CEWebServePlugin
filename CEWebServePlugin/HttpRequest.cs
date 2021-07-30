using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// taken from https://www.codeproject.com/Articles/5274512/How-to-Implement-and-Use-Awaitable-Sockets-in-Csha

namespace CEWebServePlugin
{
	sealed class HttpRequest
	{
		readonly NameValueCollection _headers;
		readonly NameValueCollection _queryString;
		readonly NameValueCollection _form;
		readonly Stream _body;

		public NameValueCollection Headers { get { return _headers; } }

	internal HttpRequest(string requestHeaders, Stream body)
		{
			_headers = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
			var tr = new StringReader(requestHeaders);
			string line = tr.ReadLine();
			if (string.IsNullOrEmpty(line))
				throw new Exception("Invalid request");
			var parts = line.Split(' ');
			if (3 != parts.Length && 2 != parts.Length)
				throw new Exception("Invalid request");
			Method = parts[0];
			Url = parts[1];
			if (3 == parts.Length)
				Version = parts[2];
			else
				Version = null;
			_queryString = new NameValueCollection();
			var i = Url.IndexOf('?');
			if (-1 < i && i < Url.Length - 1)
			{
				var qs = Url.Substring(i + 1);
				var args = qs.Split('&');
				for (i = 0; i < args.Length; i++)
				{
					var arg = args[i];
					var argParts = arg.Split('=');
					var name = argParts[0];
					if (2 == argParts.Length)
						_queryString.Add(name, argParts[1]);
					else
						_queryString.Add(name, null);
				}
			}
			while (null != (line = tr.ReadLine()))
			{
				i = line.IndexOf(':');
				if (-1 < i)
				{
					var name = line.Substring(0, i);
					var value = line.Substring(i + 1).Trim();
					_headers.Add(name, value);
				}
			}
			_form = new NameValueCollection();
			if (null == body)
				return;
			if (body.CanSeek)
				body.Position = 0L;
			_body = body;
			if (HasFormData)
			{
				var sr = new StreamReader(body);
				var sa = sr.ReadToEnd().Split(';');
				for (i = 0; i < sa.Length; i++)
				{
					var s = sa[i];
					var si = s.IndexOf('=');
					if (-1 < si)
						_form.Add(s.Substring(0, si), s.Substring(si + 1));
				}
				if (body.CanSeek)
					body.Position = 0L;
			}
		}
		public Stream Body { get { return _body; } }
		public string Method { get; }
		public string Url { get; }
		public string Version { get; }
		public bool IsKeepAlive
		{
			get
			{
				var con = _headers.Get("Connection");
				return !string.IsNullOrEmpty(con) &&
					con.ToLowerInvariant() == "keep-alive";
			}
		}
		public bool HasFormData
		{
			get
			{
				return "application/x-www-form-urlencoded".Equals(ContentType, StringComparison.InvariantCultureIgnoreCase);
			}
		}
		public string ContentType
		{
			get
			{
				return _headers.Get("Content-Type");
			}
		}

		public Boolean IsWebsocketUpgrade
		{
			get
			{
				return _headers.Get("Connection").Equals("Upgrade") && _headers.Get("Upgrade").Equals("websocket");
			}
		}

		public int ContentLength
		{
			get
			{
				if (null != _body)
					return (int)_body.Length;
				return -1;
			}
		}
		public bool IsHttp10
		{
			get
			{

				string s = Version;
				if (null != s && ("" == s ||
					0 == string.Compare("HTTP/1.0", s, StringComparison.InvariantCultureIgnoreCase) ||
					0 == string.Compare("HTTP/1", s, StringComparison.InvariantCultureIgnoreCase)))
					return true;
				return false;
			}
		}
	}
}
