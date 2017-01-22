using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEventListener
{
    class Program
    {
        static void Main(string[] args)
        {
            string connection = ConfigurationManager.AppSettings["ServiceConnection"];
            string consumerGroupName = "$Default";
            string deviceName = "PC";
            EventHubClient eventHubClient = null;
            EventHubReceiver eventHubReceiver = null;

            eventHubClient = EventHubClient.CreateFromConnectionString(connection, "messages/events");
            var ri = eventHubClient.GetRuntimeInformation();
            if (deviceName != "")
            {
                string partition = EventHubPartitionKeyResolver.ResolveToPartition(deviceName, ri.PartitionCount);
                eventHubReceiver = eventHubClient.GetConsumerGroup(consumerGroupName).
                    CreateReceiver(partition, DateTime.Now);
                Console.WriteLine($"{deviceName} - {partition}");
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
                    foreach (var item in edata.SystemProperties)
                    {
                        Console.Write($"{item.Key}, {item.Value.ToString()}");
                    }
                    Console.WriteLine($"\r\n{data}");
                }
            }
        }

    }
}
