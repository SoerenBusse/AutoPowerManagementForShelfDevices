using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class PowerControl
    {
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public static void ForceShutdownLocalMachine()
        {
            // We're just calling shutdown.exe for maximum compatibility
            Process.Start("shutdown.exe", "-s -t 0 -f");
        }

        public static void SleepLocalMachine()
        {
            SetSuspendState(false, true, false);
        }
    }
}