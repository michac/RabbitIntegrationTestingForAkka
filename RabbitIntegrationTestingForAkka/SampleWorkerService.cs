using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.Hosting;

namespace RabbitIntegrationTestingForAkka
{
    public class SampleWorkerService : BackgroundService
    {
        private readonly RabbitStreamProvider _provider;

        public SampleWorkerService(RabbitStreamProvider provider)
        {
            _provider = provider;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var inbox = await _provider.GetRabbitInbox();
            var outbox = await _provider.GetRabbitOutbox();

#pragma warning disable CS4014 // Want this to run in background until canceled
            inbox.ListenAsync(stoppingToken);
#pragma warning restore CS4014

            inbox.MessageReceived += (sender, message) =>
            {
                Console.WriteLine($"message received, \"{message.Message}\"");
            };

            var counter = 1;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var message = $"This is message #{counter}";
                outbox.Publish(message);
                Console.WriteLine($"message sent, \"{message}\"");
                counter += 1;
                
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}