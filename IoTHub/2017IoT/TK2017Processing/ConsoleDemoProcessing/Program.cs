using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleDemoProcessing
{
    class Program
    {
        static void Main(string[] args)
        {
                string connection = "HostName=pltkdpepliot2016S1.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=Q2Lib3zRmGtHYWJMogofGWbhwrBCpGmemnTvXOWj+qE=";
                string consumerGroupName = "$Default";
                string deviceName = "";
                EventHubClient eventHubClient = null;
                EventHubReceiver eventHubReceiver = null;

                eventHubClient = EventHubClient.CreateFromConnectionString(connection, "messages/events");
                var ri = eventHubClient.GetRuntimeInformation();
                if (deviceName != "")
                {
                    string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, ri.PartitionCount);
                    eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).
                        CreateReceiver(partition, DateTime.Now);
                    Task.Run(() => EventLoopAsync(eventHubReceiver));
                }
                else
                {
                    EventHubReceiver[] eventHubReceivers = new EventHubReceiver[ri.PartitionCount];
                    Console.WriteLine($"PartitionCount: {ri.PartitionCount}");

                    int i = 0;
                    foreach (var partition in ri.PartitionIds)
                    {
                        Console.WriteLine($"PartitionID: {partition}");
                        eventHubReceivers[i] = eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, DateTime.Now);
                        //Task.Run(() => eventLoop(eventHubReceivers[i])); <- very common bug!
                        var r = eventHubReceivers[i];
                        Task.Run(() => EventLoopAsync(r));
                        i++;
                    }

                }
                Console.ReadLine();
            }

        private static async Task EventLoopAsync(EventHubReceiver eventHubReceiver)
        {
            while (true)
            {
                var edata = await eventHubReceiver.ReceiveAsync();
                if (edata != null)
                {
                    var data = Encoding.UTF8.GetString(edata.GetBytes());
                    Console.WriteLine($"{eventHubReceiver.PartitionId}, {data}");
                }
            }
        }
    }
}
