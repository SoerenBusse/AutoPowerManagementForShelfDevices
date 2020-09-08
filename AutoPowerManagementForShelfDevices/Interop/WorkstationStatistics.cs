using System;
using System.Management;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class WorkstationStatistics
    {
        public static DateTime GetLastBootTime()
        {
            var query = new SelectQuery(@"SELECT LastBootUpTime FROM Win32_OperatingSystem WHERE Primary='true'");
            var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementBaseObject managementBaseObject in searcher.Get())
            {
                if (managementBaseObject is not ManagementObject)
                    continue;

                string time = managementBaseObject.Properties["LastBootUpTime"].Value.ToString() ??
                              throw new ApplicationException("Cannot get last boot time");

                return ManagementDateTimeConverter.ToDateTime(time).ToUniversalTime();
            }

            throw new ApplicationException("No last boot time entry found in WMI.");
        }
    }
}