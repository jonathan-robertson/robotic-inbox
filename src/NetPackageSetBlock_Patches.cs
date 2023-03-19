using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboticInbox
{
    [HarmonyPatch(typeof(NetPackageSetBlock), "ProcessPackage")]
    internal class NetPackage_SetBlockProcessPackage_Patch
    {
        private static readonly ModLog<NetPackage_SetBlockProcessPackage_Patch> _log = new ModLog<NetPackage_SetBlockProcessPackage_Patch>();
        private static readonly string _buffNotifyRoboticInboxNotInLcbName = "notifyRoboticInboxNotInLcb";

        // cached to avoid object creation
        private static readonly List<BlockChangeInfo> _allowedBlockChanges = new List<BlockChangeInfo>();
        private static readonly List<BlockChangeInfo> _blocksToReturn = new List<BlockChangeInfo>();
        private static readonly List<BlockChangeInfo> _airToPlace = new List<BlockChangeInfo>();
        private static readonly Dictionary<string, ItemStack> _cachedItemStacks = new Dictionary<string, ItemStack>();

        public static bool Prefix(World _world, GameManager _callbacks, PlatformUserIdentifierAbs ___persistentPlayerId, List<BlockChangeInfo> ___blockChanges, int ___localPlayerThatChanged)
        {
            try
            {
                _log.Debug($"Prefix [NetPackageSetBlock.ProcessPackage] called with {___blockChanges.Count} changes");

                if (!TryFilterAndProcessBlocks(___blockChanges, out ___blockChanges))
                {
                    _log.Debug("no changes will need to be reversed");
                    return true; // no changes to process
                }
                _log.Debug($"{_airToPlace.Count} blocks will be deleted, {_blocksToReturn.Count} will be returned, and {___blockChanges.Count} changes will be allowed to stay");

                if (!_callbacks.persistentPlayers.Players.TryGetValue(___persistentPlayerId, out var persistentPlayerData))
                {
                    _log.Warn($"Player data not present for {___persistentPlayerId} - even though this player placed a block just now... hacking?");
                    return true; // need player data
                }
                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(persistentPlayerData.EntityId);
                if (clientInfo == null)
                {
                    _log.Warn($"ClientInfo not present for {___persistentPlayerId} - even though this player placed a block just now... hacking?");
                    return true; // need clientInfo
                }
                _log.Debug($"Connection acquired for player {clientInfo.entityId}; about to delete {_airToPlace.Count} blocks.");
                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageSetBlock>().Setup(persistentPlayerData, _airToPlace, ___localPlayerThatChanged));

                ReturnBlocks(clientInfo, _world);
                return ___blockChanges.Count > 0; // only allow further processing and broadcast to other players if there are any block changes not already reversed
            }
            catch (Exception e)
            {
                _log.Error($"Failed to handle prefix for NetPackageSetBlock.ProcessPackage on behalf of ___persistentPlayerId: {___persistentPlayerId} / ___localPlayerThatChanged: {___localPlayerThatChanged}", e);
                return true;
            }
            finally
            {
                _allowedBlockChanges.Clear();
                _airToPlace.Clear();
                _blocksToReturn.Clear();
            }
        }

        private static bool TryFilterAndProcessBlocks(List<BlockChangeInfo> blockChanges, out List<BlockChangeInfo> allowedChanges)
        {
            foreach (var blockChangeInfo in blockChanges)
            {
                if (!blockChangeInfo.bChangeBlockValue)
                {
                    _log.Debug("block value did not change; ignoring");
                    _allowedBlockChanges.Add(blockChangeInfo);
                    continue; // only monitor block creations
                }

                if (!StorageManager.IsRoboticInbox(blockChangeInfo.blockValue.Block.blockID))
                {
                    _log.Debug("block value did change but is not the type of block we care about; ignoring");
                    _allowedBlockChanges.Add(blockChangeInfo);
                    continue; // only monitor inbox blocks
                }

                if (StorageManager.TryGetActiveLcbCoordsContainingPos(blockChangeInfo.pos, out _))
                {
                    _log.Debug("robotic inbox found to be within range of an active lcb");
                    _allowedBlockChanges.Add(blockChangeInfo);
                    continue; // allow blocks within range of an active lcb
                }

                _log.Debug("robotic inbox found to be outside the range of any active lcb");
                _blocksToReturn.Add(blockChangeInfo);

                var airBlock = new BlockChangeInfo
                {
                    pos = blockChangeInfo.pos,
                    bChangeBlockValue = true,
                    bUpdateLight = true,
                };
                _log.Debug($"preparing air block for {blockChangeInfo.pos}");
                _airToPlace.Add(airBlock); // TODO: NOTE THAT THIS DOES NOT TAKE MULTIDIM BLOCKS INTO ACCOUNT
            }
            allowedChanges = _allowedBlockChanges;
            return allowedChanges.Count != blockChanges.Count;
        }

        private static void ReturnBlocks(ClientInfo clientInfo, World world)
        {
            foreach (var blockChangeInfo in _blocksToReturn)
            {
                GiveOneItem(clientInfo, blockChangeInfo);
            }
            if (_blocksToReturn.Count > 0 && world.Players.dict.TryGetValue(clientInfo.entityId, out var player))
            {
                _log.Debug($"returned {_blocksToReturn.Count} blocks back to {player.GetDebugName()}");
                _ = player.Buffs.AddBuff(_buffNotifyRoboticInboxNotInLcbName);
            }
        }

        private static void GiveOneItem(ClientInfo clientInfo, BlockChangeInfo blockChangeInfo)
        {
            var name = blockChangeInfo.blockValue.Block.GetBlockName();
            if (!_cachedItemStacks.TryGetValue(name, out var itemStack))
            {
                itemStack = new ItemStack(ItemClass.GetItem(name, true), 1);
                _cachedItemStacks.Add(name, itemStack);
            }
            GiveItemStack(clientInfo, blockChangeInfo.pos, itemStack);
        }

        internal static void GiveItemStack(ClientInfo clientInfo, Vector3i pos, ItemStack itemStack)
        {
            var entityId = EntityFactory.nextEntityID++;
            GameManager.Instance.World.SpawnEntityInWorld((EntityItem)EntityFactory.CreateEntity(new EntityCreationData
            {
                entityClass = EntityClass.FromString("item"),
                id = entityId,
                itemStack = itemStack,
                pos = pos,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = clientInfo.entityId
            }));
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityId, clientInfo.entityId));
            _ = GameManager.Instance.World.RemoveEntity(entityId, EnumRemoveEntityReason.Despawned);
        }
    }
}
