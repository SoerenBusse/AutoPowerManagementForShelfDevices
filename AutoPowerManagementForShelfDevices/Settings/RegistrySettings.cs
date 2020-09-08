using System;
using AutoPowerManagementForShelfDevices.Utils;
using Microsoft.Win32;

namespace AutoPowerManagementForShelfDevices.Settings
{
    public class RegistrySettings : SettingsBase
    {
        private const string RegistryBaseLocation = @"SOFTWARE\APMFSD\";
        private const string RegistryTimeoutLidClosedKey = "TimeoutLidClosed";
        private const string RegistryTimeoutLidClosedNetworkAttachedKey = "TimeoutLidClosedNetworkAttached";
        private const string RegistryTimeoutLoggedOutKey = "TimeoutLoggedOut";
        private const string RegistryTimeoutLoggedOutNetworkAttachedKey = "TimeoutLoggedOutNetworkAttached";

        public override double TimeoutLidClosed { get; protected set; } = DefaultTimeoutLidClosed;

        public override double TimeoutLidClosedNetworkAttached { get; protected set; } =
            DefaultTimeoutLidClosedNetworkAttached;

        public override double TimeoutLoggedOut { get; protected set; } = DefaultTimeoutLoggedOut;

        public override double TimeoutLoggedOutNetworkAttached { get; protected set; } =
            DefaultTimeoutLoggedOutNetworkAttached;

        public override void Load()
        {
            // Load settings from registry
            Refresh();
        }

        protected override void Refresh()
        {
            // Try to open base registry key
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            baseKey = baseKey.OpenSubKey(RegistryBaseLocation);

            // We cannot find the key in 64bit view
            if (baseKey == null)
            {
                // Try to search in 32bit view
                baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                baseKey = baseKey.OpenSubKey(RegistryBaseLocation);
            }

            // Key weren't found. Give up
            if (baseKey == null)
                return;

            TimeoutLidClosed = RegistryUtils.ParseRegistryKey(baseKey, RegistryTimeoutLidClosedKey,
                DefaultTimeoutLidClosed);


            TimeoutLidClosedNetworkAttached = RegistryUtils.ParseRegistryKey(baseKey,
                RegistryTimeoutLidClosedNetworkAttachedKey,
                DefaultTimeoutLidClosedNetworkAttached);

            TimeoutLoggedOut =
                RegistryUtils.ParseRegistryKey(baseKey, RegistryTimeoutLoggedOutKey, DefaultTimeoutLoggedOut);

            TimeoutLoggedOutNetworkAttached = RegistryUtils.ParseRegistryKey(baseKey,
                RegistryTimeoutLoggedOutNetworkAttachedKey,
                DefaultTimeoutLoggedOutNetworkAttached);
        }
    }
}