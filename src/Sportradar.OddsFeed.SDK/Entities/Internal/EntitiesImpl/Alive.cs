/*
* Copyright (C) Sportradar AG. See LICENSE for full license governing this code
*/

using Sportradar.OddsFeed.SDK.Api;

namespace Sportradar.OddsFeed.SDK.Entities.Internal.EntitiesImpl
{
    /// <summary>
    /// Represents an alive message dispatched by the feed
    /// </summary>
    internal class Alive : Message, IAlive
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Alive" /> class
        /// </summary>
        /// <param name="timestamp">The value specifying timestamps related to the message (in the milliseconds since EPOCH UTC)</param>
        /// <param name="producer">The <see cref="IProducer" /> specifying the producer / service which dispatched the current <see cref="Message" /> message</param>
        /// <param name="subscribed">A value indicating whether the SDK is subscribed to the producer specified by the <see cref="IProducer" /> property</param>
        /// <param name="rawMessage">raw message from broker</param>
        /// <param name="requestId">broker message routingKey</param>
        internal Alive(IMessageTimestamp timestamp, IProducer producer, bool subscribed, byte[] rawMessage, string routingKey)
            : base(timestamp, producer, rawMessage, routingKey)
        {
            Subscribed = subscribed;
        }

        /// <summary>
        /// Gets a value indicating whether the SDK is subscribed to the producer specified by <see cref="IProducer" /> property
        /// </summary>
        public bool Subscribed { get; }
    }
}
