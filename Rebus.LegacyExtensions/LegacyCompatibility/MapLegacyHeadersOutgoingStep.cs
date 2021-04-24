using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.LegacyExtensions
{
	class MapLegacyHeadersOutgoingStep : IOutgoingStep
	{
		private bool _propagateAutoCorrelationSagaId;
		internal const string AutoCorrelationSagaIdHeader = "rebus-autocorrelation-saga-id";

		public MapLegacyHeadersOutgoingStep(bool propagateAutoCorrelationSagaId)
		{
			_propagateAutoCorrelationSagaId = propagateAutoCorrelationSagaId;
		}
		private void AddCustomHeaders(Dictionary<string, string> headers)
		{
			headers["message-date"] = DateTime.Parse(headers["rbs2-senttime"]).ToUniversalTime().ToString("O");
		}

		private void AddMulticastHeader (Dictionary<string, string> headers)
		{
			if (headers[Headers.Intent] == "pub")
			{
				headers["rebus-multicast"] = "";
			}
		}
		private void PropagateAutoCorrelationSagaIdToHeaders(Dictionary<string, string> headers)
		{
			if (MessageContext.Current?.TransactionContext?.Items?.ContainsKey(AutoCorrelationSagaIdHeader) == true)
				headers[AutoCorrelationSagaIdHeader] = (string)MessageContext.Current.TransactionContext.Items[AutoCorrelationSagaIdHeader];
		}

		public async Task Process(OutgoingStepContext context, Func<Task> next)
		{
			var transportMessage = context.Load<TransportMessage>();

			var body = transportMessage.Body;
			var headers = transportMessage.Headers;

			var newHeaders = MapTrivialHeaders(headers);

		   var allHeaders =  newHeaders.Union(headers).ToDictionary(x => x.Key, x => x.Value);

			MapSpecialHeaders(allHeaders);

			AddCustomHeaders(allHeaders);
			AddMulticastHeader(allHeaders);

			if (_propagateAutoCorrelationSagaId)
				PropagateAutoCorrelationSagaIdToHeaders(allHeaders);

			context.Save(new TransportMessage(allHeaders, body));

			await next();
		}

		void MapSpecialHeaders(Dictionary<string, string> headers)
		{
			MapContentType(headers);
		}

		void MapContentType(Dictionary<string, string> headers)
		{
			string contentType;
			if (!headers.TryGetValue(Headers.ContentType, out contentType)) return;

			var charset = EncodingUtils.GetCharset(contentType);

			if (charset == null)
			{
				throw new FormatException($"Could not find 'charset' property in the content type: '{contentType}'");
			}

			headers["rebus-content-type"] = "text/json";
			headers["rebus-encoding"] = charset;
		}

		Dictionary<string,string> MapTrivialHeaders(Dictionary<string, string> headers)
		{
			return headers
				.Select(kvp =>
				{
					string newKey;

					return TrivialMappings.TryGetValue(kvp.Key, out newKey)
						? new KeyValuePair<string, string>(newKey, kvp.Value)
						: kvp;
				})
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		static readonly Dictionary<string, string> TrivialMappings = new Dictionary<string, string>
		{
			{"rebus-source-queue", Headers.SourceQueue},
			{"rebus-correlation-id", Headers.CorrelationId},
			{"rebus-return-address", Headers.ReturnAddress},
			{"rebus-msg-id", Headers.MessageId},
			{"rebus-time-to-be-received", Headers.TimeToBeReceived}


		}.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

	}
}