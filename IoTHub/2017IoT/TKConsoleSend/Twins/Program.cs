using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twins
{
    class Program
    {
        static void Main(string[] args)
        {
            string m_ownerConnection, m_devConnection, m_dev1Connection, m_serviceConnection;
            m_devConnection = ConfigurationManager.AppSettings["DevConnection"];
            m_dev1Connection = ConfigurationManager.AppSettings["Dev1Connection"];
            m_ownerConnection = ConfigurationManager.AppSettings["OwnerConnection"];
            m_serviceConnection = ConfigurationManager.AppSettings["ServiceConnection"];
            Task.Run(async () => {
                RegistryManager rm = RegistryManager.CreateFromConnectionString(m_ownerConnection);
                var mqttlora01 = await rm.GetDeviceAsync("mqttlora01");
                var mqttlora01Twin = await rm.GetTwinAsync("mqttlora01");

                var patchTags = new
                {
                    tags = new
                    {
                        devicetype = "PC",
                        location = "ROOF",
                        owner = "SKANSKA"
                    }
                };
                await rm.UpdateTwinAsync("mqttlora01", JsonConvert.SerializeObject(patchTags), mqttlora01Twin.ETag);

                mqttlora01Twin = await rm.GetTwinAsync("mqttlora01");
                Console.WriteLine(mqttlora01Twin.Tags["owner"]);
            }).Wait();
            Console.WriteLine("Enter");
            Console.ReadLine();

        }
    }
}
