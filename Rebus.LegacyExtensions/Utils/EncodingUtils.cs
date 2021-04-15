using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rebus.LegacyExtensions
{
	public static class EncodingUtils
	{
		public static string GetCharset(string contentTypeHeader)
		{
			var parts = contentTypeHeader.Split(';');

			if (parts.Length == 1)
			{
				return null;
			}

			foreach (var part in parts.Skip(1))
			{
				var elements = part.Split('=');
				if (elements.Length > 1 && string.Equals(elements[0], "charset", StringComparison.OrdinalIgnoreCase))
				{
					return elements[1];
				}
			}

			return null;
		}

		public static bool TryParse(string contentTypeHeader, out string contentType, out Encoding encoding)
		{
			var parts = contentTypeHeader.Split(';');
			var charset = (string)null;
			encoding = null;
			contentType = parts[0];

			if (parts.Length == 1)
			{
				return true;
			}

			foreach (var part in parts.Skip(1))
			{
				var elements = part.Split('=');
				if (elements.Length > 1 && string.Equals(elements[0], "charset", StringComparison.OrdinalIgnoreCase))
				{
					charset = elements[1];
					break;
				}
			}

			if (charset != null)
			{
				try
				{
					encoding = Encoding.GetEncoding(charset);
				}
				catch
				{
					// Invalid or unknown encoding.
					return false;
				}
			}

			return true;
		}

		public static Encoding GetEncoding(string contentTypeHeader)
		{
			string dummy;
			Encoding result = null;

			TryParse(contentTypeHeader, out dummy, out result);
			return result;
		}
	}
}
