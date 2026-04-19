using AstolfoGorillaTagMenu.UI;
using BepInEx.Logging;
using GorillaLocomotion;
using UnityEngine;

namespace AstolfoGorillaTagMenu.Features
{
    internal static class PlayerActions
    {
        internal static ManualLogSource? Log;

        internal static bool TryTeleportTo(NetPlayer target)
        {
            if (target == null || target.IsLocal)
                return false;
            if (GTPlayer.Instance == null)
                return false;

            var rig = GorillaGameManager.StaticFindRigForPlayer(target);
            if (rig == null)
                return false;

            var pos = rig.transform.position;
            var rot = rig.transform.rotation;
            GTPlayer.Instance.TeleportTo(pos, rot, true);
            NotificationManager.Push("Teleported", 2f, NotificationKind.Success);
            return true;
        }

        internal static void CopyNickname(NetPlayer target)
        {
            if (target == null)
                return;
            var name = string.IsNullOrEmpty(target.SanitizedNickName) ? target.NickName : target.SanitizedNickName;
            if (string.IsNullOrEmpty(name))
                name = target.DefaultName;
            GUIUtility.systemCopyBuffer = name;
        }

        internal static void LogPlayerInfo(NetPlayer target)
        {
            if (target == null || Log == null)
                return;
            var rig = GorillaGameManager.StaticFindRigForPlayer(target);
            var dist = rig != null && GTPlayer.Instance != null
                ? Vector3.Distance(GTPlayer.Instance.transform.position, rig.transform.position)
                : -1f;
            Log.LogInfo(
                $"[Astolfo] {target.NickName} | actor={target.ActorNumber} | local={target.IsLocal} | master={target.IsMasterClient} | dist={dist:0.0}m");
        }

        internal static float? TryGetDistanceMeters(NetPlayer target)
        {
            if (target == null || GTPlayer.Instance == null)
                return null;
            var rig = GorillaGameManager.StaticFindRigForPlayer(target);
            if (rig == null)
                return null;
            return Vector3.Distance(GTPlayer.Instance.transform.position, rig.transform.position);
        }
    }
}
