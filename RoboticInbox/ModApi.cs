using HarmonyLib;
using System.Reflection;

namespace RoboticInbox {
    public class ModApi : IModApi {
        public void InitMod(Mod _modInstance) {
            Harmony harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModEvents.GameStartDone.RegisterHandler(StorageManager.OnGameStartDone);
        }
    }
}
