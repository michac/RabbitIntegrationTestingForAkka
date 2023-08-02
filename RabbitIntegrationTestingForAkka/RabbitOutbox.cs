using System.Threading.Channels;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;

namespace RabbitIntegrationTestingForAkka
{
    public class RabbitOutbox
    {
        private readonly IActorRef _rabbitPublisherActor;

        public RabbitOutbox(IActorRef rabbitPublisherActor)
        {
            _rabbitPublisherActor = rabbitPublisherActor;
        }

        public void Publish<T>(T message)
        {
            _rabbitPublisherActor.Tell(new RabbitEnvelope(message));
        }
    }
}