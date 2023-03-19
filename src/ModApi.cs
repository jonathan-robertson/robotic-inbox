using HarmonyLib;
using System;
using System.Reflection;

namespace RoboticInbox
{
    public class ModApi : IModApi
    {
        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
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
    }
}
