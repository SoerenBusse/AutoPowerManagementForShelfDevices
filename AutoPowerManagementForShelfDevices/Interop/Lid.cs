using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using AutoPowerManagementForShelfDevices.Interop.Native;

namespace AutoPowerManagementForShelfDevices.Interop
{
    // Inspired https://github.com/rowandh/lidstatusservice/blob/master/LidStatusService/Lid.cs by "rowandh":
    public class Lid
    {
        private static Guid _lidSwitchStateChangeGuid = new Guid("ba3e0f4d-b817-4094-a2d1-d56379e6a0f3");

        private IntPtr _powerSettingsNotificationHandle;

        public event EventHandler<LidStateChangedEventArgs>? LidStateChanged;

        public void RegisterLidEventNotifications(IntPtr serviceHandle, string serviceName)
        {
            _powerSettingsNotificationHandle = User32.RegisterPowerSettingNotification(serviceHandle,
                ref _lidSwitchStateChangeGuid, User32.DeviceNotifyServiceHandle);
            if (_powerSettingsNotificationHandle == IntPtr.Zero)
                throw new Win32Exception("Registering power settings notification handle failed.");
        }

        public void UnregisterLidEventNotifications()
        {
            if (!User32.UnregisterPowerSettingNotification(_powerSettingsNotificationHandle))
                throw new Win32Exception("Unregistering power settings notification handle failed.");
        }

        public int HandleLidEvents(int dwControl, int dwEventType, IntPtr lpEventData, IntPtr lpContext)
        {
            // If dwControl is SERVICE_CONTROL_POWEREVENT
            // and dwEventType is PBT_POWERSETTINGCHANGE
            // then lpEventData is a pointer to a POWERBROADCAST_SETTING struct
            // Ref. https://msdn.microsoft.com/en-us/library/ms683241(v=vs.85).aspx
            if (dwControl == User32.ServiceControlPowerEvent && dwEventType == User32.PowerButtonPowerSettingChange)
            {
                var powerBroadcastSetting = (User32.PowerBroadcastSetting?) Marshal.PtrToStructure(lpEventData,
                    typeof(User32.PowerBroadcastSetting));
                if (powerBroadcastSetting == null)
                    throw new Win32Exception(
                        "Cannot not marshal power broadcast setting structure.");

                if (powerBroadcastSetting.Value.PowerSetting == _lidSwitchStateChangeGuid)
                {
                    bool lidOpen = powerBroadcastSetting.Value.Data != 0;
                    LidStateChanged?.Invoke(this, new LidStateChangedEventArgs(lidOpen));

                    return WindowsErrorCodes.Success;
                }
            }

            return WindowsErrorCodes.CallNotImplemented;
        }
    }
}