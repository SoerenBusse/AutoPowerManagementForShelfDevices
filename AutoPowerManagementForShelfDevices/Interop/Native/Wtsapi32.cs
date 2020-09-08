using System;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop.Native
{
    public static class Wtsapi32
    {
        private const string DllName = "wtsapi32.dll";

        [StructLayout(LayoutKind.Sequential)]
        public struct WtsSessionInfo
        {
            public int SessionId;
            public string WinStationName;
            public WtsConnectStateClass State;
        }

        public enum WtsConnectStateClass
        {
            WtsActive,
            WtsConnected,
            WtsConnectQuery,
            WtsShadow,
            WtsDisconnected,
            WtsIdle,
            WtsListen,
            WtsReset,
            WtsDown,
            WtsInit
        }

        public enum WtsInfoClass
        {
            WtsInitialProgram,
            WtsApplicationName,
            WtsWorkingDirectory,
            WtsOemId,
            WtsSessionId,
            WtsUserName,
            WtsWinStationName,
            WtsDomainName,
            WtsConnectState,
            WtsClientBuildNumber,
            WtsClientName,
            WtsClientDirectory,
            WtsClientProductId,
            WtsClientHardwareId,
            WtsClientAddress,
            WtsClientDisplay,
            WtsClientProtocolType,
            WtsIdleTime,
            WtsLogonTime,
            WtsIncomingBytes,
            WtsOutgoingBytes,
            WtsIncomingFrames,
            WtsOutgoingFrames,
            WtsClientInfo,
            WtsSessionInfo
        }

        [DllImport(DllName, SetLastError = true, EntryPoint = "WTSLogoffSession")]
        public static extern bool WtsLogoffSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport(DllName, EntryPoint ="WTSQuerySessionInformation")]
        public static extern bool WtsQuerySessionInformation(IntPtr hServer, int sessionId,
            WtsInfoClass wtsInfoClass,
            out IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport(DllName, SetLastError = true, EntryPoint = "WTSOpenServer")]
        public static extern IntPtr WtsOpenServer(string pServerName);

        [DllImport(DllName, EntryPoint = "WTSCloseServer")]
        public static extern void WtsCloseServer(IntPtr hServer);

        [DllImport(DllName, SetLastError = true, EntryPoint = "WTSEnumerateSessions")]
        public static extern bool WtsEnumerateSessions(
            IntPtr hServer,
            int reserved,
            int version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        [DllImport(DllName, EntryPoint = "WTSFreeMemory")]
        public static extern void WtsFreeMemory(IntPtr pMemory);
    }
}