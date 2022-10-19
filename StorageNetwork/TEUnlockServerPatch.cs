using HarmonyLib;
using System;

namespace StorageNetwork {
    [HarmonyPatch(typeof(GameManager), "TEUnlockServer", new Type[] { typeof(int), typeof(Vector3i), typeof(int) })]
    internal class TEUnlockServerPatch {
        private static readonly ModLog log = new ModLog(typeof(TEUnlockServerPatch));

        public static bool Prefix(int _clrIdx, Vector3i _blockPos) {
            // TODO: finalize params: GameManager __instance, int _clrIdx, Vector3i _blockPos, int _lootEntityId
            try {
                StorageManager.Distribute(_clrIdx, _blockPos);
            } catch (Exception e) {
                log.Error("failed to handle prefix for GameManager.TEUnlockServer", e);
            }
            return true;
        }
    }
}
