using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using AutoPowerManagementForShelfDevices.Interop.Native;

namespace AutoPowerManagementForShelfDevices.Interop
{
    public static class Logout
    {
        private static readonly int[] WellKnownSessionIds = {0, 65536};

        public static void LogoutAllUsers()
        {
            // Open local server
            IntPtr server = Wtsapi32.WtsOpenServer(Environment.MachineName);

            try
            {
                // Get user sessions and logout each user
                foreach (int sessionId in GetSessionIDs(server).Where(id => !WellKnownSessionIds.Contains(id)))
                {
                    // Get Username
                    string? username = GetUsername(server, sessionId);
                    if (string.IsNullOrWhiteSpace(username))
                        continue;

                    Wtsapi32.WtsLogoffSession(server, sessionId, false);
                }
            }
            finally
            {
                Wtsapi32.WtsCloseServer(server);
            }
        }

        public static bool AreAllUsersLoggedOut()
        {
            // Open local server
            IntPtr server = Wtsapi32.WtsOpenServer(Environment.MachineName);
            try
            {
                // Get user sessions and logout each user
                return GetSessionIDs(server).All(id => WellKnownSessionIds.Contains(id));
            }
            finally
            {
                Wtsapi32.WtsCloseServer(server);
            }
        }

        private static string? GetUsername(IntPtr server, int sessionId)
        {
            IntPtr buffer = IntPtr.Zero;

            try
            {
                // Check is method returns success
                if (!Wtsapi32.WtsQuerySessionInformation(server, sessionId, Wtsapi32.WtsInfoClass.WtsUserName, out buffer,
                    out uint _))
                    return null;

                return Marshal.PtrToStringAnsi(buffer)?.ToUpper().Trim();
            }
            finally
            {
                Wtsapi32.WtsFreeMemory(buffer);
            }
        }

        private static IEnumerable<int> GetSessionIDs(IntPtr server)
        {
            // Buffer which contains the SessionInfo structs
            IntPtr buffer = IntPtr.Zero;

            // Count of structs in this buffer
            int count = 0;

            // Retrieve all sessions
            try
            {
                if (!Wtsapi32.WtsEnumerateSessions(server, 0, 1, ref buffer, ref count))
                    throw new Win32Exception("Cannot enumerate wts sessions.");

                // Set start of memory location to buffer beginning
                IntPtr currentIndex = buffer;

                // Iterate over all sessions
                for (int i = 0; i < count; i++)
                {
                    var sessionInfo = (Wtsapi32.WtsSessionInfo?) Marshal.PtrToStructure(currentIndex, typeof(Wtsapi32.WtsSessionInfo));
                    if (sessionInfo == null)
                        throw new Win32Exception(
                            "Cannot not marshal session info structure.");
                    currentIndex += Marshal.SizeOf(typeof(Wtsapi32.WtsSessionInfo));
                    yield return sessionInfo.Value.SessionId;
                }
            }
            finally
            {
                // Free memory buffer
                Wtsapi32.WtsFreeMemory(buffer);
            }
        }
    }
}