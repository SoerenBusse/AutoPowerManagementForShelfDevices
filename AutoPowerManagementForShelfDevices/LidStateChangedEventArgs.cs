using System;

namespace AutoPowerManagementForShelfDevices
{
    public class LidStateChangedEventArgs:EventArgs
    {
        public bool LidOpen { get; }

        public LidStateChangedEventArgs(bool lidOpen)
        {
            LidOpen = lidOpen;
        }
    }
}