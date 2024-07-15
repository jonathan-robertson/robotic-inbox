using HarmonyLib;
using RoboticInbox.Utilities;
using System;

namespace RoboticInbox.Patches
{
    [HarmonyPatch(typeof(NetPackageTELock), "ProcessPackage")]
    internal class NetPackageTELock_ProcessPackage_Patch
    {
        private static readonly ModLog<NetPackageTELock_ProcessPackage_Patch> _log = new ModLog<NetPackageTELock_ProcessPackage_Patch>();

        public static void Postfix(NetPackageTELock.TELockType ___type, int ___clrIdx, int ___posX, int ___posY, int ___posZ, int ___lootEntityId, int ___entityIdThatOpenedIt)
        {
            try
            {
                var _blockPos = new Vector3i(___posX, ___posY, ___posZ);
                _log.Trace($"Postfix: ({___clrIdx}) {___lootEntityId} [{___type}] {_blockPos} {___entityIdThatOpenedIt}");
                switch (___type)
                {
                    case NetPackageTELock.TELockType.LockServer:
                        if (StorageManager.ActiveCoroutines.TryGetValue(_blockPos, out var coroutine))
                        {
                            _log.Trace($"Active coroutine detected at {_blockPos}; stopping and removing it.");
                            _ = StorageManager.ActiveCoroutines.Remove(_blockPos);
                            ThreadManager.StopCoroutine(coroutine);
                        }
                        break;
                    case NetPackageTELock.TELockType.UnlockServer:
                        StorageManager.Distribute(___clrIdx, _blockPos);
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error("Postfix", e);
            }
        }
    }
}
