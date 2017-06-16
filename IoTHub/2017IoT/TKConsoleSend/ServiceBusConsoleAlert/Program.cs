using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBusConsoleAlert
{
    class Program
    {
        static SubscriptionClient m_clientAll = SubscriptionClient.Create("importantiotmessages", "all");
        static void Main(string[] args)
        {
            Task.Run(() => processAll());
            Console.WriteLine("Enter = End");
            Console.ReadLine();

        }
        static async void processAll()
        {
            while (true)
            {
                var msg = await m_clientAll.ReceiveAsync();
                if (msg != null)
                {
                    Console.WriteLine($"ALERT: {msg.MessageId}");
                    await m_clientAll.CompleteAsync(msg.LockToken); //Yes, we processed "ALERT"
                }
            }
        }

    }
}
