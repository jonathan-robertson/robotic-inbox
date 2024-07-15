using HarmonyLib;
using RoboticInbox.Utilities;
using System;

namespace RoboticInbox.Patches
{
    [HarmonyPatch(typeof(GameManager), "OnApplicationQuit")]
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

    [HarmonyPatch(typeof(GameManager), "TEUnlockServer")]
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


    [HarmonyPatch(typeof(GameManager), "TELockServer")]
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
}
