using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKConsoleServiceSend
{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceconnection = ConfigurationManager.AppSettings["ServiceConnection"];
            Task.Run(async () =>
            {
                //Microsoft.Azure.Devices.Client
                var serviceClient = ServiceClient.CreateFromConnectionString(serviceconnection);

                for (int i = 0; i < 10; i++)
                {
                    await serviceClient.SendAsync("PC", new Message(new byte[] { 1, 2, 3 }));
                }
                Console.WriteLine("SENDED");
                Console.ReadLine();
            }).Wait();

        }
    }
}
