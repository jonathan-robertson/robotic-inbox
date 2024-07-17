using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoboticInbox.Utilities
{
    internal class StorageManager
    {
        private static readonly ModLog<StorageManager> _log = new ModLog<StorageManager>();

        public const int Y_MIN = 0;
        public const int Y_MAX = 253; // Block.CanPlaceBlockAt treats 253 as maximum height

        private static FastTags<TagGroup.Global> RoboticinboxTag { get; } = FastTags<TagGroup.Global>.Parse("roboticinbox");
        private static FastTags<TagGroup.Global> RoboticinboxinsecureTag { get; } = FastTags<TagGroup.Global>.Parse("roboticinboxinsecure");
        private static List<int> InboxBlockIds { get; } = new List<int>();
        private static List<int> InsecureInboxBlockIds { get; } = new List<int>();
        private static int LandClaimRadius { get; set; }

        public static Dictionary<Vector3i, Coroutine> ActiveCoroutines { get; private set; } = new Dictionary<Vector3i, Coroutine>();

        internal static void OnGameStartDone()
        {
            if (!ConnectionManager.Instance.IsServer)
            {
                _log.Warn("Mod recognizes you as a client, so this locally installed mod will be inactive until you host a game.");
                return;
            }
            _log.Info("Mod recognizes you as the host, so it will begin managing containers.");

            _log.Info("Attempting to register block IDs for Mod.");
            foreach (var kvp in Block.nameToBlock)
            {
                if (kvp.Value.Tags.Test_AnySet(RoboticinboxTag))
                {
                    InboxBlockIds.Add(kvp.Value.blockID);
                    _log.Info($"{kvp.Value.blockName} (block id: {kvp.Value.blockID}) verified as a Robotic Inbox Block.");
                }
                else if (HasRoboticInboxInsecureTag(kvp.Value))
                {
                    InsecureInboxBlockIds.Add(kvp.Value.blockID);
                    _log.Info($"{kvp.Value.blockName} (block id: {kvp.Value.blockID}) verified as an Insecure Robotic Inbox Block.");
                }
            }

            var size = GameStats.GetInt(EnumGameStats.LandClaimSize);
            LandClaimRadius = (size % 2 == 1 ? size - 1 : size) / 2;
            _log.Info($"LandClaimRadius found to be {LandClaimRadius}m");
        }

        internal static bool HasRoboticInboxSecureTag(Block block)
        {
            return block.Tags.Test_AnySet(RoboticinboxTag);
        }

        internal static bool HasRoboticInboxInsecureTag(Block block)
        {
            return block.Tags.Test_AnySet(RoboticinboxinsecureTag);
        }

        internal static void OnGameManagerApplicationQuit()
        {
            if (ActiveCoroutines.Count > 0)
            {
                _log.Trace($"Stopping {ActiveCoroutines.Count} active coroutines for shutdown.");
                foreach (var kvp in ActiveCoroutines)
                {
                    ThreadManager.StopCoroutine(kvp.Value);
                }
                _log.Trace($"All scanning coroutines stopped for shutdown.");
            }
            else
            {
                _log.Trace("No scanning coroutines needed to be stopped for shutdown.");
            }
        }

        internal static bool IsNotActiveRoboticInbox(ITileEntity tileEntity)
        {
            return !InboxBlockIds.Contains(tileEntity.blockValue.Block.blockID);
        }

        internal static void Distribute(int clrIdx, Vector3i sourcePos)
        {
            _log.Trace($"Distribute called for tile entity at {sourcePos}");
            var world = GameManager.Instance.World;
            var source = world.GetTileEntity(clrIdx, sourcePos);
            if (source == null || source.blockValue.Block == null)
            {
                _log.Trace($"TileEntity not found at {sourcePos}");
                return;
            }
            if (IsNotActiveRoboticInbox(source))
            {
                _log.Trace($"!InboxBlockIds.Contains(source.blockValue.Block.blockID) at {sourcePos} -- InboxBlockIds does not contain {source.blockValue.Block.blockID}");
                return; // only focus on robotic inbox blocks which are not broken
            }
            _log.Trace($"TileEntity block id confirmed as a Robotic Inbox Block");
            if (!TryCastAsContainer(source, out var sourceContainer))
            {
                _log.Trace($"TileEntity at {sourcePos} could not be converted into a TileEntityLootContainer.");
                return;
            }

            GetBoundsWithinWorldAndLandClaim(sourcePos, out var min, out var max);
            if (min == max)
            {
                _log.Trace($"Min and Max ranges to scan for containers are equal, so there is no range to scan containers within.");
                return;
            }
            ActiveCoroutines.Add(sourcePos, ThreadManager.StartCoroutine(OrganizeCoroutine(world, clrIdx, sourcePos, source, sourceContainer, min, max)));
        }

        private static void GetBoundsWithinWorldAndLandClaim(Vector3i source, out Vector3i min, out Vector3i max)
        {
            min = max = default;

            if (!GameManager.Instance.World.GetWorldExtent(out var _minMapSize, out var _maxMapSize))
            {
                _log.Warn("World.GetWorldExtent failed when checking for limits; this is not expected and may indicate an error.");
                return;
            }
            _log.Trace($"minMapSize: {_minMapSize}, maxMapSize: {_maxMapSize}, actualMapSize: {_maxMapSize - _minMapSize}");

            if (SettingsManager.BaseSiphoningProtection && TryGetActiveLandClaimPosContaining(source, out var lcbPos))
            {
                _log.Trace($"Land Claim was found containing {source} (pos: {lcbPos}); clamping to world and land claim coordinates.");
                min.x = FastMax(source.x - SettingsManager.InboxHorizontalRange, lcbPos.x - LandClaimRadius, _minMapSize.x);
                max.x = FastMin(source.x + SettingsManager.InboxHorizontalRange, lcbPos.x + LandClaimRadius, _maxMapSize.x);
                min.z = FastMax(source.z - SettingsManager.InboxHorizontalRange, lcbPos.z - LandClaimRadius, _minMapSize.z);
                max.z = FastMin(source.z + SettingsManager.InboxHorizontalRange, lcbPos.z + LandClaimRadius, _maxMapSize.z);
                if (SettingsManager.InboxVerticalRange == -1)
                {
                    min.y = Utils.FastMax(Y_MIN, _minMapSize.y);
                    max.y = Utils.FastMin(Y_MAX, _maxMapSize.y);
                }
                else
                {
                    min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, Y_MIN, _minMapSize.y);
                    max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, Y_MAX, _maxMapSize.y);
                }
                _log.Trace($"clampedMin: {min}, clampedMax: {max}.");
                return;
            }

            _log.Trace($"Land Claim not found containing {source}; clamping to world coordinates only.");
            min.x = Utils.FastMax(source.x - SettingsManager.InboxHorizontalRange, _minMapSize.x);
            max.x = Utils.FastMin(source.x + SettingsManager.InboxHorizontalRange, _maxMapSize.x);
            min.z = Utils.FastMax(source.z - SettingsManager.InboxHorizontalRange, _minMapSize.z);
            max.z = Utils.FastMin(source.z + SettingsManager.InboxHorizontalRange, _maxMapSize.z);
            if (SettingsManager.InboxVerticalRange == -1)
            {
                min.y = Utils.FastMax(Y_MIN, _minMapSize.y);
                max.y = Utils.FastMin(Y_MAX, _maxMapSize.y);
            }
            else
            {
                min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, Y_MIN, _minMapSize.y);
                max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, Y_MAX, _maxMapSize.y);
            }
            _log.Trace($"clampedMin: {min}, clampedMax: {max}.");
            return;
        }

        private static bool TryGetActiveLandClaimPosContaining(Vector3i sourcePos, out Vector3i lcbPos)
        {
            var _world = GameManager.Instance.World;
            foreach (var kvp in GameManager.Instance.persistentPlayers.Players)
            {
                if (!_world.IsLandProtectionValidForPlayer(kvp.Value))
                {
                    continue; // this player has been offline too long
                }
                foreach (var pos in kvp.Value.GetLandProtectionBlocks())
                {
                    if (sourcePos.x >= pos.x - LandClaimRadius &&
                        sourcePos.x <= pos.x + LandClaimRadius &&
                        sourcePos.z >= pos.z - LandClaimRadius &&
                        sourcePos.z <= pos.z + LandClaimRadius)
                    {
                        lcbPos = pos;
                        return true;
                    }
                }
            }
            lcbPos = default;
            return false;
        }

        private static int FastMax(int v1, int v2, int v3)
        {
            return Utils.FastMax(v1, Utils.FastMax(v2, v3));
        }

        private static int FastMin(int v1, int v2, int v3)
        {
            return Utils.FastMin(v1, Utils.FastMin(v2, v3));
        }

        private static int FindMaxDistance(Vector3i v1, Vector3i v2)
        {
            var maxValue = 0;
            foreach (var val in new List<int>() { v1.z, v1.y, v1.z, v2.x, v2.y, v2.z })
            {
                if (maxValue < val)
                {
                    maxValue = val;
                }
            }
            return maxValue;
        }

        private static bool IsWithin(int x, int y, int z, Vector3i min, Vector3i max)
        {
            return x >= min.x
                && x <= max.x
                && y >= min.y
                && y <= max.y
                && z >= min.z
                && z <= max.z;
        }

        private static IEnumerator OrganizeCoroutine(World world, int clrIdx, Vector3i sourcePos, TileEntity source, ITileEntityLootable sourceContainer, Vector3i min, Vector3i max)
        {
            MarkInUse(source, -1);
            // NOTE: While the repitition is really gross, it does achieve O(n), vs a 'cleaner' set of loops that would be O(n^2)
            _log.Trace($"[{sourcePos}] starting organize coroutine");
            var maxDist = FindMaxDistance(sourcePos - min, max - sourcePos);
            for (var distance = 1; distance <= maxDist; distance++)
            {
                // bottom slice
                if (sourcePos.y - distance >= min.y)
                {
                    var y = sourcePos.y - distance;
                    for (var x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                    {
                        for (var z = Utils.FastMax(sourcePos.z - distance, min.z); z <= Utils.FastMax(sourcePos.z + distance, max.z); z++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                // top slice
                if (sourcePos.y + distance <= max.y)
                {
                    var y = sourcePos.y + distance;
                    for (var x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                    {
                        for (var z = Utils.FastMax(sourcePos.z - distance, min.z); z <= Utils.FastMax(sourcePos.z + distance, max.z); z++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                // south face
                if (sourcePos.z - distance >= min.z)
                {
                    var z = sourcePos.z - distance;
                    for (var y = Utils.FastMax(sourcePos.y - distance + 1, min.y); y <= Utils.FastMin(sourcePos.y + distance - 1, max.y); y++)
                    {
                        for (var x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                // north face
                if (sourcePos.z + distance <= max.z)
                {
                    var z = sourcePos.z + distance;
                    for (var y = Utils.FastMax(sourcePos.y - distance + 1, min.y); y <= Utils.FastMin(sourcePos.y + distance - 1, max.y); y++)
                    {
                        for (var x = Utils.FastMax(sourcePos.x - distance, min.x); x <= Utils.FastMin(sourcePos.x + distance, max.x); x++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                // west face
                if (sourcePos.x - distance >= min.x)
                {
                    var x = sourcePos.x - distance;
                    for (var y = Utils.FastMax(sourcePos.y - distance + 1, min.y); y <= Utils.FastMin(sourcePos.y + distance - 1, max.y); y++)
                    {
                        for (var z = Utils.FastMax(sourcePos.z - distance + 1, min.z); z <= Utils.FastMin(sourcePos.z + distance - 1, max.z); z++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                // east face
                if (sourcePos.x + distance <= max.x)
                {
                    var x = sourcePos.x + distance;
                    for (var y = Utils.FastMax(sourcePos.y - distance + 1, min.y); y <= Utils.FastMin(sourcePos.y + distance - 1, max.y); y++)
                    {
                        for (var z = Utils.FastMax(sourcePos.z - distance + 1, min.z); z <= Utils.FastMin(sourcePos.z + distance - 1, max.z); z++)
                        {
                            if (VerifyContainer(world, clrIdx, x, y, z, out var targetPos, out var target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                }
                yield return null; // free up frames just before distance grows
            }
            _log.Trace($"[{sourcePos}] ending organize coroutine");
            _ = ActiveCoroutines.Remove(sourcePos);
            MarkNotInUse(source);
        }

        private static bool VerifyContainer(World world, int clrIdx, int x, int y, int z, out Vector3i pos, out TileEntity tileEntity, out ITileEntityLootable tileEntityLootContainer)
        {
            pos = new Vector3i(x, y, z);
            tileEntity = world.GetTileEntity(clrIdx, pos);
            return TryCastAsContainer(tileEntity, out tileEntityLootContainer)
                && tileEntityLootContainer.bPlayerStorage
                && !tileEntityLootContainer.bPlayerBackpack
                && !IsRoboticInbox(tileEntity.blockValue.Block.blockID);
        }

        private static bool IsRoboticInbox(int blockId)
        {
            return InboxBlockIds.Contains(blockId) || InsecureInboxBlockIds.Contains(blockId);
        }

        private static bool CheckAndHandleInUse(TileEntity source, Vector3i sourcePos, TileEntity target, Vector3i targetPos)
        {
            var entityIdInSourceContainer = GameManager.Instance.GetEntityIDForLockedTileEntity(source);
            if (entityIdInSourceContainer != -1)
            {
                _log.Trace($"player {entityIdInSourceContainer} is currently accessing source container at {sourcePos}; skipping");
                NotificationManager.PlaySoundVehicleStorageOpen(sourcePos);
                return true;
            }
            var entityIdInTargetContainer = GameManager.Instance.GetEntityIDForLockedTileEntity(target);
            if (entityIdInTargetContainer != -1)
            {
                _log.Trace($"player {entityIdInTargetContainer} is currently accessing target container at {targetPos}; skipping");
                NotificationManager.NotifyInUse(entityIdInTargetContainer, targetPos);
                return true;
            }
            return false;
        }

        private static void Distribute(TileEntity source, ITileEntityLootable sourceContainer, Vector3i sourcePos, TileEntity target, ITileEntityLootable targetContainer, Vector3i targetPos)
        {
            if (CheckAndHandleInUse(source, sourcePos, target, targetPos))
            {
                _log.Trace($"returning early");
                return;
            }

            if (!CanAccess(source, target, targetPos))
            {
                NotificationManager.PlaySoundVehicleStorageOpen(targetPos);
                return;
            }

            try
            {
                var totalItemsTransferred = 0;

                // TODO: do not work on server
                //source.SetUserAccessing(true);
                //target.SetUserAccessing(true);
                //MarkInUse(sourcePos, source.EntityId, source.entityId);
                //MarkInUse(targetPos, target.EntityId, source.entityId);

                for (var s = 0; s < sourceContainer.items.Length; s++)
                {
                    if (ItemStack.Empty.Equals(sourceContainer.items[s])) { continue; }
                    var foundMatch = false;
                    var fullyMoved = false;
                    var startCount = sourceContainer.items[s].count;
                    // try to stack source itemStack into any matching target itemStacks
                    for (var t = 0; t < targetContainer.items.Length; t++)
                    {
                        if (targetContainer.items[t].itemValue.ItemClass != sourceContainer.items[s].itemValue.ItemClass)
                        {
                            // Move on to next target if this target doesn't match source type
                            continue;
                        }
                        foundMatch = true;
                        (var anyMoved, var allMoved) = targetContainer.TryStackItem(t, sourceContainer.items[s]);
                        if (allMoved)
                        {
                            // All items could be stacked
                            totalItemsTransferred += startCount;
                            fullyMoved = true;
                            break;
                        }
                    }
                    // for any items left over in source itemStack, place in a new target slot
                    if (foundMatch && !fullyMoved)
                    {

                        // Not all items could be stacked
                        if (targetContainer.AddItem(sourceContainer.items[s]))
                        {
                            // Remaining items could be moved to empty slot
                            sourceContainer.UpdateSlot(s, ItemStack.Empty);
                            totalItemsTransferred += startCount;
                        }
                        else
                        {
                            // Remaining items could not be moved to empty slot
                            totalItemsTransferred += startCount - sourceContainer.items[s].count;
                        }
                    }
                }
                if (totalItemsTransferred > 0)
                {
                    targetContainer.items = StackSortUtil.CombineAndSortStacks(targetContainer.items);
                    SignManager.HandleTransferred(targetPos, target, totalItemsTransferred);
                }
            }
            catch (Exception e)
            {
                _log.Error("encountered issues organizing with Inbox", e);
            }
            finally
            {
                // TODO: do not work on server
                //source.SetUserAccessing(false);
                //target.SetUserAccessing(false);
                //MarkNotInUse(sourcePos, source.EntityId);
                //MarkNotInUse(targetPos, target.EntityId);
            }
        }

        private static bool CanAccess(TileEntity source, TileEntity target, Vector3i targetPos)
        {
            var sourceIsLockable = TryCastAsLock(source, out var sourceLock);
            var targetIsLockable = TryCastAsLock(target, out var targetLock);

            if (!targetIsLockable)
            {
                return true;
            }

            if (!targetLock.IsLocked())
            {
                return true;
            }

            // so target is both lockable and currently locked...

            if (!targetLock.HasPassword())
            {
                SignManager.HandleTargetLockedWithoutPassword(targetPos, target);
                return false;
            }

            if (!sourceIsLockable || !sourceLock.IsLocked())
            {
                SignManager.HandleTargetLockedWhileSourceIsNot(targetPos, target);
                return false;
            }

            if (sourceLock.GetPassword().Equals(targetLock.GetPassword()))
            {
                return true;
            }

            SignManager.HandlePasswordMismatch(targetPos, target);
            return false;
        }

        /* TODO: provide feedback with textures to non-writable storage
        private static readonly BlockFace[] blockFaces = new BlockFace[] { BlockFace.Top, BlockFace.Bottom, BlockFace.North, BlockFace.West, BlockFace.South, BlockFace.East };
        private static readonly int TextureChalkboard = 115;
        private static readonly int TextureRedConcrete = 156;
        private static readonly int TextureMetalRed = 88;
        private static readonly int TextureConcreteYellow = 152;
        private static readonly int TextureMetalStainlessSteel = 77; // blue
        private static readonly int TextureConcreteBlue = 153;
        private static readonly int TextureConcreteGreen = 160;
        private static IEnumerator DelayUpdateTextures(float seconds, Vector3i pos, int[] originalTextures) {
            //_log.Debug($"{BlockFace.Top}: {originalTextures[(uint)BlockFace.Top]}, {BlockFace.Bottom}: {originalTextures[(uint)BlockFace.Bottom]}, {BlockFace.North}: {originalTextures[(uint)BlockFace.North]}, {BlockFace.West}: {originalTextures[(uint)BlockFace.West]}, {BlockFace.South}: {originalTextures[(uint)BlockFace.South]}, {BlockFace.East}: {originalTextures[(uint)BlockFace.East]}");
            yield return new WaitForSeconds(seconds);
            foreach (BlockFace _side in blockFaces) {
                GameManager.Instance.SetBlockTextureServer(pos, _side, originalTextures[(uint)_side], -1);
            }
        }
        */

        private static bool TryCastAsContainer(TileEntity entity, out ITileEntityLootable typed)
        {
            if (entity != null)
            {
                if (IsCompositeStorage(entity))
                {
                    typed = (entity as TileEntityComposite).GetFeature<TEFeatureStorage>();
                    return typed != null;
                }
                if (IsNonCompositeStorage(entity))
                {
                    typed = entity as ITileEntityLootable;
                    return typed != null;
                }
            }
            typed = null;
            return false;
        }

        private static bool IsCompositeStorage(TileEntity entity)
        {
            return entity.GetTileEntityType() == TileEntityType.Composite
                && (entity as TileEntityComposite).GetFeature<TEFeatureStorage>() != null;
        }

        private static bool IsNonCompositeStorage(TileEntity entity)
        {
            return entity.GetTileEntityType() == TileEntityType.Loot ||
                entity.GetTileEntityType() == TileEntityType.SecureLoot ||
                entity.GetTileEntityType() == TileEntityType.SecureLootSigned;
        }

        private static bool TryCastAsLock(TileEntity entity, out ILockable typed)
        {
            if (IsCompositeLock(entity))
            {
                typed = (entity as TileEntityComposite).GetFeature<TEFeatureLockable>();
                return typed != null;
            }
            else if (IsLock(entity))
            {
                typed = entity as ILockable;
                return typed != null;
            }
            typed = null;
            return false;
        }

        private static bool IsCompositeLock(TileEntity entity)
        {
            return entity != null
                && entity is TileEntityComposite
                && (entity as TileEntityComposite).GetFeature<TEFeatureLockable>() != null;
        }

        private static bool IsLock(TileEntity entity)
        {
            return entity.GetTileEntityType() == TileEntityType.SecureLoot
                || entity.GetTileEntityType() == TileEntityType.SecureLootSigned;
        }

        private static void MarkInUse(ITileEntity tileEntity, int entityIdThatOpenedIt)
        {
            if (!GameManager.Instance.lockedTileEntities.ContainsKey(tileEntity))
            {
                _log.Trace($"MarkInUse: marked tile entity confirmed as being in-use");
                GameManager.Instance.lockedTileEntities.Add(tileEntity, entityIdThatOpenedIt);
            }
            else
            {
                _log.Trace($"MarkInUse: tile entity was already marked as being in-use");
            }
        }

        private static void MarkNotInUse(ITileEntity tileEntity)
        {
            if (GameManager.Instance.lockedTileEntities.Remove(tileEntity))
            {
                _log.Trace($"MarkNotInUse: marked tileEntity as no longer being in use");
            }
            else
            {
                _log.Trace($"MarkNotInUse: tileEntity was not present in lockedTileEntities list");
            }
        }
    }
}
