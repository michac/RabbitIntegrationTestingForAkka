using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace RabbitIntegrationTestingForAkka
{
    public class RabbitStreamService : BackgroundService
    {
        public const string InitMessage = "start";
        public const string CompleteMessage = "complete";
        public const string AckMessage = "ack";

        
        private readonly ActorSystem _system;
        private readonly RabbitStreamProvider _provider;
        private ActorMaterializer _mat;

        public RabbitStreamService(ActorSystem system, RabbitStreamProvider provider)
        {
            _system = system;
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = RestartSettings.Create(
                minBackoff: TimeSpan.FromSeconds(3),
                maxBackoff: TimeSpan.FromSeconds(10),
                randomFactor: 0.2);
            
            _mat = ActorMaterializer.Create(_system);
            var connectionSettings =
                AmqpConnectionDetails
                    .Create("mchris_tomahawk", 5671)
                    .WithCredentials(AmqpCredentials.Create("admin", "Pho#1enix"))
                    .WithSsl(new SslOption("mchris_tomahawk", enabled: true))
                    .WithAutomaticRecoveryEnabled(false);
            var exchangeDeclaration = ExchangeDeclaration.Create("akka-stream-exchange", ExchangeType.Fanout);
            var queueDeclaration =
                QueueDeclaration.Create("akka-stream-source").WithDurable(false).WithAutoDelete(true);
            var bindingDeclaration = BindingDeclaration.Create("akka-stream-source", "akka-stream-exchange");

            // create akka stream with rabbit source and actor sink

            var restartSource = RestartSource.WithBackoff(() => AmqpSource.AtMostOnceSource(
                NamedQueueSourceSettings.Create(connectionSettings, "akka-stream-source")
                    .WithDeclarations(exchangeDeclaration, queueDeclaration, bindingDeclaration),
                bufferSize: 10), settings);

            // var actorSinkProps = DependencyResolver.For(_system).Props<RabbitSinkActor>();
            // var actorSink = _system.ActorOf(actorSinkProps, "rabbit-sink");

            var runnableGraph = restartSource.Select(ConvertIncomingMessageToEnvelope).ToMaterialized(BroadcastHub.Sink<RabbitEnvelope>(bufferSize: 256), Keep.Right);
            var broadcastProducer = runnableGraph.Run(_mat);
            // amqpSource
            //     .RunWith(Sink.ActorRefWithAck<IncomingMessage>(actorSink, InitMessage, AckMessage, CompleteMessage),
            //         mat);
            

            // create akka stream with actor source and rabbit sink


            var actorPublisherProps = DependencyResolver.For(_system).Props<RabbitPublisherActor>();
            var actorPublisherSource = Source.ActorPublisher<OutgoingMessage>(actorPublisherProps);

            var rabbitRestartSink = RestartSink.WithBackoff(() =>
            {
                Console.WriteLine("Restarting sink");
                var rabbitSink = AmqpSink.Create(AmqpSinkSettings.Create(connectionSettings)
                    .WithExchange("akka-stream-exchange"));

                return rabbitSink;
            }, settings);

            var actorPublisherRef = actorPublisherSource.To(rabbitRestartSink).Run(_mat);
            
            _provider.Initialize(actorPublisherRef, broadcastProducer, _mat);

            await stoppingToken.WaitForCancellation();

            _mat.Dispose();
        }

        private RabbitEnvelope ConvertIncomingMessageToEnvelope(IncomingMessage message)
        {
            switch (message.Properties.Type)
            {
                case "String":
                    return new RabbitEnvelope(System.Text.Encoding.UTF8.GetString(message.Bytes.ToArray()));
                default:
                    return new RabbitEnvelope(null);
            }
        }
    }
}