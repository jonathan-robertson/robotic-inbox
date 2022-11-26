using HarmonyLib;
using System.Reflection;

namespace RoboticInbox {
    public class ModApi : IModApi {
        private static readonly ModLog<ModApi> log = new ModLog<ModApi>();
        public void InitMod(Mod _modInstance) {
            //log.DebugMode = true;
            log.Debug("Mod Loading");
            Harmony harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModEvents.GameStartDone.RegisterHandler(StorageManager.OnGameStartDone);
            log.Debug("Mod Loaded");
        }
    }
}
