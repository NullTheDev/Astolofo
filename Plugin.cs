using System;
using AstolfoGorillaTagMenu.Core;
using AstolfoGorillaTagMenu.Patches;
using AstolfoGorillaTagMenu.UI;
using BepInEx;
using UnityEngine;

namespace AstolfoGorillaTagMenu
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private UI.AstolfoMenuBehaviour? _menu;

        private void Awake()
        {
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loading.");
            GorillaGameManager.OnInstanceReady(OnGameReady);
        }

        private void OnGameReady()
        {
            try
            {
                if (_menu != null)
                    return;

                NotificationManager.EnsureCreated();
                PatchHandler.PatchAll();
                AstolfoCore.Initialize(Logger);
                gameObject.AddComponent<CoreTickBehaviour>();

                _menu = gameObject.AddComponent<UI.AstolfoMenuBehaviour>();
                _menu.Initialize(Logger);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Astolfo] Failed to start: {ex}");
            }
        }

        private void OnDestroy()
        {
            AstolfoCore.Shutdown();
            PatchHandler.UnpatchAll();
        }
    }
}
