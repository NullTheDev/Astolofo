using System;
using AstolfoGorillaTagMenu.UI;
using BepInEx.Logging;

namespace AstolfoGorillaTagMenu.Core
{
    public static class AstolfoCore
    {
        private static bool _hooked;

        private static Action? _onJoinedRoom;
        private static Action? _onReturnedSingle;
        private static Action<NetPlayer>? _onPlayerJoin;
        private static Action<NetPlayer>? _onPlayerLeave;

        public static event Action? OnJoinedRoom;
        public static event Action? OnReturnedToSinglePlayer;
        public static event Action<NetPlayer?>? OnMasterClientSwitched;
        public static event Action<NetPlayer?>? OnPlayerJoined;
        public static event Action<NetPlayer?>? OnPlayerLeft;
        public static event Action? OnNetworkFrame;

        internal static void RaiseNetworkFrame() => OnNetworkFrame?.Invoke();

        internal static void RaiseMasterClientSwitched(NetPlayer player)
        {
            OnMasterClientSwitched?.Invoke(player);
        }

        public static void Initialize(ManualLogSource log)
        {
            LimitMonitor.Initialize(log);

            if (_hooked)
                return;

            var ns = NetworkSystem.Instance;
            if (ns == null)
            {
                log.LogWarning("[Astolfo] NetworkSystem.Instance was null; core hooks not attached.");
                return;
            }

            _onJoinedRoom = HandleJoinedRoom;
            _onReturnedSingle = HandleReturnedSingle;
            _onPlayerJoin = HandlePlayerJoin;
            _onPlayerLeave = HandlePlayerLeave;

            ns.OnJoinedRoomEvent += _onJoinedRoom;
            ns.OnReturnedToSinglePlayer += _onReturnedSingle;
            ns.OnPlayerJoined += _onPlayerJoin;
            ns.OnPlayerLeft += _onPlayerLeave;

            _hooked = true;
            log.LogInfo("[Astolfo] Core: subscribed to NetworkSystem room/player events.");
        }

        public static void Shutdown()
        {
            if (!_hooked)
                return;

            var ns = NetworkSystem.Instance;

            if (ns != null)
            {
                if (_onJoinedRoom != null)
                    ns.OnJoinedRoomEvent -= _onJoinedRoom;

                if (_onReturnedSingle != null)
                    ns.OnReturnedToSinglePlayer -= _onReturnedSingle;

                if (_onPlayerJoin != null)
                    ns.OnPlayerJoined -= _onPlayerJoin;

                if (_onPlayerLeave != null)
                    ns.OnPlayerLeft -= _onPlayerLeave;
            }

            _onJoinedRoom = null;
            _onReturnedSingle = null;
            _onPlayerJoin = null;
            _onPlayerLeave = null;

            _hooked = false;
        }

        private static void HandleJoinedRoom()
        {
            OnJoinedRoom?.Invoke();
            NotificationManager.Push("Room joined", 2.8f, NotificationKind.Success);
        }

        private static void HandleReturnedSingle()
        {
            OnReturnedToSinglePlayer?.Invoke();
            NotificationManager.Push("Returned to single player", 2.8f, NotificationKind.Info);
        }

        private static void HandlePlayerJoin(NetPlayer player)
        {
            OnPlayerJoined?.Invoke(player);

            if (player == null)
                return;

            var name = !string.IsNullOrEmpty(player.SanitizedNickName)
                ? player.SanitizedNickName
                : player.NickName;

            if (string.IsNullOrEmpty(name))
                name = player.DefaultName;

            NotificationManager.Push($"{name} joined", 2.6f, NotificationKind.Info);
        }

        private static void HandlePlayerLeave(NetPlayer player)
        {
            OnPlayerLeft?.Invoke(player);

            if (player == null)
                return;

            var name = !string.IsNullOrEmpty(player.SanitizedNickName)
                ? player.SanitizedNickName
                : player.NickName;

            if (string.IsNullOrEmpty(name))
                name = player.DefaultName;

            NotificationManager.Push($"{name} left", 2.6f, NotificationKind.Muted);
        }
    }
}