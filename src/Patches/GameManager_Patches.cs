using HarmonyLib;
using System;

namespace RoboticInbox.Patches {
    [HarmonyPatch(typeof(GameManager), "TEUnlockServer", new Type[] { typeof(int), typeof(Vector3i), typeof(int) })]
    internal class GameManager_TEUnlockServer_Patch {
        private static readonly ModLog<GameManager_TEUnlockServer_Patch> log = new ModLog<GameManager_TEUnlockServer_Patch>();

        public static bool Prefix(int _clrIdx, Vector3i _blockPos) {
            try {
                StorageManager.Distribute(_clrIdx, _blockPos);
            } catch (Exception e) {
                log.Error("Failed to handle prefix for GameManager.TEUnlockServer", e);
            }
            return true;
        }
    }
}
