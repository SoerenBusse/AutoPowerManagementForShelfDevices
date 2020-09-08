using System;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop.Native
{
    public static class User32
    {
        private const string DllName = "user32.dll";

        public const int DeviceNotifyServiceHandle = 0x00000001;
        public const int ServiceControlPowerEvent = 0x0000000D;
        public const int PowerButtonPowerSettingChange = 0x8013;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct PowerBroadcastSetting
        {
            public readonly Guid PowerSetting;
            public readonly uint DataLength;
            public readonly byte Data;
        }

        [DllImport(DllName, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid,
            int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnregisterPowerSettingNotification(IntPtr handle);
    }
}