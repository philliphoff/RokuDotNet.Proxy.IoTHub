using Microsoft.Azure.Devices.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace RokuDotNet.Proxy.IoTHub
{
    public sealed class IoTHubRokuRpcServer : IRokuRpcServer
    {
        private readonly string connectionString;
        private readonly IRokuRpcServerHandler handler;

        private readonly SemaphoreSlim listenLock = new SemaphoreSlim(1, 1);

        private DeviceClient deviceClient;
        
        public IoTHubRokuRpcServer(string connectionString, IRokuRpcServerHandler handler)
        {
            this.connectionString = connectionString;
            this.handler = handler;
        }

        #region IRokuRpcServer Members

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            if (this.deviceClient == null)
            {
                await this.listenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (this.deviceClient == null)
                    {
                        this.deviceClient = DeviceClient.CreateFromConnectionString(this.connectionString);

                        await this.deviceClient.SetMethodHandlerAsync("InvokeMethod", OnInvokeMethod, null).ConfigureAwait(false);

                        await this.deviceClient.OpenAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    this.listenLock.Release();
                }
            }
        }

        public async Task StopListeningAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.deviceClient != null)
            {
                await this.listenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (this.deviceClient != null)
                    {
                        await this.deviceClient.CloseAsync().ConfigureAwait(false);

                        this.deviceClient.Dispose();

                        this.deviceClient = null;
                    }
                }
                finally
                {
                    this.listenLock.Release();
                }
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.deviceClient?.Dispose();
        }

        #endregion

        private async Task<MethodResponse> OnInvokeMethod(MethodRequest request, object context)
        {
            string invocationString = request.DataAsJson;
            var invocation = JsonConvert.DeserializeObject<MethodInvocation>(invocationString);

            using (var tokenSource = new CancellationTokenSource())
            {
                var invocationResponse = await this.handler.HandleMethodInvocationAsync(invocation, tokenSource.Token).ConfigureAwait(false);
                var invocationResponseString = JsonConvert.SerializeObject(invocationResponse);

                return new MethodResponse(Encoding.UTF8.GetBytes(invocationResponseString), 0);
            }
        }
    }
}