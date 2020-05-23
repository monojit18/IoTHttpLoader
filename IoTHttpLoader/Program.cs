using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using AZD = Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace IoTHttpLoader
{
    class Program
    {

        private static int kCounter = 0;
        private static List<string> kDeviceConnectionList = null;
        private static List<AZD.Device> kDevicesList = null;
        private static AZD.RegistryManager kRegistryManager = null;

        private static string kIoTHubConnectionString = "HostName=hub32.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=/wd70w8VWU5n7ayUIpzGJhLEWWqZn+Qwek1li0Y+kMk=";
        private static string kDeviceConnectionString = "HostName=hub32.azure-devices.net;DeviceId={0};SharedAccessKey={1}";

        //private static string kIoTHubConnectionString = "HostName=iotHubREL.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=lrEBA8Urryr/jebf5JKqeeio9rGDXGr/V/FVCQSQbCE=";
        //private static string kDeviceConnectionString = "HostName=iotHubREL.azure-devices.net;DeviceId={0};SharedAccessKey={1}";

        private static int kTotalDevices = 32;
        private static int kDevicePageSize = kTotalDevices;
        private static CancellationTokenSource kTokenSource = new CancellationTokenSource();
        private static SemaphoreSlim kSemaphoreParallel = new SemaphoreSlim(kTotalDevices);
        private static SemaphoreSlim kSemaphoreWait = new SemaphoreSlim(0);        

        private static IEnumerable<bool> Infinite()
        {

            while (true)
            {

                yield return true;
            }


        }

        private static async Task PrepareTestAsync()
        {

            kRegistryManager = AZD.RegistryManager.CreateFromConnectionString(kIoTHubConnectionString);
            kDeviceConnectionList = new List<string>();
            kDevicesList = new List<AZD.Device>();

            do
            {

                var devicesList = (await kRegistryManager.GetDevicesAsync(kDevicePageSize)).ToList();
                kDevicesList.AddRange(devicesList);

                if (kDevicesList.Count == kTotalDevices)
                    break;


            } while (true);

            for (int idx = 0; idx < kDevicesList.Count; ++idx)
            {

                try
                {

                    var device = kDevicesList[idx];
                    var connString = device.Authentication.SymmetricKey.PrimaryKey;
                    connString = string.Format(kDeviceConnectionString, device.Id, connString);
                    Console.WriteLine(connString);

                    kDeviceConnectionList.Add(connString);

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);

                }

            }
            
        }

        private static IoTHubClient CreateHttpClient()
        {

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("iotclient", (HttpClient httpClient) =>
            {

                //httpClient.DefaultRequestHeaders.Add("Connection", "Close");

            }).AddTypedClient<IoTHubClient>();

            //    .ConfigurePrimaryHttpMessageHandler(() =>
            //{

            //    var handler = new HttpClientHandler()
            //    {

            //        ServerCertificateCustomValidationCallback =
            //        (httpRequestMessage, cert, cetChain, policyErrors) =>
            //        {
            //            return true;
            //        }

            //    };

            //    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            //    return handler;

            //}).AddTypedClient<IoTHubClient>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var iotHubClient = serviceProvider.GetRequiredService<IoTHubClient>();
            return iotHubClient;

        }

        private static async Task SendDeviceToCloudMessagesAsync(string messageString,
                                                                 string connString,
                                                                 CancellationToken
                                                                 cancellationToken)
        {

            var httpClient = CreateHttpClient();
            await httpClient.SendDeviceToCloudMessagesAsync(messageString, connString,
                                                            cancellationToken);
        }

        private static async Task LoadTestAsync()
        {
            var opt = new ParallelOptions()
            {

                MaxDegreeOfParallelism = kTotalDevices

            };

            await Task.Run(() =>
            {

                //while(true)
                //{

                    kCounter = 0;
                    //var lst = Enumerable.Range(0, 5000000).ToList<int>();
                    //var tskArray = lst.Select(async (int indx) =>
                    Parallel.ForEach(Infinite(), opt, async (bool bl) =>                    
                    {

                        if (kCounter > kTotalDevices)
                            kCounter = 0;

                        var idx = (kCounter++) % kTotalDevices;
                        var connString = kDeviceConnectionList[idx];

                        var model = new HttpMessage()
                        {

                            Data = DateTime.UtcNow.ToLongTimeString()

                        };

                        var messageString = JsonConvert.SerializeObject(model);
                        await kSemaphoreParallel.WaitAsync();
                        await SendDeviceToCloudMessagesAsync(messageString, connString,
                                                             kTokenSource.Token);
                        kSemaphoreParallel.Release();                    

                    });

                    //await Task.WhenAll(tskArray);                    
                    //await Task.Delay(10);                              

            });

            await kSemaphoreWait.WaitAsync();
            Console.WriteLine($"Finally:{DateTime.UtcNow}-{kCounter}");

        }

        static async Task Main(string[] args)
        {

            Console.WriteLine("Hello IoTHttp Loader!");
            await PrepareTestAsync();
            await LoadTestAsync();
            


        }
    }
}
