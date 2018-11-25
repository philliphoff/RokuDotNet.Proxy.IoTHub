using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RokuDotNet.Proxy.Mqtt
{
    public sealed class IoTHubRokuRpcClient : IRokuRpcClient
    {
        private readonly string connectionString;

        public IoTHubRokuRpcClient(string connectionString)
        {
            this.connectionString = connectionString;
        }

        #region IRokuRpc Members

        public async Task<T> InvokeMethodAsync<TMessagePayload, T>(string deviceId, string methodName, TMessagePayload payload, CancellationToken cancellationToken)
        {
            using (var client = ServiceClient.CreateFromConnectionString(this.connectionString))
            {
                await client.OpenAsync().ConfigureAwait(false);

                var cloudToDeviceMethod = new CloudToDeviceMethod("InvokeMethod");
                string invocationString = JsonConvert.SerializeObject(
                    new MethodInvocation
                    {
                        DeviceId = deviceId,
                        MethodName = methodName,
                        Payload = JToken.FromObject(payload)
                    });

                cloudToDeviceMethod.SetPayloadJson(invocationString);
                
                var result = await client.InvokeDeviceMethodAsync(deviceId, cloudToDeviceMethod, cancellationToken);

                string invocationResponseString = result.GetPayloadAsJson();

                await client.CloseAsync().ConfigureAwait(false);

                var invocationResponse = JsonConvert.DeserializeObject<MethodInvocationResponse>(invocationResponseString);

                return invocationResponse.Payload.ToObject<T>();
            }
        }

        #endregion
    }
}