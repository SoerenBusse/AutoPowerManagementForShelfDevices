using System;
using System.Timers;

namespace AutoPowerManagementForShelfDevices.Settings
{
    public abstract class SettingsBase
    {
        protected const double DefaultTimeoutLidClosed = 5 * 60 * 1000;
        protected const double DefaultTimeoutLidClosedNetworkAttached = 5 * 60 * 1000;
        protected const double DefaultTimeoutLoggedOut = 120 * 60 * 1000;
        protected const double DefaultTimeoutLoggedOutNetworkAttached = 5 * 60 * 1000;
     
        private const double RefreshSettingsInterval = 15 * 60 * 1000;
        
        public abstract double TimeoutLidClosed { get; protected set; }
        public abstract double TimeoutLidClosedNetworkAttached { get; protected set; }
        public abstract double TimeoutLoggedOut { get; protected set; }
        public abstract double TimeoutLoggedOutNetworkAttached { get; protected set; }

        private Timer _timer;

        protected SettingsBase()
        {
            _timer = new Timer(RefreshSettingsInterval);
            _timer.Elapsed += (o, args) => { Refresh(); };
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public abstract void Load();

        protected abstract void Refresh();
    }
}