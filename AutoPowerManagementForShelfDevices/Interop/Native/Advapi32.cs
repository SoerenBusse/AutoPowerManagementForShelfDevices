using System;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop.Native
{
    public static class Advapi32
    {
        private const string DllName = "advapi32.dll";
        
        public delegate int ServiceControlHandlerEx(int dwControl, int dwEventType, IntPtr lpEventData,
            IntPtr lpContext);
        
        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int RegisterServiceCtrlHandlerEx(string lpServiceName,
            ServiceControlHandlerEx serviceControlHandlerEx,
            IntPtr context);
    }
}