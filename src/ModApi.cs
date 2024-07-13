using HarmonyLib;
using RoboticInbox.Utilities;
using System;
using System.Reflection;

namespace RoboticInbox
{
    public class ModApi : IModApi
    {
        private const string ModMaintainer = "kanaverum";
        private const string SupportLink = "https://discord.gg/hYa2sNHXya";

        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            try
            {
                new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
                SettingsManager.Load();
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
            }
            catch (Exception e)
            {
                _log.Error($"Failed to start up Robotic Inbox mod; take a look at logs for guidance but feel free to also reach out to the mod maintainer {ModMaintainer} via {SupportLink}", e);
            }
        }

        private void OnGameStartDone()
        {
            try
            {
                StorageManager.OnGameStartDone();
            }
            catch (Exception e)
            {
                _log.Error("OnGameStartDone Failed", e);
            }
        }

        private void OnGameShutdown()
        {
            try
            {
                if (StorageManager.ActiveCoroutines.Count == 0)
                {
                    _log.Info("No coroutines needed to be stopped for shutdown.");
                    return;
                }
                _log.Info($"Stopping {StorageManager.ActiveCoroutines.Count} live coroutines for shutdown.");
                foreach (var kvp in StorageManager.ActiveCoroutines)
                {
                    ThreadManager.StopCoroutine(kvp.Value);
                }
                _log.Info($"All coroutines stopped for shutdown.");
            }
            catch (Exception e)
            {
                _log.Error("OnGameShutdown Failed", e);
            }
        }
    }
}
