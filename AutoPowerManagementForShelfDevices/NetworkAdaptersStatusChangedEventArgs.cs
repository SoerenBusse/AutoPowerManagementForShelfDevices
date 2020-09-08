using System;

namespace AutoPowerManagementForShelfDevices
{
    public class NetworkAdaptersStatusChangedEventArgs : EventArgs
    {
        public bool IsAttached { get; }

        public NetworkAdaptersStatusChangedEventArgs(bool isAttached)
        {
            IsAttached = isAttached;
        }
    }
}