using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoPowerManagementForShelfDevices.Interop;
using AutoPowerManagementForShelfDevices.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Stateless.Graph;

namespace AutoPowerManagementForShelfDevices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SettingsBase _settings;
        private readonly PowerManagementStateMachine _powerManagementStateMachine;
        private readonly Lid _lid;
        private readonly NetworkAdapters _networkAdapters;
        private readonly ServiceRunningStatus _serviceRunningStatus;
        private readonly IHostLifetime _hostLifetime;

        public Worker(ILogger<Worker> logger, SettingsBase settings,
            PowerManagementStateMachine powerManagementStateMachine, Lid lid,
            NetworkAdapters networkAdapters, ServiceRunningStatus serviceRunningStatus,
            IHostLifetime hostLifetime)
        {
            _logger = logger;
            _settings = settings;
            _powerManagementStateMachine = powerManagementStateMachine;
            _lid = lid;
            _networkAdapters = networkAdapters;
            _serviceRunningStatus = serviceRunningStatus;
            _hostLifetime = hostLifetime;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Load Registry Keys
            _settings.Load();

            // Check if lifetime is a windows service
            if (!(_hostLifetime is WindowsServiceLifetime))
            {
                throw new NotSupportedException(
                    $"Current Lifetime isn't supported: {_hostLifetime}. Require WindowsServiceLifetime");
            }

            // Check service running status and apply current status to state machine
            _serviceRunningStatus.InitStatus();
            _powerManagementStateMachine.OnServiceRunningStatusUpdate(_serviceRunningStatus.ServiceStatus);

            // Retrieve service handle
            WindowsServiceLifetime windowsServiceLifetime = (WindowsServiceLifetime) _hostLifetime;

            // We need to do some reflection magic, because the field is protected.
            // See issue:
            IntPtr serviceHandle = GetServiceHandle(windowsServiceLifetime) ??
                                   throw new ArgumentNullException(nameof(serviceHandle),
                                       "Cannot get Service Handle. Result is null");

            // Register lid event
            bool status = _lid.RegisterLidEventNotifications(serviceHandle, windowsServiceLifetime.ServiceName,
                _powerManagementStateMachine.OnLidChange);

            // Throw exception if register lid event failed
            if (!status)
            {
                throw new ApplicationException("Cannot register lid event notification");
            }

            // Register Network Cable event
            _networkAdapters.OnNetworkAdaptersStatusChange += _powerManagementStateMachine.OnNetworkChange;

            // Initialize network updaters with current computers network state  
            _networkAdapters.Init();

            _logger.LogInformation("Service is ready to process events");

            stoppingToken.Register(s => tcs.SetResult(true), tcs);

            return tcs.Task;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service will exit. Cleanup...");
            _lid.UnregisterLidEventNotifications();
            _networkAdapters.OnNetworkAdaptersStatusChange -= _powerManagementStateMachine.OnNetworkChange;

            return Task.CompletedTask;
        }

        private IntPtr? GetServiceHandle(WindowsServiceLifetime windowsServiceLifetime)
        {
            FieldInfo field = windowsServiceLifetime.GetType().BaseType?
                .GetField("_statusHandle", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
                return null;

            return (IntPtr?) field.GetValue(windowsServiceLifetime);
        }
    }
}