using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class Logout
    {
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("Wtsapi32.dll")]
        static extern bool WTSQuerySessionInformation(System.IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass,
            out System.IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern Int32 WTSEnumerateSessions(IntPtr hServer, [MarshalAs(UnmanagedType.U4)] Int32 reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 version, ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        private struct WtsSessionInfo
        {
            public Int32 SessionId;
            [MarshalAs(UnmanagedType.LPStr)] public String WinStationName;
            public WtsConnectStateClass State;
        }

        enum WtsConnectStateClass
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

        enum WtsInfoClass
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

        public static void LogoutAllUsers()
        {
            // Open local server
            IntPtr server = WTSOpenServer(Environment.MachineName);

            // Get user sessions and logout each user
            foreach (int sessionId in GetSessionIDs(server))
            {
                // Ignore well-known sessions
                if(sessionId == 0 || sessionId == 65536)
                    continue;

                // Get Username
                string username = GetUsername(server, sessionId);
                if (string.IsNullOrWhiteSpace(username))
                    continue;

                WTSLogoffSession(server, sessionId, false);
            }
        }

        public static bool AreAllUsersLoggedOut()
        {
            // Open local server
            IntPtr server = WTSOpenServer(Environment.MachineName);

            // Get user sessions and logout each user
            return GetSessionIDs(server).Count == 0;
        }

        private static string GetUsername(IntPtr server, int sessionId)
        {
            IntPtr buffer = IntPtr.Zero;
            uint bufferSize = 0;

            string username;

            try
            {
                bool status = WTSQuerySessionInformation(server, sessionId, WtsInfoClass.WtsUserName, out buffer,
                    out bufferSize);

                // Check is method returns success
                if (!status)
                {
                    return null;
                }

                username = Marshal.PtrToStringAnsi(buffer)?.ToUpper().Trim();
            }
            finally
            {
                WTSFreeMemory(buffer);
            }

            return username;
        }

        private static List<int> GetSessionIDs(IntPtr server)
        {
            List<int> sessionIds = new List<int>();

            // Buffer which contains the SessionInfo structs
            IntPtr buffer = IntPtr.Zero;

            // Count of structs in this buffer
            int count = 0;

            // Retrieve all sessions
            try
            {
                int status = WTSEnumerateSessions(server, 0, 1, ref buffer, ref count);

                // Set start of memory location to buffer beginning
                IntPtr currentIndex = buffer;

                if (status != 0)
                {
                    // Iterate over all sessions

                    for (int i = 0; i < count; i++)
                    {
                        WtsSessionInfo sessionInfo =
                            (WtsSessionInfo) Marshal.PtrToStructure(currentIndex, typeof(WtsSessionInfo));
                        currentIndex += Marshal.SizeOf(typeof(WtsSessionInfo));
                        sessionIds.Add(sessionInfo.SessionId);
                    }
                }
            }
            finally
            {
                // Free memory buffer
                WTSFreeMemory(buffer);
            }

            return sessionIds;
        }
    }
}