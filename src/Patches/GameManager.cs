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

        public static bool Prefix(GameManager __instance, int _clrIdx, Vector3i _blockPos, int _lootEntityId, int _entityIdThatOpenedIt, string _customUi = null)
        {
            try
            {
                if (!ConnectionManager.Instance.IsServer)
                {
                    return true; // only run this check on the server (this method on client will send a NetPackage to the server)
                }

                var tileEntity = __instance.m_World.GetTileEntity(_clrIdx, _blockPos);
                if (tileEntity == null || StorageManager.IsNotActiveRoboticInbox(tileEntity))
                {
                    return true; // not what we're looking to monitor
                }

                if (__instance.lockedTileEntities.ContainsKey(tileEntity))
                {
                    // deny access
                    _log.Trace($"[{_blockPos}] robotic inbox denied access to {_entityIdThatOpenedIt} because it was actively distributing contents");
                    if ((__instance.m_World.GetEntity(_entityIdThatOpenedIt) as EntityPlayerLocal) == null)
                    {
                        SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageTELock>().Setup(NetPackageTELock.TELockType.DeniedAccess, _clrIdx, _blockPos, _lootEntityId, _entityIdThatOpenedIt, _customUi, true), false, _entityIdThatOpenedIt, -1, -1, null, 192);
                    }
                    else
                    {
                        __instance.TEDeniedAccessClient(_clrIdx, _blockPos, _lootEntityId, _entityIdThatOpenedIt);
                    }
                    return false;
                }

                // We should never be in a situation where access is granted but the inbox is currently distributing.
                // So this is now more of a sanity check.
                if (StorageManager.ActiveCoroutines.TryGetValue(_blockPos, out var coroutine))
                {
                    // TODO: downgrade back to trace?
                    _log.Warn($"active coroutine detected at {_blockPos}; stopping and removing it before player {_entityIdThatOpenedIt} accesses underlying robotic inbox.");
                    _ = StorageManager.ActiveCoroutines.Remove(_blockPos);
                    ThreadManager.StopCoroutine(coroutine);
                    return true;
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
            return true;
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
