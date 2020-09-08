using System;
using AutoPowerManagementForShelfDevices.Enums;
using AutoPowerManagementForShelfDevices.Interop;
using AutoPowerManagementForShelfDevices.Utils;
using Microsoft.Win32;

namespace AutoPowerManagementForShelfDevices
{
    public class ServiceRunningStatus
    {
        private const string RegistryBaseLocation = @"SOFTWARE\APMFSD\";
        private const string LastBootTime = "LastBootTime";

        public ServiceStatus ServiceStatus { get; private set; }

        public void InitStatus()
        {
            // Check last boot time
            RegistryKey baseKey = OpenRegistryBaseKey();
            var lastUnixBootTimeInRegistry = RegistryUtils.ParseRegistryKey<long>(baseKey, LastBootTime, 0);

            // Set new last boot time
            DateTime currentLastBootTime = WorkstationStatistics.GetLastBootTime();
            long currentLastUnixBootTime = ((DateTimeOffset) currentLastBootTime).ToUnixTimeSeconds();

            baseKey.SetValue(LastBootTime, currentLastUnixBootTime);

            // If lastBootTime is empty it's the first start of this service
            if (lastUnixBootTimeInRegistry == 0)
                ServiceStatus = ServiceStatus.FirstStart;

            if (currentLastUnixBootTime == lastUnixBootTimeInRegistry)
                ServiceStatus = ServiceStatus.Restarted;

            ServiceStatus = ServiceStatus.StartedOnBoot;
        }

        private static RegistryKey OpenRegistryBaseKey()
        {
            RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey? baseKey = localMachine.OpenSubKey(RegistryBaseLocation, true);

            // We cannot find the key in 64bit view
            if (baseKey == null)
            {
                // Try to search in 32bit view
                baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                baseKey = baseKey.OpenSubKey(RegistryBaseLocation, true);
            }

            // Key weren't found. We need to create it
            if (baseKey == null)
                baseKey = localMachine.CreateSubKey(RegistryBaseLocation) ??
                          throw new ApplicationException($"Cannot create registry key {RegistryBaseLocation}.");

            return baseKey;
        }
    }
}