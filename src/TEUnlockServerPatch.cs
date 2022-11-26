using HarmonyLib;
using System;

namespace RoboticInbox {
    [HarmonyPatch(typeof(GameManager), "TEUnlockServer", new Type[] { typeof(int), typeof(Vector3i), typeof(int) })]
    internal class TEUnlockServerPatch {
        private static readonly ModLog<TEUnlockServerPatch> log = new ModLog<TEUnlockServerPatch>();

        public static bool Prefix(int _clrIdx, Vector3i _blockPos) {
            try {
                StorageManager.Distribute(_clrIdx, _blockPos);
            } catch (Exception e) {
                log.Error("failed to handle prefix for GameManager.TEUnlockServer", e);
            }
            return true;
        }
    }
}
