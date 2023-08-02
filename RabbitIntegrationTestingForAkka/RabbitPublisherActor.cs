using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams.Actors;
using Akka.Streams.Amqp.RabbitMq;
using RabbitMQ.Client;

namespace RabbitIntegrationTestingForAkka
{
    public class RabbitPublisherActor : ActorPublisher<OutgoingMessage>
    {
        private Queue<OutgoingMessage> _buffer = new Queue<OutgoingMessage>();

        protected override bool Receive(object message)
        {
            switch (message)
            {
                case RabbitEnvelope rm:
                    if (_buffer.Count == 0 && TotalDemand > 0)
                    {
                        Context.GetLogger().Info($"Received envelope. Buffer is empty and total demand is {TotalDemand} so sending to stream.");
                        OnNext(RabbitMessageToOutgoingMessage(rm));
                    }
                    else
                    {
                        Context.GetLogger().Info($"Received envelope. Buffer is has {_buffer.Count} values, so adding to buffer then attempting to empty buffer.");
                        _buffer.Enqueue(RabbitMessageToOutgoingMessage(rm));
                        DeliverBuffer();
                    }
                    return true;
                case Request _:
                    DeliverBuffer();
                    return true;
                case Cancel _:
                    Context.Stop(Self);
                    return true;
                default:
                    return false;
            }
        }
        
        private OutgoingMessage RabbitMessageToOutgoingMessage(RabbitEnvelope rm)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rm.Message));

            var type = typeof(IBasicProperties).Assembly.GetTypes().Where(t => t.FullName == "RabbitMQ.Client.Framing.BasicProperties");
            var props = (IBasicProperties) Activator.CreateInstance(type.First());

            props.Type = rm.Message.GetType().Name;
            
            return new OutgoingMessage(ByteString.CopyFrom(bytes), true, true, props);
        }

        private void DeliverBuffer()
        {
            while (TotalDemand > 0 && _buffer.Count > 0)
            {
                Context.GetLogger().Info($"In deliver buffer loop. Buffer has {_buffer.Count} values, and TotalDemand is {TotalDemand}.");
                
                var use = _buffer.DequeueTake((int) Math.Min(TotalDemand, int.MaxValue)).ToImmutableList();

                use.ForEach(OnNext);
            }
        }
    }

    public static class QueueExtensions
    {
        public static IEnumerable<OutgoingMessage> DequeueTake(this Queue<OutgoingMessage> queue, int count)
        {
            var takenCount = 0;

            while (queue.Count != 0 && takenCount < count)
            {
                yield return queue.Dequeue();
                takenCount += 1;
            }
        }
    }
}