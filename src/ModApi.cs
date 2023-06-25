using HarmonyLib;
using System;
using System.Reflection;

namespace RoboticInbox
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = true; // TODO: set to false before publish

        public void InitMod(Mod _modInstance)
        {
            new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
            ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
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
