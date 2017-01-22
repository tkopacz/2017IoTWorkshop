using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TKConsoleSend
{
    class Program
    {
        static Random rnd = new Random();
        const int NODEVICES = 21;
        const int NOMESSAGES = 200;
        const int PROGRESS = 10;
        const int DELAYMS = 500;
        static void Main(string[] args)
        {
            string ownerconnection = ConfigurationManager.AppSettings["OwnerConnection"];
            string devConnection = ConfigurationManager.AppSettings["DevConnection"];
            var builder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(ownerconnection);
            var devClient =
                DeviceClient.CreateFromConnectionString(
                    devConnection,
                    Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only
                    );

            //Parallel.For(1, 20, async (i) =>
            //{
            //    await devClient.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(new byte[] { (byte)i, 2, 3 }));
            //});

            var result = Parallel.For(1, NODEVICES, async (i) =>
                  {
                      Console.WriteLine($"demo{i} - START");
                      RegistryManager rm = RegistryManager.CreateFromConnectionString(ownerconnection);
                      //await rm.RemoveDeviceAsync("demo1");
                      Device d = await rm.GetDeviceAsync($"demo{i}");
                      if (d == null)
                      {
                          await rm.AddDeviceAsync(new Device($"demo{i}"));
                      }
                      d = await rm.GetDeviceAsync($"demo{i}");
                      string connection = $"HostName={builder.HostName};DeviceId={d.Id};SharedAccessKey={d.Authentication.SymmetricKey.PrimaryKey}";
                      var deviceClient =
                          DeviceClient.CreateFromConnectionString(
                              connection,
                              Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only
                              );

                      Console.WriteLine($"demo{i} - SENDING");
                      for (byte j = 0; j < NOMESSAGES; j++)
                      {
                          DateTime now = DateTime.Now;
                          TimeSpan ts = new TimeSpan(now.Year, now.Minute, now.Second, now.Millisecond);
                          MAllNum mallnum = new MAllNum()
                          {
                              DeviceName = d.Id,
                              Light = 1000 * Math.Cos(ts.TotalMilliseconds / 175) * Math.Sin(ts.TotalMilliseconds / 360) + rnd.Next(30),
                              Potentiometer1 = 1000 * Math.Cos(ts.TotalMilliseconds / 275) * Math.Sin(ts.TotalMilliseconds / 560) + rnd.Next(30),
                              Potentiometer2 = 1000 * Math.Cos(ts.TotalMilliseconds / 60) * Math.Sin(ts.TotalMilliseconds / 120) + rnd.Next(30),
                              Pressure = (float)(1000 * Math.Cos(ts.TotalMilliseconds / 180) * Math.Sin(ts.TotalMilliseconds / 110) + rnd.Next(30)),
                              Temperature = (float)(1000 * Math.Cos(ts.TotalMilliseconds / 180) * Math.Sin(ts.TotalMilliseconds / 110) + rnd.Next(30)),
                              ADC3 = rnd.Next(1000),
                              ADC4 = rnd.Next(1000),
                              ADC5 = rnd.Next(1000),
                              ADC6 = rnd.Next(1000),
                              ADC7 = rnd.Next(1000),
                              Altitude = (float)(1000 * Math.Cos(ts.TotalMilliseconds / 480) * Math.Sin(ts.TotalMilliseconds / 2000) + rnd.Next(30)),
                          };
                          var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mallnum)));
                          if (rnd.NextDouble() > 0.8) { message.Properties.Add("direction", "eventhub"); }
                          if (rnd.NextDouble() > 0.9) { message.Properties.Add("status", "error"); }
                          await deviceClient.SendEventAsync(
                                  message);
                          if (j% PROGRESS == 0)
                          {
                              Console.WriteLine($"demo{i} - {j}");
                          }
                          await Task.Delay(DELAYMS);
                      }
                      Console.WriteLine($"demo{i} - END");
                  }
             );
            Console.WriteLine("END");
            Console.ReadLine();
        }
    }




    public class MIoTBase
    {
        public DateTime Dt { get; } = DateTime.Now;
        public string MsgType { get; set; }
        public string DeviceName { get; set; }
        protected MIoTBase(string msgType)
        {
            Dt = DateTime.Now;
            MsgType = msgType;
        }
    }
    public class MSPI : MIoTBase
    {
        public double Potentiometer1 { get; set; }
        public double Potentiometer2 { get; set; }
        public double Light { get; set; }
        public MSPI() : base("MSPI") { }
        public MSPI(string msgType) : base(msgType) { }
    }
    public class MAllNum : MSPI
    {
        public double ADC3 { get; internal set; }
        public double ADC4 { get; internal set; }
        public double ADC5 { get; internal set; }
        public double ADC6 { get; internal set; }
        public double ADC7 { get; internal set; }
        public float Altitude { get; internal set; }
        public float Pressure { get; internal set; }
        public float Temperature { get; internal set; }
        public MAllNum() : base("MAllNum") { }
    }
}
