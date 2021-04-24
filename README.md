# Rebus.LegacyExtensions
Rebus legacy compatibility extensions, intended for Rebus interoperability of old rebus endpoints (v0.84) with modern ones.

Legacy compatibility activation is done activating the option => EnableLegacyCompatibility("Encoding").

Also One exchange per class for publish can be activated using => UseOneExchangePerClassPublish(), in the transport configuration.