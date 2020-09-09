namespace AutoPowerManagementForShelfDevices.Enums
{
    public enum State
    {
        // States for checking if the system is booted with lid close
        // Prevents automatic shutdown, if the system is booted using WoL for remote access and remote management 
        Start,
        StartNetworkAttached,
        Boot,
        BootNetworkAttached,
        WoLWait,
        WoLWaitNetworkAttached,
        
        // Main states for handling the power management
        LidOpen,
        LidOpenNetworkAttached,
        LidClosed,
        LidClosedNetworkAttached,
        LoggedOut,
        LoggedOutNetworkAttached,
        Shutdown,
        Sleep,
        SleepNetworkAttached
    }
}