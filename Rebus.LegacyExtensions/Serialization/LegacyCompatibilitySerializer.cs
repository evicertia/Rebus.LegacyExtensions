using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

#pragma warning disable 1998

namespace Rebus.LegacyExtensions
{
	/// <summary>
	/// Implementation of <see cref="ISerializer"/> that uses Newtonsoft JSON.NET internally, with some pretty robust settings
	/// (i.e. full type info is included in the serialized format in order to support deserializing "unknown" types like
	/// implementations of interfaces, etc)
	/// </summary>
	internal class LegacyCompatibilitySerializer : ISerializer
	{
		/// <summary>
		/// Proper content type when a message has been serialized with this serializer (or another compatible JSON serializer) and it uses the standard UTF8 encoding
		/// </summary>
		public const string JsonUtf8ContentType = "application/json;charset=utf-8";

		/// <summary>
		/// Contents type when the content is JSON
		/// </summary>
		public const string JsonContentType = "application/json";

		internal static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
		{
			SerializationBinder = new LegacySubscriptionMessagesBinder(),
			TypeNameHandling = TypeNameHandling.Auto,
			Formatting = Formatting.None,
			Culture = CultureInfo.InvariantCulture,
			Converters = new List<JsonConverter> { new StringEnumConverter() }
		};

		internal static readonly Encoding DefaultEncoding = Encoding.UTF8;

		readonly JsonSerializerSettings _settings;
		readonly Encoding _encoding;

		public LegacyCompatibilitySerializer()
			: this(DefaultSettings, DefaultEncoding)
		{
		}

		internal LegacyCompatibilitySerializer(Encoding encoding)
			: this(DefaultSettings, encoding)
		{
		}

		internal LegacyCompatibilitySerializer(JsonSerializerSettings jsonSerializerSettings)
			: this(jsonSerializerSettings, DefaultEncoding)
		{
		}

		internal LegacyCompatibilitySerializer(JsonSerializerSettings jsonSerializerSettings, Encoding encoding)
		{
			_settings = jsonSerializerSettings;
			_encoding = encoding;
		}

		private string SerializeWithArrayInitialType(object body)
		{
			var sb = new StringBuilder(256);
			var serializer = JsonSerializer.Create(_settings);

			using (StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture))
			using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
			{
				sw.Write("{\"$type\": \"System.Object[]\", \"$values\":");
				serializer.Serialize(jsonWriter, body);
				sw.Write("}");
				return sw.ToString();
			}
		}

		/// <summary>
		/// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
		/// </summary>
		public async Task<TransportMessage> Serialize(Message message)
		{
			var jsonText = SerializeWithArrayInitialType(message.Body);

			var bytes = _encoding.GetBytes(jsonText);
			var headers = message.Headers.Clone();
			headers[Headers.ContentType] = $"{JsonContentType};charset={_encoding.HeaderName}";
			return new TransportMessage(headers, bytes);
		}

		/// <summary>
		/// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
		/// </summary>
		public async Task<Message> Deserialize(TransportMessage transportMessage)
		{
			var contentType = transportMessage.Headers.GetValue(Headers.ContentType);

			// Optimize default case by using the same _enconding instance.
			if (contentType == JsonUtf8ContentType)
			{
				return GetMessage(transportMessage, _encoding);
			}

			if (contentType.StartsWith(JsonContentType))
			{
				var encoding = EncodingUtils.GetEncoding(contentType);
				return GetMessage(transportMessage, encoding);
			}

			throw new FormatException($"Unknown content type: '{contentType}' - must be '{JsonUtf8ContentType}' for the JSON serialier to work");
		}

		Message GetMessage(TransportMessage transportMessage, Encoding bodyEncoding)
		{
			var bodyString = bodyEncoding.GetString(transportMessage.Body);
			var bodyObject = Deserialize(bodyString);
			var headers = transportMessage.Headers.Clone();
			return new Message(headers, bodyObject);
		}

		object Deserialize(string bodyString)
		{
			try
			{
				return JsonConvert.DeserializeObject(bodyString, _settings);
			}
			catch (Exception exception)
			{
				throw new FormatException($"Could not deserialize JSON text: '{bodyString}'", exception);
			}
		}
	}
}