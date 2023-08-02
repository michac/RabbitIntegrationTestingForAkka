using System;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace RabbitIntegrationTestingForAkka
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            // goal: run akka.net actor system as a windows service
            // goal: connect to rabbit mq using Akka streams connector, and make it work with akka.net actor system
            
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddWindowsService();

            builder.Services.AddAkka("playground", c => { });

            builder.Services.AddSingleton<RabbitStreamProvider>();
            builder.Services.AddHostedService<RabbitStreamService>();
            builder.Services.AddHostedService<SampleWorkerService>();

            var host = builder.Build();
            
            host.Run();
        }
    }
}