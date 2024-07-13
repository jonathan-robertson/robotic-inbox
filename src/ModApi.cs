using HarmonyLib;
using RoboticInbox.Utilities;
using System;
using System.Reflection;

namespace RoboticInbox
{
    public class ModApi : IModApi
    {
        private const string ModMaintainer = "kanaverum";
        private const string SupportLink = "https://discord.gg/hYa2sNHXya";

        private static readonly ModLog<ModApi> _log = new ModLog<ModApi>();

        public static bool DebugMode { get; set; } = false;

        public void InitMod(Mod _modInstance)
        {
            try
            {
                new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
                SettingsManager.Load();
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
                ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
            }
            catch (Exception e)
            {
                _log.Error($"Failed to start up Robotic Inbox mod; take a look at logs for guidance but feel free to also reach out to the mod maintainer {ModMaintainer} via {SupportLink}", e);
            }
        }

        private void OnGameStartDone()
        {
            try
            {
                StorageManager.OnGameStartDone();
            }
            catch (Exception e)
            {
                _log.Error("OnGameStartDone Failed", e);
            }
        }

        private void OnPlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            try
            {
                if (clientInfo == null)
                {
                    switch (respawnType)
                    {
                        case RespawnType.NewGame: // local player creating a new game
                        case RespawnType.LoadedGame: // local player loading existing game
                        case RespawnType.Died: // existing player returned from death
                            for (var i = 0; i < GameManager.Instance.World.GetLocalPlayers().Count; i++)
                            {
                                SettingsManager.PropagateHorizontalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
                                SettingsManager.PropagateVerticalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
                            }
                            break;
                    }
                }
                else
                {
                    if (!GameManager.Instance.World.Players.dict.TryGetValue(clientInfo.entityId, out var player) || !player.IsAlive())
                    {
                        return; // player not found or player not ready
                    }

                    switch (respawnType)
                    {
                        case RespawnType.EnterMultiplayer: // first-time login for new player
                        case RespawnType.JoinMultiplayer: // existing player rejoining
                        case RespawnType.Died: // existing player returned from death

                            SettingsManager.PropagateHorizontalRange(player);
                            SettingsManager.PropagateVerticalRange(player);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Failed to handle PlayerSpawnedInWorld event.", e);
            }
        }

        private void OnGameShutdown()
        {
            try
            {
                if (StorageManager.ActiveCoroutines.Count == 0)
                {
                    _log.Info("No coroutines needed to be stopped for shutdown.");
                    return;
                }
                _log.Info($"Stopping {StorageManager.ActiveCoroutines.Count} live coroutines for shutdown.");
                foreach (var kvp in StorageManager.ActiveCoroutines)
                {
                    ThreadManager.StopCoroutine(kvp.Value);
                }
                _log.Info($"All coroutines stopped for shutdown.");
            }
            catch (Exception e)
            {
                _log.Error("OnGameShutdown Failed", e);
            }
        }
    }
}
