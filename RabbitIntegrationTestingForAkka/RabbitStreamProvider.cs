using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Dsl;

namespace RabbitIntegrationTestingForAkka
{
    public class RabbitStreamProvider
    {
        private readonly TaskCompletionSource<int> _isReady = new TaskCompletionSource<int>();
        private IActorRef _rabbitPublisherActor;
        private Source<RabbitEnvelope,NotUsed> _broadcastProducer;
        private ActorMaterializer _mat;

        public void Initialize(IActorRef rabbitPublisherActor, 
            Source<RabbitEnvelope, NotUsed> broadcastProducer,
            ActorMaterializer mat)
        {
            _rabbitPublisherActor = rabbitPublisherActor;
            _broadcastProducer = broadcastProducer;
            _mat = mat;
            _isReady.SetResult(0);
        }
        
        public async Task<RabbitOutbox> GetRabbitOutbox()
        {
            await _isReady.Task;

            return new RabbitOutbox(_rabbitPublisherActor);
        }

        public async Task<RabbitInbox> GetRabbitInbox()
        {
            await _isReady.Task;

            return new RabbitInbox(_broadcastProducer.RunWith(ChannelSink.AsReader<RabbitEnvelope>(256), _mat));
        }
    }
}