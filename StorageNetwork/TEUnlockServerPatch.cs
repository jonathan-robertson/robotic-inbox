using HarmonyLib;
using System;

namespace StorageNetwork {
    [HarmonyPatch(typeof(GameManager), "TEUnlockServer", new Type[] { typeof(int), typeof(Vector3i), typeof(int) })]
    internal class TEUnlockServerPatch {
        private static readonly ModLog log = new ModLog(typeof(TEUnlockServerPatch));

        public static bool Prefix(GameManager __instance, int _clrIdx, Vector3i _blockPos, int _lootEntityId) {
            try {
                log.Info($@"TEUnlockServer called for GameManager:
clrIdx: {_clrIdx}
blockPos: {_blockPos}
lootEntityId: {_lootEntityId}");

                var otherPos = _blockPos + Vector3i.up;
                var target = __instance.World.GetTileEntity(_clrIdx, otherPos);
                StorageManager.Distribute(__instance.World.GetTileEntity(_clrIdx, _blockPos), __instance.World.GetTileEntity(_clrIdx, otherPos), _blockPos);

                /*
                if (tileEntityLootContainer != null) {
                    _player.AimingGun = false;
                    Vector3i blockPos = tileEntityLootContainer.ToWorldPos();
                    tileEntityLootContainer.bWasTouched = tileEntityLootContainer.bTouched;
                    _world.GetGameManager().TELockServer(_clrIdx, blockPos, tileEntityLootContainer.entityId, _player.entityId, null);
                    return true;
                }
                */
                //__instance.World.GetTileEntity(_clrIdx, _blockPos + Vector3i.up);
            } catch (Exception e) {
                log.Error("failed to handle prefix for GameManager.TEUnlockServer", e);
            }
            return true;
        }
    }
}
