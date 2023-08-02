using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Akka.Streams.Amqp.RabbitMq;

namespace RabbitIntegrationTestingForAkka
{
    public class RabbitInbox
    {
        private readonly ChannelReader<RabbitEnvelope> _channel;
        public event EventHandler<RabbitEnvelope> MessageReceived;

        public RabbitInbox(ChannelReader<RabbitEnvelope> channel)
        {
            _channel = channel;
        }
        
        // async method to listen for messages on channel reader and raise the MessageReceived event
        public async Task ListenAsync(CancellationToken cancellationToken = default)
        {
            while (await _channel.WaitToReadAsync(cancellationToken))
            {
                while (_channel.TryRead(out var message))
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
    }
}