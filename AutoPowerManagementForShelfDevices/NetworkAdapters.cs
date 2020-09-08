using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace AutoPowerManagementForShelfDevices
{
    public class NetworkAdapters
    {
        public delegate void NetworkAdaptersStatusChange(bool attached);

        public event NetworkAdaptersStatusChange OnNetworkAdaptersStatusChange;

        private int _connectedPorts = 0;

        private ILogger<NetworkAdapters> _logger;

        public NetworkAdapters(ILogger<NetworkAdapters> logger)
        {
            _logger = logger;

            NetworkChange.NetworkAddressChanged += (sender, args) => HandleConnectedInterfaces();
        }

        public void Init()
        {
            _logger.LogInformation("Get current network adapters status");
            HandleConnectedInterfaces();
        }

        private void HandleConnectedInterfaces()
        {
            int newConnectedPorts = 0;

            // Iterate over all interfaces and search for ethernet interfaces which are up
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        newConnectedPorts++;
                    }
                }
            }

            // If no network adapter is up, the cable is unplugged
            if (newConnectedPorts == 0 && _connectedPorts == 1)
            {
                OnNetworkAdaptersStatusChange?.Invoke(false);
                _logger.LogInformation($"Sending status update event. Attached: false");
            }

            // Check if a new cable is plugged in, but only notify on first cable
            if (newConnectedPorts == 1 && _connectedPorts == 0)
            {
                OnNetworkAdaptersStatusChange?.Invoke(true);
                _logger.LogInformation($"Sending status update event. Attached: true");
            }

            // Save new status to variable
            _connectedPorts = newConnectedPorts;
        }
    }
}