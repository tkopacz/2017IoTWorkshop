using Microsoft.Azure.Management.DataLake.Analytics;
using Microsoft.Azure.Management.DataLake.Analytics.Models;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.StoreUploader;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TK_SendUSQLAndDownloadResult {
    class Program {
        static void Main(string[] args) {
            DataLakeProcess dpl = new DataLakeProcess();
            dpl.Login();
            Guid jobId = dpl.SubmitJobByPath(@"C:\AzureFY15TK\18aHDInsight2016\02 Data Lake\tkdemo.mydatalake\TK_SendUSQLAndDownloadResult\TK_SendUSQLAndDownloadResult\script.txt", "script.txt");
            dpl.WaitForJob(jobId);
            dpl.DownloadFile(@"/Output/SearchLog-from-Data-Lake.csv", @"C:\AzureFY15TK\18aHDInsight2016\02 Data Lake\tkdemo.mydatalake\TK_SendUSQLAndDownloadResult\TK_SendUSQLAndDownloadResult\scriptOutput.txt");
            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
    class DataLakeProcess {
        private string _adlsAccountName;
        private string _adlaAccountName;
        private static DataLakeStoreAccountManagementClient _adlsClient;
        private static DataLakeStoreFileSystemManagementClient _adlsFileSystemClient;
        private IProgress<UploadProgress> progress;
        private DataLakeAnalyticsJobManagementClient _adlaJobClient;

        public void Login() {
            try {
                _adlsAccountName = "tkdemo"; // TODO: Replace this value with the name for a NEW Store account.
                _adlaAccountName = "tkdemo";
                TokenCredentials tokenCreds;
                // Authenticate the user
                //tokenCreds = AuthenticateUser("common", "https://management.core.windows.net/",
                //"3a7f2922-6ad2-43bb-add4-159886bff23e", new Uri("http://tkopaczDataLakeStoreADLAApp")).Result; // TODO: Replace bracketed values.

                //Authenitcate app - using SPN
                tokenCreds = AuthenticateApplication("microsoft.onmicrosoft.com", "https://management.core.windows.net/",
                   "2d03d99e-e95a-4655-8e8c-9a5f9f406ab6", new Uri("https://PLTKAzureSPN"),
                                                                                                                                                                                                                                                                                  "c7T7AM5D+9uNrEkgK1m4q30jqqCo0viL32bgO0x39cI="
                   ).Result;
                setupClients(tokenCreds, "ae7513ac-36d5-4635-b466-e2230009cc8b"); // TODO: Replace bracketed value.
            } catch (Exception ex) {
                Console.WriteLine(ex);
                Console.ReadLine();
                throw;
            }
        }

        public void Test() {
            try {
                string name = "_test_do_skasowania" + DateTime.Now.ToString("yyyyMMddhhmmsss") + Guid.NewGuid().ToString() + " _";
                _adlsFileSystemClient.FileSystem.Mkdirs(_adlsAccountName, name);
                _adlsFileSystemClient.FileSystem.Delete(_adlsAccountName, name);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                Console.ReadLine();
                throw;
            }


        }

        // Authenticate the user with AAD through an interactive popup.
        // You need to have an application registered with AAD in order to authenticate.
        //   For more information and instructions on how to register your application with AAD, see:
        //   https://azure.microsoft.com/en-us/documentation/articles/resource-group-create-service-principal-portal/
        public static async Task<TokenCredentials> AuthenticateUser(string tenantId, string resource, string appClientId, Uri appRedirectUri, string userId = "") {
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenantId);

            var tokenAuthResult = await authContext.AcquireTokenAsync(
                resource,
                appClientId,
                appRedirectUri,
                new PlatformParameters(PromptBehavior.Auto),
                UserIdentifier.AnyUser);

            return new TokenCredentials(tokenAuthResult.AccessToken);
        }

        // Authenticate the application with AAD through the application's secret key.
        // You need to have an application registered with AAD in order to authenticate.
        //   For more information and instructions on how to register your application with AAD, see:
        //   https://azure.microsoft.com/en-us/documentation/articles/resource-group-create-service-principal-portal/
        public static async Task<TokenCredentials> AuthenticateApplication(string tenantId, string resource, string appClientId, Uri appRedirectUri, String clientSecret) {
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenantId);
            var credential = new ClientCredential(appClientId, clientSecret);

            var tokenAuthResult = await authContext.AcquireTokenAsync(resource, credential);

            return new TokenCredentials(tokenAuthResult.AccessToken);
        }

        //Set up clients
        private void setupClients(TokenCredentials tokenCreds, string subscriptionId) {
            //System.NET.Http w wersji 4.0 a nie 4.1
            _adlsClient = new DataLakeStoreAccountManagementClient(tokenCreds);
            _adlsClient.SubscriptionId = subscriptionId;

            _adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(tokenCreds);
            _adlaJobClient = new DataLakeAnalyticsJobManagementClient(tokenCreds);
        }

        public void UploadFile(string srcFilePath, string destFilePath, bool force = true) {
            var ok = false;
            while (!ok) {
                try {
                    _adlsFileSystemClient.FileSystem.Mkdirs(_adlsAccountName, destFilePath);
                    var parameters = new UploadParameters(srcFilePath, destFilePath, _adlsAccountName, isOverwrite: force);
                    var frontend = new DataLakeStoreFrontEndAdapter(_adlsAccountName, _adlsFileSystemClient);
                    progress = new MyProgress();
                    var uploader = new DataLakeStoreUploader(parameters, frontend, progress);
                    uploader.Execute();
                    ok = true;
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    Login();
                }
            }
        }

        public void DownloadFile(string srcFilePath, string destFilePath) {
            var ok = false;
            while (!ok) {
                try {
                    var stream = _adlsFileSystemClient.FileSystem.Open(_adlsAccountName, srcFilePath);
                    var fileStream = new FileStream(destFilePath, FileMode.Create);
                    stream.CopyTo(fileStream);
                    fileStream.Close();
                    stream.Close();
                    ok = true;
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    Login();
                }
            }
        }
        public Guid SubmitJobByPath(string scriptPath, string jobName) {
            var script = File.ReadAllText(scriptPath);

            var jobId = Guid.NewGuid();
            var properties = new USqlJobProperties(script);
            var parameters = new JobInformation(jobName, JobType.USql, properties, priority: 1, degreeOfParallelism: 1, jobId: jobId);
            var jobInfo = _adlaJobClient.Job.Create(_adlaAccountName, jobId, parameters);
            Console.WriteLine($"{jobInfo.StartTime}");
            return jobId;
        }

        public JobResult WaitForJob(Guid jobId) {
            var jobInfo = _adlaJobClient.Job.Get(_adlaAccountName, jobId);
            while (jobInfo.State != JobState.Ended) {
                jobInfo = _adlaJobClient.Job.Get(_adlaAccountName, jobId);
                USqlJobProperties p = (USqlJobProperties)jobInfo.Properties;
                if (p != null) {
                    Console.WriteLine($"{jobInfo.Name} - {p.TotalCompilationTime} - {p.TotalPauseTime} - {p.TotalQueuedTime} - {p.TotalRunningTime}");
                }
                Thread.Sleep(1000);
            }
            return jobInfo.Result.Value;
        }

    }

    public class MyProgress : IProgress<UploadProgress> {
        public void Report(UploadProgress value) {
            Console.WriteLine($"{value.TotalFileLength}, {value.TotalSegmentCount} , {value.UploadedByteCount}");
        }
    }
}
