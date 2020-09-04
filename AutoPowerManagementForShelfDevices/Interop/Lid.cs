using System;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop
{
    // Thanks to "rowandh":
    // https://github.com/rowandh/lidstatusservice/blob/master/LidStatusService/Lid.cs
    public class Lid
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private readonly struct PowerBroadcastSetting
        {
            public readonly Guid PowerSetting;
            public readonly uint DataLength;
            public readonly byte Data;
        }

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid,
            Int32 flags);

        [DllImport("User32", EntryPoint = "UnregisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr RegisterServiceCtrlHandlerEx(string lpServiceName, ServiceControlHandlerEx cbex,
            IntPtr context);

        private const int DeviceNotifyServiceHandle = 0x00000001;
        private const int ServiceControlPowerEvent = 0x0000000D;
        private const int PowerButtonPowerSettingChange = 0x8013;

        private static Guid _guidLidSwitchStateChange =
            new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0, 0xF3);

        private delegate IntPtr ServiceControlHandlerEx(int control, int eventType, IntPtr eventData, IntPtr context);

        private event Action<bool> LidEventHandler;

        private IntPtr _powerSettingsNotificationHandle;
        private readonly ServiceControlHandlerEx _serviceControlHandler;

        public Lid()
        {
            // Assign callback delegate as member variable to prevent error with garbage collection
            _serviceControlHandler = MessageHandler;
        }

        public bool RegisterLidEventNotifications(IntPtr serviceHandle, string serviceName,
            Action<bool> lidEventHandler)
        {
            LidEventHandler = lidEventHandler;

            _powerSettingsNotificationHandle = RegisterPowerSettingNotification(serviceHandle,
                ref _guidLidSwitchStateChange,
                DeviceNotifyServiceHandle);

            var serviceCtrlHandler = RegisterServiceCtrlHandlerEx(serviceName, _serviceControlHandler, IntPtr.Zero);

            if (serviceCtrlHandler == IntPtr.Zero)
                return false;

            return _powerSettingsNotificationHandle != IntPtr.Zero;
        }

        public bool UnregisterLidEventNotifications()
        {
            return _powerSettingsNotificationHandle != IntPtr.Zero &&
                   UnregisterPowerSettingNotification(_powerSettingsNotificationHandle);
        }

        private IntPtr MessageHandler(int dwControl, int dwEventType, IntPtr lpEventData, IntPtr lpContext)
        {
            // If dwControl is SERVICE_CONTROL_POWEREVENT
            // and dwEventType is PBT_POWERSETTINGCHANGE
            // then lpEventData is a pointer to a POWERBROADCAST_SETTING struct
            // Ref. https://msdn.microsoft.com/en-us/library/ms683241(v=vs.85).aspx
            if (dwControl == ServiceControlPowerEvent && dwEventType == PowerButtonPowerSettingChange)
            {
                var ps = (PowerBroadcastSetting) Marshal.PtrToStructure(lpEventData, typeof(PowerBroadcastSetting));

                if (ps.PowerSetting == _guidLidSwitchStateChange)
                {
                    var isLidOpen = ps.Data != 0;
                    LidEventHandler?.Invoke(isLidOpen);
                }
            }

            return IntPtr.Zero;
        }
    }
}