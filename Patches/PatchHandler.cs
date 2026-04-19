using System;
using System.Reflection;
using AstolfoGorillaTagMenu;
using HarmonyLib;
using UnityEngine;

namespace AstolfoGorillaTagMenu.Patches
{
    public static class PatchHandler
    {
        private static Harmony? _harmony;

        public static bool IsPatched { get; private set; }
        public static int PatchErrors { get; private set; }

        public static void PatchAll()
        {
            if (IsPatched)
                return;

            try
            {
                _harmony = new Harmony(PluginInfo.GUID);
                var asm = Assembly.GetExecutingAssembly();
                PatchErrors = 0;
                _harmony.PatchAll(asm);
                IsPatched = true;
                Debug.Log($"[Astolfo] Harmony PatchAll complete ({asm.GetName().Name}).");
            }
            catch (Exception ex)
            {
                PatchErrors++;
                Debug.LogError($"[Astolfo] Harmony PatchAll failed: {ex}");
            }
        }

        public static void UnpatchAll()
        {
            if (_harmony == null || !IsPatched)
                return;
            try
            {
                _harmony.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Astolfo] Unpatch: {ex.Message}");
            }
            finally
            {
                _harmony = null;
                IsPatched = false;
            }
        }
    }
}
