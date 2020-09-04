using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class PowerControl
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InitiateSystemShutdown(string machineName, string message, uint timeout,
            bool forceAppsClosed, bool rebootAfterShutdown);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public static void ForceShutdownLocalMachine()
        {
            InitiateSystemShutdown("", "Shutdown", 0, true, false);
        }

        public static void SleepLocalMachine()
        {
            SetSuspendState(false, true, false);
        }
    }
}