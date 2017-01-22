using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Azure.Devices.Shared;
using System.Threading;

namespace WpfPC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region misc
        public MainWindow()
        {
            m_devConnection = ConfigurationManager.AppSettings["DevConnection"];
            m_dev1Connection = ConfigurationManager.AppSettings["Dev1Connection"];
            m_ownerConnection = ConfigurationManager.AppSettings["OwnerConnection"];
            m_serviceConnection = ConfigurationManager.AppSettings["ServiceConnection"];
            InitializeComponent();
        }
        string m_ownerConnection, m_devConnection, m_dev1Connection, m_serviceConnection;
        #endregion
        #region Info and registration
        private async void cmd02GetPCInfo_Click(object sender, RoutedEventArgs e)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(m_ownerConnection);

            Device pc = await rm.GetDeviceAsync("PC");
            Debug.WriteLine($"Authentication.SymmetricKey.PrimaryKey: {pc.Authentication.SymmetricKey.PrimaryKey}");
            Debug.WriteLine($"CloudToDeviceMessageCount: {pc.CloudToDeviceMessageCount}");
            Debug.WriteLine($"ConnectionState: {pc.ConnectionState}");
            Debug.WriteLine($"ConnectionStateUpdatedTime: {pc.ConnectionStateUpdatedTime}");
            Debug.WriteLine($"ETag: {pc.ETag}");
            Debug.WriteLine($"GenerationId: {pc.GenerationId}");
            Debug.WriteLine($"Id: {pc.Id}");
            Debug.WriteLine($"LastActivityTime: {pc.LastActivityTime}");
            Debug.WriteLine($"Status: {pc.Status}");
            Debug.WriteLine($"StatusReason: {pc.StatusReason}");
            Debug.WriteLine($"StatusUpdatedTime: {pc.StatusUpdatedTime}");

            var twins = await rm.GetTwinAsync("PC");
            Debug.WriteLine($"Twins, JSON:\r\n{twins.ToJson(Newtonsoft.Json.Formatting.Indented)}");


        }

        private async void cmd01RegisterNewDevice_Click(object sender, RoutedEventArgs e)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(m_ownerConnection);

            Device[] devArr = new Device[10];
            for (int i = 0; i < 10; i++)
            {
                devArr[i] = new Device($"wpf{i}");
            }
            foreach (var item in await rm.GetDevicesAsync(1000))
            {
                if (item.Id.StartsWith("wpf")) await rm.RemoveDevices2Async(new Device[] { item });
            }

            var resultAdd = await rm.AddDevices2Async(devArr);
            foreach (var item in resultAdd.Errors)
            {
                Debug.WriteLine(item);
            }

            //SAS
            var builder = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(m_ownerConnection);
            Device pc = await rm.GetDeviceAsync("PC");
            var sasBuilder = new SharedAccessSignatureBuilder()
            {
                Key = pc.Authentication.SymmetricKey.PrimaryKey,
                Target = String.Format("{0}/devices/{1}", builder.HostName, WebUtility.UrlEncode(pc.Id)),
                TimeToLive = TimeSpan.FromDays(5)
            };

            string sas = $"HostName={builder.HostName};DeviceId={pc.Id};SharedAccessSignature={sasBuilder.ToSignature()}";
            Debug.WriteLine($"SAS: {sas}");

        }
        #endregion
        #region Send
        private async void cmd03SendTelemetryAMQP_Click(object sender, RoutedEventArgs e)
        {
            DeviceClient dc = DeviceClient.CreateFromConnectionString(m_devConnection,
                Microsoft.Azure.Devices.Client.TransportType.Amqp);
            await sendMessages(dc);
            await dc.CloseAsync();
            Debug.WriteLine("cmd03SendTelemetryAMQP_Click");
        }

        private static async Task sendMessages(DeviceClient dc)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 20; i++)
            {
                await dc.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(
                    UTF8Encoding.UTF8.GetBytes($"Komunikat: {i}")));
            }
            var ms = sw.ElapsedMilliseconds;
            Console.WriteLine($"Time: {ms}");
            //Debugger.Break();
        }

        private async void cmd04SendTelemetryMQTT_Click(object sender, RoutedEventArgs e)
        {
            DeviceClient dc = DeviceClient.CreateFromConnectionString(m_devConnection,
    Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            await sendMessages(dc);
            await dc.CloseAsync();
            Debug.WriteLine("cmd04SendTelemetryMQTT_Click");
        }

        private async void cmd05SendTelemetryHTTP_Click(object sender, RoutedEventArgs e)
        {
            DeviceClient dc = DeviceClient.CreateFromConnectionString(m_devConnection,
Microsoft.Azure.Devices.Client.TransportType.Http1);
            await sendMessages(dc);
            await dc.CloseAsync();
            Debug.WriteLine("cmd05SendTelemetryHTTP_Click");
        }

        private async void cmd06SendTelemetryBlob_Click(object sender, RoutedEventArgs e)
        {
            byte[] arr = new byte[50000];
            arr[0] = 255;
            arr[arr.Length - 1] = 255;
            var stream = new MemoryStream(arr);
            DeviceClient dc = DeviceClient.CreateFromConnectionString(m_devConnection,
Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            string name = $"cmd06SendTelemetryBlob_Click-{DateTime.UtcNow:yyyyMMddHHmmsss}";
            await dc.UploadToBlobAsync(name,
                stream);
            //Maybe - send message that new blob arrive :)
            await dc.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(UTF8Encoding.UTF8.GetBytes(name)));
            await dc.CloseAsync();
            Debug.WriteLine("cmd06SendTelemetryBlob_Click");
        }
        #endregion
        #region Messages
        private async void cmd07SubscribeAndWaitForMessage_Click(object sender, RoutedEventArgs e)
        {
            DeviceClient dc = DeviceClient.CreateFromConnectionString(m_devConnection,
Microsoft.Azure.Devices.Client.TransportType.Amqp); //MQTT - ok, but no AbadonAsync
            await dc.OpenAsync();
            Debug.WriteLine("Waiting for message!");
            bool end = false;
            while (!end)
            {
                var msg = await dc.ReceiveAsync(TimeSpan.FromSeconds(500));
                if (msg != null)
                {
                    Debug.WriteLine($"{msg.MessageId} - {Encoding.ASCII.GetString(msg.GetBytes())}");
                    if (msg.DeliveryCount > 1)
                    {
                        await dc.CompleteAsync(msg);
                        end = true;
                        Debug.WriteLine($"{msg.MessageId} - OK");

                    }
                    else
                    {
                        await dc.AbandonAsync(msg); //Not working in MQTT (due to protocol characteristics)
                        // dc.RejectAsync - doszło ale odrzycamy
                        Debug.WriteLine($"{msg.MessageId} - AbadonAsync");
                    }
                }
            }

        }
        #endregion
        #region Call Method
        static DeviceClient dcPC, dcPC1;
        private async void cmd08SubscribeAndWaitForMethod_Click(object sender, RoutedEventArgs e)
        {
            removeAndCloseClientDevicesConnections();
            dcPC = DeviceClient.CreateFromConnectionString(m_devConnection,
Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            dcPC.SetMethodHandler("m1", new MethodCallback(callback_m1), new byte[] { 1 });
            dcPC.SetMethodHandler("m2", new MethodCallback(callback_m2), new byte[] { 2 });

            //Second "device"
            dcPC1= DeviceClient.CreateFromConnectionString(m_dev1Connection,
Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            dcPC1.SetMethodHandler("m1", new MethodCallback(callback_m1), new byte[] { 3 });
            dcPC1.SetMethodHandler("m2", new MethodCallback(callback_m2), new byte[] { 4 });

            await dcPC1.SetDesiredPropertyUpdateCallback(devicePC1_configupdate, null);

            Debug.WriteLine("cmd08SubscribeAndWaitForMethod_Click");

        }

        private void cmd08Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            removeAndCloseClientDevicesConnections();
            Debug.WriteLine("cmd08Unsubscribe_Click");
        }

        private void removeAndCloseClientDevicesConnections()
        {
            if (dcPC != null)
            {
                dcPC.CloseAsync();
                dcPC = null;
            }
            if (dcPC1 != null)
            {
                dcPC1.CloseAsync();
                dcPC1 = null;
            }
        }

        private async void cmd09CallMethod_Click(object sender, RoutedEventArgs e)
        {
            ServiceClient sc = ServiceClient.CreateFromConnectionString(m_serviceConnection);
            CloudToDeviceMethod cdm = new CloudToDeviceMethod("m1"/*,responseTimeout,connectionTimeout*/);
            JObject jo = new JObject();
            jo.Add("Param1", 12);
            jo.Add("Param2", "AA");
            cdm.SetPayloadJson(jo.ToString());
            var result = await sc.InvokeDeviceMethodAsync("PC", cdm);
            Debug.WriteLine($"{result.GetPayloadAsJson()}");
        }

        private async Task<MethodResponse> callback_m1(MethodRequest methodRequest, object userContext)
        {
            var str = UTF8Encoding.UTF8.GetString(methodRequest.Data);
            Debug.WriteLine($"--------DEVICE------\r\nName: {methodRequest.Name}, Data: {str}");
            return new MethodResponse(UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { State = true, Msg = "OK" }))
                , 0);
        }

        private async Task<MethodResponse> callback_m2(MethodRequest methodRequest, object userContext)
        {
            var str = UTF8Encoding.UTF8.GetString(methodRequest.Data);
            Debug.WriteLine($"--------DEVICE------\r\nName: {methodRequest.Name}, Data: {str}");
            return new MethodResponse(UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { State = false, Msg = "Bad!" }))
                , -1);
        }
        #endregion
        #region Twins

        private async void cmd10WorkWithTwins_Click(object sender, RoutedEventArgs e)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(m_ownerConnection);
            var pc = await rm.GetDeviceAsync("PC");
            var pc1 = await rm.GetDeviceAsync("PC1");
            Debug.WriteLine($"PC: {pc.ConnectionState}");
            Debug.WriteLine($"PC1: {pc1.ConnectionState}");
            if (pc.ConnectionState != DeviceConnectionState.Connected || pc1.ConnectionState != DeviceConnectionState.Connected) return;
            Debug.WriteLine("Connected!");
            var pcTwin = await rm.GetTwinAsync("PC");
            dumpTwin(pcTwin);
            var pc1Twin = await rm.GetTwinAsync("PC1");
            dumpTwin(pc1Twin);

            var patchPC = new
            {
                properties = new
                {
                    desired = new
                    {
                        newerBeUpdatedFromDesiredToReporting = true,
                    }
                },
                tags = new
                {
                    devicetype = "PC",
                    location = "ROOF"
                }
            };
            await rm.UpdateTwinAsync("PC", JsonConvert.SerializeObject(patchPC), pcTwin.ETag);
            var patchPC1 = new
            {
                properties = new
                {
                    desired = new
                    {
                        configUpdated = DateTime.UtcNow,
                        disabled = false
                    }
                },
                tags = new
                {
                    devicetype = "PC",
                    location = "GARAGE"
                }
            };

            await rm.UpdateTwinAsync("PC1", JsonConvert.SerializeObject(patchPC1),pc1Twin.ETag);
            Thread.Sleep(10000); //Wait
            var query = rm.CreateQuery("SELECT * FROM devices WHERE deviceId = 'PC1'");
            var results = await query.GetNextAsTwinAsync();
            foreach (var result in results)
            {
                dumpTwin(result);
            }

            query = rm.CreateQuery("SELECT * FROM devices WHERE tags.devicetype = 'PC'");
            results = await query.GetNextAsTwinAsync();
            foreach (var result in results)
            {
                dumpTwin(result);
            }

            query = rm.CreateQuery("SELECT * FROM devices WHERE properties.MyProp = '123'");
            results = await query.GetNextAsTwinAsync();
            foreach (var result in results)
            {
                dumpTwin(result);
            }
        }

        private static async Task devicePC1_configupdate(TwinCollection desiredProperties, object userContext)
        {
            //ON DEVICE PC1
            Debug.WriteLine("--------DEVICE------\r\nPC1 - devicePC1_configupdate");
            TwinCollection reportedProperties = new TwinCollection();
            foreach (dynamic item in desiredProperties)
            {
                Debug.WriteLine($"PC1 - {item}");
                reportedProperties[item.Key] = item.Value;
            }
            reportedProperties["MyProp"] = 123;
            await dcPC1.UpdateReportedPropertiesAsync(reportedProperties);
            //await dcPC1.SetDesiredPropertyUpdateCallback(devicePC1_configupdate, null);

            return;
        }

        private void dumpTwin(Twin t)
        {
            Debug.WriteLine($"-----\r\n{t.DeviceId}, {t.ETag}\r\n>Desired");
            foreach (var item in t.Properties.Desired)
            {
                Debug.Write($"{item},");
            }
            Debug.WriteLine("\r\n>Reported");
            foreach (var item in t.Properties.Reported)
            {
                Debug.Write($"{item},");
            }
            Debug.WriteLine("\r\n>Tags");
            foreach (var item in t.Tags)
            {
                Debug.Write($"{item},");
            }
            Debug.WriteLine("");

        }
        #endregion
        #region sheduledtask
        private async void cmd11ScheduledUpdate_Click(object sender, RoutedEventArgs e)
        {
            removeAndCloseClientDevicesConnections();
            dcPC = DeviceClient.CreateFromConnectionString(m_devConnection,
Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            dcPC.SetMethodHandler("update", new MethodCallback(update_pc), new byte[] { 1 });

            //Second "device"
            dcPC1 = DeviceClient.CreateFromConnectionString(m_dev1Connection,
Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            dcPC1.SetMethodHandler("update", new MethodCallback(update_pc1), new byte[] { 3 });

            RegistryManager rm = RegistryManager.CreateFromConnectionString(m_ownerConnection);
            JobClient jc = JobClient.CreateFromConnectionString(m_ownerConnection);
            JobResponse result;
            string jobId;
            //Update Tags
            jobId = Guid.NewGuid().ToString();
            var twin = new Twin();
            twin.Tags["Virtual"] = true;
            twin.ETag = "*";
            result = await jc.ScheduleTwinUpdateAsync(jobId,
                "not is_defined(tags.abc)", //not is_defined(tags.virtual)
                twin,
                DateTime.Now,
                100);
            await MonitorJob(jc, jobId);

            jobId = Guid.NewGuid().ToString();
            CloudToDeviceMethod directMethod = new CloudToDeviceMethod("update", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            directMethod.SetPayloadJson("{\"url\":\"http://www.cos\"}");
            //var query = rm.CreateQuery("SELECT * FROM devices WHERE tags.devicetype = 'PC'");
            result = await jc.ScheduleDeviceMethodAsync(jobId,
                "tags.devicetype = 'PC'", //Another
                directMethod,
                DateTime.Now,
                10);
            await MonitorJob(jc, jobId);
        }

        public static async Task MonitorJob(JobClient jc, string jobId)
        {
            Debug.WriteLine($"{jobId}");
            JobResponse result;
            do
            {
                result = await jc.GetJobAsync(jobId);
                Debug.WriteLine($"Job Status : {result.Status}");
                Thread.Sleep(2000);
            } while ((result.Status != JobStatus.Completed) && (result.Status != JobStatus.Failed));
            Debug.WriteLine(
                $"QueryCondition: {result.QueryCondition} \r\n" +
                $"Result:         {result.Status}, {result?.FailureReason} \r\n" +
                $"DeviceCount:    {result.DeviceJobStatistics?.DeviceCount} \r\n" +
                $"FailedCount:    {result.DeviceJobStatistics?.FailedCount} \r\n" +
                $"PendingCount:   {result.DeviceJobStatistics?.PendingCount} \r\n" +
                $"RunningCount:   {result.DeviceJobStatistics?.RunningCount} \r\n" +
                $"SucceededCount: {result.DeviceJobStatistics?.SucceededCount} \r\n"



                );
        }

        private async Task<MethodResponse> update_pc1(MethodRequest methodRequest, object userContext)
        {
            Debug.WriteLine($"--------DEVICE------\r\nPC1: {methodRequest.Name}, {methodRequest.DataAsJson}");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["FVUPDATE"] = DateTime.UtcNow;
            await dcPC1.UpdateReportedPropertiesAsync(reportedProperties);
            return new MethodResponse(UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Msg = "FVUPDATE" }))
                , 0);
        }

        private async Task<MethodResponse> update_pc(MethodRequest methodRequest, object userContext)
        {
            Debug.WriteLine($"--------DEVICE------\r\nPC: {methodRequest.Name}, {methodRequest.DataAsJson}");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["FVUPDATE"] = DateTime.UtcNow;
            await dcPC.UpdateReportedPropertiesAsync(reportedProperties);
            return new MethodResponse(UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Msg = "FVUPDATE" }))
                , 0);
        }

        #endregion
    }
}
