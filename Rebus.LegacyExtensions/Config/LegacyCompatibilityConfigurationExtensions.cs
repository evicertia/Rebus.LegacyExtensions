using System.Text;
using Rebus.LegacyExtensions;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Serialization;
using Rebus.Transport;

namespace Rebus.Config
{
	/// <summary>
	/// Configuration extensions for enabling legacy compatibility
	/// </summary>
	public static class LegacyCompatibilityConfigurationExtensions
	{
		/// <summary>
		/// Makes Rebus "legacy compatible", i.e. enables wire-level compatibility with older Rebus versions. WHen this is enabled,
		/// all endpoints need to be old Rebus endpoints or new Rebus endpoints with this feature enabled
		/// </summary>
		public static void EnableLegacyCompatibility(this OptionsConfigurer configurer, Encoding encoding, bool propagateAutoCorrelationSagaId = true)
		{
			configurer.Register<ISerializer>(c =>
			{
				return new LegacyCompatibilitySerializer(encoding ?? LegacyCompatibilitySerializer.DefaultEncoding);
			});

			configurer.Decorate(c =>
			{
				var pipeline = c.Get<IPipeline>();

				pipeline = new PipelineStepConcatenator(pipeline)
					.OnReceive(new MapLegacyHeadersIncomingStep(propagateAutoCorrelationSagaId), PipelineAbsolutePosition.Front);

				// unpack object[] of transport message
				pipeline = new PipelineStepInjector(pipeline)
					.OnReceive(new UnpackLegacyMessageIncomingStep(), PipelineRelativePosition.After, typeof (DeserializeIncomingMessageStep));

				// pack into object[]
				pipeline = new PipelineStepInjector(pipeline)
					.OnSend(new PackLegacyMessageOutgoingStep(), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));

				pipeline = new PipelineStepInjector(pipeline)
					.OnSend(new MapLegacyHeadersOutgoingStep(propagateAutoCorrelationSagaId), PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep));

				return pipeline;
			});
		}
	}
}