/*
* Copyright (C) Sportradar AG. See LICENSE for full license governing this code
*/

using Sportradar.OddsFeed.SDK.Api;

namespace Sportradar.OddsFeed.SDK.Entities.Internal.EntitiesImpl
{
    /// <summary>
    /// Represents a message indicating a producer serving odds via the feed went down. The class might be dispatched
    /// either by the feed or the SDK itself
    /// </summary>
    internal class ProducerStatusChange : Message, IProducerStatusChange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerStatusChange" /> class
        /// </summary>
        /// <param name="timestamp">The value specifying when the message was generated in the milliseconds since EPOCH UTC </param>
        /// <param name="producer">The <see cref="IProducer" /> specifying the producer / service which dispatched the
        /// <param name="rawMessage">raw message from broker</param>
        /// <param name="requestId">broker message routingKey</param>
        /// current <see cref="ProducerStatusChange" /> message
        /// </param>
        public ProducerStatusChange(long timestamp, IProducer producer, byte[] rawMessage, string routingKey)
            : base(new MessageTimestamp(timestamp), producer, rawMessage, routingKey) { }
    }
}
