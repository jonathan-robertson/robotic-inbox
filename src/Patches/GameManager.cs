using HarmonyLib;
using RoboticInbox.Utilities;
using System;
using System.Collections.Generic;

namespace RoboticInbox.Patches
{
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnApplicationQuit))]
    internal class GameManager_OnApplicationQuit_Patches
    {
        private static readonly ModLog<GameManager_OnApplicationQuit_Patches> _log = new ModLog<GameManager_OnApplicationQuit_Patches>();

        public static bool Prefix()
        {
            try
            {
                StorageManager.OnGameManagerApplicationQuit();
                SignManager.OnGameManagerApplicationQuit();
            }
            catch (Exception e)
            {
                _log.Error("OnGameShutdown Failed", e);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TEUnlockServer))]
    internal class GameManager_TEUnlockServer_Patches
    {
        private static readonly ModLog<GameManager_TEUnlockServer_Patches> _log = new ModLog<GameManager_TEUnlockServer_Patches>();

        public static void Postfix(int _clrIdx, Vector3i _blockPos)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer)
                {
                    return;
                }
                StorageManager.Distribute(_clrIdx, _blockPos);
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }


    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TELockServer))]
    internal class GameManager_TELockServer_Patches
    {
        private static readonly ModLog<GameManager_TELockServer_Patches> _log = new ModLog<GameManager_TELockServer_Patches>();

        public static void Postfix(int _clrIdx, Vector3i _blockPos)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer)
                {
                    return;
                }
                if (StorageManager.ActiveCoroutines.TryGetValue(_blockPos, out var coroutine))
                {
                    _log.Trace($"Active coroutine detected at {_blockPos}; stopping and removing it.");
                    _ = StorageManager.ActiveCoroutines.Remove(_blockPos);
                    ThreadManager.StopCoroutine(coroutine);
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeBlocks))]
    internal class GameManager_ChangeBlocks_Patches
    {
        private static readonly ModLog<GameManager_ChangeBlocks_Patches> _log = new ModLog<GameManager_ChangeBlocks_Patches>();

        private static bool Prefix(PlatformUserIdentifierAbs persistentPlayerId, ref List<BlockChangeInfo> _blocksToChange, GameManager __instance)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer)
                {
                    return true;
                }

                for (var i = 0; i < _blocksToChange.Count; i++)
                {
                    var blockChangeInfo = _blocksToChange[i];
                    if (!blockChangeInfo.blockValue.ischild
                        && !blockChangeInfo.blockValue.isair
                        && blockChangeInfo.bChangeBlockValue
                        && StorageManager.HasRoboticInboxSecureTag(blockChangeInfo.blockValue.Block))
                    {
                        var tileEntity = __instance.m_World.GetTileEntity(blockChangeInfo.pos);
                        if (tileEntity != null
                            && tileEntity is TileEntityComposite composite
                            && StorageManager.HasRoboticInboxInsecureTag(tileEntity.blockValue.Block))
                        {
                            // We can see that the block is being upgraded from insecure -> secure
                            // i.e. our lock is being repaired
                            if (persistentPlayerId == null) // host
                            {
                                _log.Trace($"[{blockChangeInfo.pos}] {__instance.persistentLocalPlayer?.PrimaryId?.CombinedString} repaired and has taken ownership over robotic inbox");
                                composite.Owner = __instance.persistentLocalPlayer?.PrimaryId;
                            }
                            else // client/remote player
                            {
                                _log.Trace($"[{blockChangeInfo.pos}] {persistentPlayerId.CombinedString} repaired and has taken ownership over robotic inbox");
                                composite.Owner = persistentPlayerId; // one repairing now takes ownership of block
                            }
                            composite.SetModified(); // sync change to clients
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
            return true;
        }
    }
}
