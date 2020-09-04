using System;
using System.Timers;
using AutoPowerManagementForShelfDevices.Enums;
using AutoPowerManagementForShelfDevices.Interop;
using AutoPowerManagementForShelfDevices.Settings;
using Microsoft.Extensions.Logging;
using Stateless;
using Stateless.Graph;
using State = AutoPowerManagementForShelfDevices.Enums.State;

namespace AutoPowerManagementForShelfDevices
{
    public class PowerManagementStateMachine
    {
        private readonly ILogger<PowerManagementStateMachine> _logger;
        private readonly SettingsBase _settings;

        private readonly StateMachine<State, Trigger> _machine;

        public PowerManagementStateMachine(ILogger<PowerManagementStateMachine> logger, SettingsBase settings)
        {
            _logger = logger;
            _settings = settings;

            // Create timer for state machine
            var timer = new Timer
            {
                AutoReset = false,
            };

            // Create PowerManagement State machine
            _machine = new StateMachine<State, Trigger>(State.LidOpen);

            _machine.Configure(State.LidOpen)
                .Permit(Trigger.LidClose, State.LidClosed)
                .Permit(Trigger.NetworkAttach, State.LidOpenNetworkAttached)
                .OnEntry(() => LogState(State.LidOpen));

            _machine.Configure(State.LidOpenNetworkAttached)
                .Permit(Trigger.LidClose, State.LidClosedNetworkAttached)
                .Permit(Trigger.NetworkUnplug, State.LidOpen)
                .OnEntry(() => LogState(State.LidOpenNetworkAttached));

            _machine.Configure(State.LidClosed)
                .Permit(Trigger.LidOpen, State.LidOpen)
                .Permit(Trigger.NetworkAttach, State.LidClosedNetworkAttached)
                .Permit(Trigger.TimerExpired, State.LoggedOut)
                .OnEntry(() => LogState(State.LidClosed))
                .OnEntry(() => ResetAndStartTimer(timer, _settings.TimeoutLidClosed));

            _machine.Configure(State.LidClosedNetworkAttached)
                .Permit(Trigger.LidOpen, State.LidOpenNetworkAttached)
                .Permit(Trigger.NetworkUnplug, State.LidClosed)
                .Permit(Trigger.TimerExpired, State.Shutdown)
                .OnEntry(() => LogState(State.LidClosedNetworkAttached))
                .OnEntry(() => ResetAndStartTimer(timer, _settings.TimeoutLidClosedNetworkAttached));

            _machine.Configure(State.LoggedOut)
                .Permit(Trigger.LidOpen, State.LidOpen)
                .Permit(Trigger.NetworkAttach, State.LoggedOutNetworkAttached)
                .Permit(Trigger.TimerExpired, State.Sleep)
                .OnEntry(() => LogState(State.LoggedOut))
                .OnEntry(() => ResetAndStartTimer(timer, _settings.TimeoutLoggedOut))
                .OnEntry(Logout.LogoutAllUsers);

            _machine.Configure(State.LoggedOutNetworkAttached)
                .Permit(Trigger.LidOpen, State.LidOpenNetworkAttached)
                .Permit(Trigger.NetworkUnplug, State.LoggedOut)
                .Permit(Trigger.TimerExpired, State.Shutdown)
                .OnEntry(() => LogState(State.LoggedOutNetworkAttached))
                .OnEntry(() => ResetAndStartTimer(timer, _settings.TimeoutLoggedOutNetworkAttached));

            _machine.Configure(State.Shutdown)
                .OnEntry(() => LogState(State.Shutdown))
                .OnEntry(PowerControl.ForceShutdownLocalMachine);

            _machine.Configure(State.Sleep)
                .OnEntry(() => LogState(State.Sleep))
                .OnEntry(PowerControl.SleepLocalMachine);

            // Ignore invalid triggers for state
            // Our machine isn't a complete DFA
            _machine.OnUnhandledTrigger((state, trigger) => { });

            // Add Event when Timer elapsed
            // This will call the TimerExpired Trigger on the current state
            timer.Elapsed += (sender, args) => _machine.Fire(Trigger.TimerExpired);
        }

        public void OnLidChange(bool open)
        {
            _logger.LogDebug($"Received event: OnLidChange with parameters open: {open}");

            if (open)
            {
                _machine.Fire(Trigger.LidOpen);
                return;
            }

            _machine.Fire(Trigger.LidClose);
        }

        public void OnNetworkChange(bool attached)
        {
            _logger.LogDebug($"Received event: OnNetworkChange with parameters attached: {attached}");

            if (attached)
            {
                _machine.Fire(Trigger.NetworkAttach);
                return;
            }

            _machine.Fire(Trigger.NetworkUnplug);
        }

        public string GetUmlDotGraph()
        {
            return UmlDotGraph.Format(_machine.GetInfo());
        }

        private void ResetAndStartTimer(Timer timer, double interval)
        {
            timer.Stop();
            timer.Interval = interval;
            timer.Start();
        }

        private void LogState(State state)
        {
            _logger.LogDebug($"Entering {nameof(state)} state");
        }
    }
}