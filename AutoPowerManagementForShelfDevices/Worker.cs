using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoPowerManagementForShelfDevices.Interop;
using AutoPowerManagementForShelfDevices.Interop.Native;
using AutoPowerManagementForShelfDevices.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

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
        
        private readonly WindowsServiceLifetime _windowsServiceLifetime;
        private readonly IntPtr _serviceHandle;
        
        private readonly Advapi32.ServiceControlHandlerEx _serviceControlHandler;
        private readonly Advapi32.ServiceControlHandlerEx _baseServiceControlHandler;

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
            _serviceControlHandler = HandleServiceControlEvents;

            // Check if lifetime is a windows service
            if (!(hostLifetime is WindowsServiceLifetime windowsServiceLifetime))
                throw new NotSupportedException(
                    $"Current Lifetime isn't supported: {_windowsServiceLifetime}. Require WindowsServiceLifetime");
            _windowsServiceLifetime = windowsServiceLifetime;

            // Retrieve service handle
            _serviceHandle = GetServiceHandle();
            
            // Get access to service control handler of the ServiceBase class
            MethodInfo? baseServiceControlHandlerMethod = windowsServiceLifetime.GetType().BaseType?.GetMethod(
                "ServiceCommandCallbackEx",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (baseServiceControlHandlerMethod == null)
                throw new ApplicationException("Could not find ServiceCommandCallbackEx method in ServiceBase class.");

            _baseServiceControlHandler = (control, type, data, context) =>
            {
                return (int) (baseServiceControlHandlerMethod.Invoke(windowsServiceLifetime,
                                  new object[] {control, type, data, context}) ??
                              throw new ApplicationException("Unexpected return value from ServiceCommandCallbackEx."));
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Load Registry Keys
            _settings.Load();

            // Check service running status and apply current status to state machine
            _serviceRunningStatus.InitStatus();
            _powerManagementStateMachine.OnServiceRunningStatusUpdate(_serviceRunningStatus.ServiceStatus);
            
            // Register our own service control handler
            if (Advapi32.RegisterServiceCtrlHandlerEx(_windowsServiceLifetime.ServiceName, _serviceControlHandler, IntPtr.Zero) ==
                0)
                throw new Win32Exception("Registering service control handler failed.");

            // Register lid event
            _lid.LidStateChanged += (sender, args) => _powerManagementStateMachine.OnLidChange(args.LidOpen);
            _lid.RegisterLidEventNotifications(_serviceHandle, _windowsServiceLifetime.ServiceName);

            // Register Network Cable event
            _networkAdapters.OnNetworkAdaptersStatusChange += _powerManagementStateMachine.OnNetworkChange;

            // Initialize network updaters with current computers network state  
            _networkAdapters.Init();

            _logger.LogInformation("Service is ready to process events");

            await using CancellationTokenRegistration stoppingRegistration = stoppingToken.Register(s => tcs.SetResult(true), tcs);
            await tcs.Task;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service will exit. Cleanup...");
            _lid.UnregisterLidEventNotifications();
            _networkAdapters.OnNetworkAdaptersStatusChange -= _powerManagementStateMachine.OnNetworkChange;

            return Task.CompletedTask;
        }

        private IntPtr GetServiceHandle()
        {
            FieldInfo? field = _windowsServiceLifetime.GetType().BaseType?
                .GetField("_statusHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new ApplicationException("Cannot retrieve status handle from windows service lifetime.");

            return (IntPtr) (field.GetValue(_windowsServiceLifetime) ?? throw new ApplicationException("Service handle must not be null."));
        }

        private int HandleServiceControlEvents(int dwControl, int dwEventType, IntPtr lpEventData, IntPtr lpContext)
        {
            int status = _lid.HandleLidEvents(dwControl, dwEventType, lpEventData, lpContext);

            // Forward to base handler
            if (status == WindowsErrorCodes.CallNotImplemented)
                status = _baseServiceControlHandler.Invoke(dwControl, dwEventType, lpEventData, lpContext);

            return status;
        }
    }
}