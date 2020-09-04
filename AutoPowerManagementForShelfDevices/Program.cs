using System;
using System.Runtime.InteropServices;
using AutoPowerManagementForShelfDevices.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoPowerManagementForShelfDevices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("This service can only run on windows");

                return;
            }

            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
                services.AddSingleton<PowerManagementStateMachine>();
                services.AddSingleton<Lid>();
                services.AddSingleton<NetworkAdapters>();
            });

            hostBuilder.ConfigureLogging(loggerFactory => loggerFactory.AddEventLog());
            hostBuilder.UseWindowsService();

            return hostBuilder;
        }
    }
}