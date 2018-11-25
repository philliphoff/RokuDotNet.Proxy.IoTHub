using System;
using System.Threading;
using System.Threading.Tasks;
using RokuDotNet.Client;
using RokuDotNet.Client.Input;
using RokuDotNet.Client.Query;
using RokuDotNet.Proxy;
using RokuDotNet.Proxy.IoTHub;
using RokuDotNet.Proxy.Mqtt;

namespace RokuDotNet.Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            string deviceName = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_ID");
            string deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");
            string serviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_SERVICE_CONNECTION_STRING");

            using (var cts = new CancellationTokenSource())
            {
                Func<string, CancellationToken, Task<IRokuDevice>> deviceMapFunc =
                    (_, __) =>
                    {
                        var discoveryClient = new UdpRokuDeviceDiscoveryClient();

                        return discoveryClient.DiscoverFirstDeviceAsync();
                    };

                var handler = new RokuRpcServerHandler(deviceMapFunc);
                var server = new IoTHubRokuRpcServer(deviceConnectionString, handler);

                await server.StartListeningAsync(cts.Token);

                try
                {
                    var rpcClient = new IoTHubRokuRpcClient(serviceConnectionString);

                    using (var deviceClient = new ProxyRokuDevice("test", rpcClient))
                    {
                        var result = await deviceClient.Query.GetTvChannelsAsync(cts.Token);

                        foreach(var channel in result.Channels)
                        {
                            Console.WriteLine(channel.Name);
                        }
                    }
                }
                finally
                {
                    await server.StopListeningAsync(cts.Token);
                }

                Console.WriteLine("Press <ENTER> to quit...");
                Console.ReadLine();
            }
        }
    }
}
