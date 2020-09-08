using System;
using System.Management;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class WorkstationStatistics
    {
        public static DateTime? GetLastBootTime()
        {
            var query = new SelectQuery(@"SELECT LastBootUpTime FROM Win32_OperatingSystem WHERE Primary='true'");
            var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementBaseObject managementBaseObject in searcher.Get())
            {
                if (managementBaseObject is not ManagementObject)
                    continue;

                var time = managementBaseObject.Properties["LastBootUpTime"].Value.ToString();

                if (time == null)
                    return null;

                return ManagementDateTimeConverter.ToDateTime(time).ToUniversalTime();
            }

            return null;
        }
    }
}