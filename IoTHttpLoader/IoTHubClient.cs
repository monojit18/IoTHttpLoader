using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace IoTHttpLoader
{
    public class IoTHubClient
    {

        private static HttpClient _httpClient;
        //private static string kMqttAPiURLString = "http://akswkshpfe.eastus.cloudapp.azure.com/iot/mqtt/api";
        private static string kMqttAPiURLString = "http://52.142.36.139/iot/mqtt/api";
        //private static string kMqttAPiURLString = "https://iotloadfunc.azurewebsites.net/api/SendMqttApp";

        public IoTHubClient(HttpClient httpClient)
        {
            if (_httpClient == null)
                _httpClient = httpClient;

            // Console.WriteLine(_httpClient.GetHashCode());
            
        }      

        public async Task SendDeviceToCloudMessagesAsync(string messageString,
                                                                 string connString,
                                                                 CancellationToken cancellationToken)
        {

            try
            {

                var content = new StringContent(messageString, Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage()
                {

                    RequestUri = new Uri(kMqttAPiURLString),
                    Method = HttpMethod.Post,
                    Headers =
                    {

                        { "conn", connString }

                    },
                    Content = content

                };


                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                var result = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"{result}-{response.StatusCode}-{DateTime.UtcNow.ToLongTimeString()}");                

                
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Message:{ex.Message}");

            }



        }

    }
}
