using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.LegacyExtensions
{
	[StepDocumentation("Mutates the headers of the incoming message by mapping understood Rebus1 headers to their counterparts in Rebus2")]
	class MapLegacyHeadersIncomingStep : IIncomingStep
	{
		internal const string LegacyMessageHeader = "rbs2-rebus-legacy-message";
		internal const string AutoCorrelationSagaIdHeader = "rebus-autocorrelation-saga-id";
		internal const string OldRebusMessage = "OldRebusMessage";
		internal const string LegacyConvertedMessage = "LegacyConvertedMessage";
		private bool _propagateAutoCorrelationSagaId;
		public MapLegacyHeadersIncomingStep(bool propagateAutoCorrelationSagaId)
		{
			_propagateAutoCorrelationSagaId = propagateAutoCorrelationSagaId;
		}

		private void StoreAutoCorrelationSagaId(TransportMessage message)
		{
			if(message.Headers.ContainsKey(AutoCorrelationSagaIdHeader))
				MessageContext.Current.TransactionContext.Items.TryAdd(AutoCorrelationSagaIdHeader, message.Headers["rebus-autocorrelation-saga-id"]);
		}

		private void AddIntentHeader(Dictionary<string, string> headers)
		{
			if (headers.ContainsKey("rebus-multicast"))
				headers[Headers.Intent] = "pub";
			else
				headers[Headers.Intent] = "p2p";
		}
		public async Task Process(IncomingStepContext context, Func<Task> next)
		{
			var transportMessage = context.Load<TransportMessage>();
			var headers = transportMessage.Headers;

			if (headers.ContainsKey("rebus-msg-id"))
			{
				MutateLegacyTransportMessage(context, headers, transportMessage);
			}

			if(_propagateAutoCorrelationSagaId)
			{
				StoreAutoCorrelationSagaId(transportMessage);
			}
			await next();
		}

		private void AddAsyncLegacyHeaders(Dictionary<string, string> headers)
		{
			if (headers["rbs2-corr-id"].StartsWith("request-reply"))
			{
				headers["rbs2-in-reply-to"] = headers["rbs2-corr-id"];
			}
		}

		void MutateLegacyTransportMessage(IncomingStepContext context, Dictionary<string, string> headers, TransportMessage transportMessage)
		{
			var newHeaders = MapTrivialHeaders(headers);

			var allHeaders = newHeaders.Union(headers).ToDictionary(x => x.Key, x => x.Value);
			AddAsyncLegacyHeaders(allHeaders);
			MapSpecialHeaders(allHeaders);
			AddIntentHeader(allHeaders);

			// If "rbs2-msg-id" header is present, message comes from V2 Rebus with legacyCompatibility activated
			allHeaders[LegacyMessageHeader] = headers.ContainsKey(Headers.MessageId)
				? LegacyConvertedMessage
				: OldRebusMessage;

			context.Save(new TransportMessage(allHeaders, transportMessage.Body));
		}

		void MapSpecialHeaders(Dictionary<string, string> headers)
		{
			MapContentType(headers);
		}

		static void MapContentType(Dictionary<string, string> headers)
		{
			string contentType;
			if (!headers.TryGetValue("rebus-content-type", out contentType)) return;

			if (contentType == "text/json")
			{
				string contentEncoding;

				if (headers.TryGetValue("rebus-encoding", out contentEncoding))
				{
					headers[Headers.ContentType] = $"{LegacyCompatibilitySerializer.JsonContentType};charset={contentEncoding}";
				}
				else
				{
					throw new FormatException(
						"Content type was 'text/json', but the 'rebus-encoding' header was not present!");
				}
			}
			else
			{
				throw new FormatException(
					$"Sorry, but the '{contentType}' content type is currently not supported by the legacy header mapper");
			}
		}

		Dictionary<string, string> MapTrivialHeaders(Dictionary<string, string> headers)
		{
			var rebus2Headers = new List<string>() { Headers.SourceQueue, Headers.CorrelationId, Headers.ReturnAddress, Headers.MessageId};
			return headers.Where(kvp => !rebus2Headers.Contains(kvp.Key))
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
			{"rebus-time-to-be-received", Headers.TimeToBeReceived},
		};
	}
}