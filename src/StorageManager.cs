using RoboticInbox.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoboticInbox
{
    internal class StorageManager
    {
        private static readonly ModLog<StorageManager> _log = new ModLog<StorageManager>();

        public const int Y_MIN = 0;
        public const int Y_MAX = 253; // Block.CanPlaceBlockAt treats 253 as maximum height

        private static readonly FastTags<TagGroup.Global> roboticinboxTag = FastTags<TagGroup.Global>.Parse("roboticinbox");
        private static readonly FastTags<TagGroup.Global> roboticinboxinsecureTag = FastTags<TagGroup.Global>.Parse("roboticinboxinsecure");

        public static string MessageTargetContainerInUse { get; private set; } = "Robotic Inbox was [ff8000]unable to organize this container[-] as it was in use.";
        public static string SoundVehicleStorageOpen { get; private set; } = "vehicle_storage_open";
        public static string SoundVehicleStorageClose { get; private set; } = "vehicle_storage_close";

        public static List<int> InboxBlockIds { get; private set; } = new List<int>();
        public static List<int> InsecureInboxBlockIds { get; private set; } = new List<int>();
        public static int LandClaimRadius { get; private set; }
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
                if (kvp.Value.Tags.Test_AnySet(roboticinboxTag))
                {
                    InboxBlockIds.Add(kvp.Value.blockID);
                    _log.Info($"{kvp.Value.blockName} (block id: {kvp.Value.blockID}) verified as a Robotic Inbox Block.");
                }
                else if (kvp.Value.Tags.Test_AnySet(roboticinboxinsecureTag))
                {
                    InsecureInboxBlockIds.Add(kvp.Value.blockID);
                    _log.Info($"{kvp.Value.blockName} (block id: {kvp.Value.blockID}) verified as an Insecure Robotic Inbox Block.");
                }
            }

            var size = GameStats.GetInt(EnumGameStats.LandClaimSize);
            LandClaimRadius = (size % 2 == 1 ? size - 1 : size) / 2;
            _log.Info($"LandClaimRadius found to be {LandClaimRadius}m");
        }

        internal static void Distribute(int clrIdx, Vector3i sourcePos)
        {
            _log.Debug($"Distribute called for tile entity at {sourcePos}");
            var source = GameManager.Instance.World.GetTileEntity(clrIdx, sourcePos);
            if (source == null || source.blockValue.Block == null)
            {
                _log.Debug($"TileEntity not found at {sourcePos}");
                return;
            }
            if (!InboxBlockIds.Contains(source.blockValue.Block.blockID))
            {
                _log.Debug($"!InboxBlockIds.Contains(source.blockValue.Block.blockID) at {sourcePos} -- {InboxBlockIds} does not contain {source.blockValue.Block.blockID}");
                return; // only focus on robotic inbox blocks which are not broken
            }
            _log.Debug($"TileEntity block id confirmed as a Robotic Inbox Block");
            if (!TryCastAsContainer(source, out var sourceContainer))
            {
                _log.Debug($"TileEntity at {sourcePos} could not be converted into a TileEntityLootContainer.");
                return;
            }

            GetBoundsWithinWorldAndLandClaim(sourcePos, out var min, out var max);
            ActiveCoroutines.Add(sourcePos, ThreadManager.StartCoroutine(OrganizeCoroutine(clrIdx, sourcePos, source, sourceContainer, min, max)));
        }

        private static void GetBoundsWithinWorldAndLandClaim(Vector3i source, out Vector3i min, out Vector3i max)
        {
            min = max = default;

            if (!GameManager.Instance.World.GetWorldExtent(out var _minMapSize, out var _maxMapSize))
            {
                _log.Warn("World.GetWorldExtent failed when checking for limits; this is not expected and may indicate an error.");
                return;
            }
            _log.Debug($"minMapSize: {_minMapSize}, maxMapSize: {_maxMapSize}, actualMapSize: {_maxMapSize - _minMapSize}");

            if (SettingsManager.BaseSiphoningProtection && TryGetActiveLandClaimPosContaining(source, out var lcbPos))
            {
                _log.Debug($"Land Claim was found containing {source} (pos: {lcbPos}); clamping to world and land claim coordinates.");
                min.x = FastMax(source.x - SettingsManager.InboxHorizontalRange, lcbPos.x - LandClaimRadius, _minMapSize.x);
                min.z = FastMax(source.z - SettingsManager.InboxHorizontalRange, lcbPos.z - LandClaimRadius, _minMapSize.z);
                min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, Y_MIN, _minMapSize.y);
                max.x = FastMin(source.x + SettingsManager.InboxHorizontalRange, lcbPos.x + LandClaimRadius, _maxMapSize.x);
                max.z = FastMin(source.z + SettingsManager.InboxHorizontalRange, lcbPos.z + LandClaimRadius, _maxMapSize.z);
                max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, Y_MAX, _maxMapSize.y);
                _log.Debug($"clampedMin: {min}, clampedMax: {max}.");
                return;
            }

            _log.Debug($"Land Claim not found containing {source}; clamping to world coordinates only.");
            min.x = Utils.FastMax(source.x - SettingsManager.InboxHorizontalRange, _minMapSize.x);
            min.z = Utils.FastMax(source.z - SettingsManager.InboxHorizontalRange, _minMapSize.z);
            min.y = FastMax(source.y - SettingsManager.InboxVerticalRange, Y_MIN, _minMapSize.y);
            max.x = Utils.FastMin(source.x + SettingsManager.InboxHorizontalRange, _maxMapSize.x);
            max.z = Utils.FastMin(source.z + SettingsManager.InboxHorizontalRange, _maxMapSize.z);
            max.y = FastMin(source.y + SettingsManager.InboxVerticalRange, Y_MAX, _maxMapSize.y);
            _log.Debug($"clampedMin: {min}, clampedMax: {max}.");
            return;
        }

        internal static bool TryGetActiveLandClaimPosContaining(Vector3i sourcePos, out Vector3i lcbPos)
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

        public static int FastMax(int v1, int v2, int v3)
        {
            return Utils.FastMax(v1, Utils.FastMax(v2, v3));
        }

        public static int FastMin(int v1, int v2, int v3)
        {
            return Utils.FastMin(v1, Utils.FastMin(v2, v3));
        }

        private static IEnumerator OrganizeCoroutineNew(int clrIdx, Vector3i sourcePos, TileEntity source, ITileEntityLootable sourceContainer, Vector3i min, Vector3i max)
        {
            if (min == max) { return null; } // end it here

            _ = GameManager.Instance.World;


            // TODO: loop through horizontal squares, radiating out from center
            var x = 0;
            while (true)
            {
                if (x > max.x)
                {

                }
            }

            _ = ActiveCoroutines.Remove(sourcePos);
        }

        private static IEnumerator OrganizeCoroutine(int clrIdx, Vector3i sourcePos, TileEntity source, ITileEntityLootable sourceContainer, Vector3i min, Vector3i max)
        {
            // TODO: optimize this
            // TODO: possibly check at most... 1 slice of x at a time?
            //  see how much time it will take to yield after each vertical cross-section of x/z at a time
            //  test by returning entire map for clamped range and see if it halts zombies
            var world = GameManager.Instance.World;
            Vector3i targetPos;
            for (var y = min.y; y <= max.y; y++)
            {
                targetPos.y = y;
                for (var x = min.x; x <= max.x; x++)
                {
                    targetPos.x = x;
                    for (var z = min.z; z <= max.z; z++)
                    {
                        targetPos.z = z;
                        if (targetPos != sourcePos) // avoid targeting self (duh)
                        {
                            var target = world.GetTileEntity(clrIdx, targetPos);
                            if (VerifyContainer(target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, sourceContainer, sourcePos, target, targetContainer, targetPos);
                            }
                        }
                    }
                    //yield return null; // [way too slow] free up game frame after scanning each y/x column
                }
                yield return null; // free up game frame after scanning each y slice
            }
            _ = ActiveCoroutines.Remove(sourcePos);
        }

        private static bool VerifyContainer(TileEntity entity, out ITileEntityLootable tileEntityLootContainer)
        {
            return TryCastAsContainer(entity, out tileEntityLootContainer)
                && tileEntityLootContainer.bPlayerStorage
                && !tileEntityLootContainer.bPlayerBackpack
                && !IsRoboticInbox(entity.blockValue.Block.blockID);
        }

        internal static bool IsRoboticInbox(int blockId)
        {
            return InboxBlockIds.Contains(blockId) || InsecureInboxBlockIds.Contains(blockId);
        }

        private static bool CheckAndHandleInUse(TileEntity source, Vector3i sourcePos, TileEntity target, Vector3i targetPos)
        {
            var entityIdInSourceContainer = GameManager.Instance.GetEntityIDForLockedTileEntity(source);
            if (entityIdInSourceContainer != -1)
            {
                _log.Trace($"player {entityIdInSourceContainer} is currently accessing source container at {sourcePos}; skipping");
                GameManager.Instance.PlaySoundAtPositionServer(sourcePos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
                return true;
            }
            var entityIdInTargetContainer = GameManager.Instance.GetEntityIDForLockedTileEntity(target);
            if (entityIdInTargetContainer != -1)
            {
                _log.Trace($"player {entityIdInTargetContainer} is currently accessing target container at {targetPos}; skipping");
                var clientInfo = ConnectionManager.Instance.Clients.ForEntityId(entityIdInTargetContainer);
                if (clientInfo == null)
                {
                    GameManager.ShowTooltip(GameManager.Instance.World.GetPrimaryPlayer(), MessageTargetContainerInUse);
                }
                else
                {
                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageShowToolbeltMessage>().Setup(MessageTargetContainerInUse, SoundVehicleStorageOpen));
                }
                GameManager.Instance.PlaySoundAtPositionServer(targetPos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
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
                GameManager.Instance.PlaySoundAtPositionServer(targetPos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
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
                    HandleTransferred(targetPos, target, totalItemsTransferred);
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
                HandleTargetLockedWithoutPassword(targetPos, target);
                return false;
            }

            if (!sourceIsLockable || !sourceLock.IsLocked())
            {
                HandleTargetLockedWhileSourceIsNot(targetPos, target);
                return false;
            }

            if (sourceLock.GetPassword().Equals(targetLock.GetPassword()))
            {
                return true;
            }

            HandlePasswordMismatch(targetPos, target);
            return false;
        }

        private static bool TryCastToITileEntitySignable(TileEntity tileEntity, out ITileEntitySignable signable)
        {
            switch (tileEntity)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    signable = signedContainer;
                    return true;
                case TileEntityComposite composite:
                    signable = composite.GetFeature<TEFeatureSignable>();
                    return true;
            }
            signable = null;
            return false;
        }

        private static bool TryGetOwner(TileEntity target, out PlatformUserIdentifierAbs owner)
        {
            switch (target)
            {
                case TileEntityComposite composite:
                    owner = composite.Owner;
                    return true;
                case TileEntitySecureLootContainerSigned tileEntitySecureLootContainerSigned:
                    owner = tileEntitySecureLootContainerSigned.ownerID;
                    return true;
            }
            owner = null;
            return false;
        }

        private static void HandleTargetLockedWithoutPassword(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                _ = ThreadManager.StartCoroutine(ShowTemporaryText(SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked without password")); // TODO: test 2
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandleTargetLockedWhileSourceIsNot(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                _ = ThreadManager.StartCoroutine(ShowTemporaryText(SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked but Inbox is not"));
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandlePasswordMismatch(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                _ = ThreadManager.StartCoroutine(ShowTemporaryText(SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Password Does not match Inbox"));
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandleTransferred(Vector3i pos, TileEntity target, int totalItemsTransferred)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                _ = ThreadManager.StartCoroutine(ShowTemporaryText(SettingsManager.DistributionSuccessNoticeTime, signable, owner, $"Added + Sorted\n{totalItemsTransferred} Item{(totalItemsTransferred > 1 ? "s" : "")}"));
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageClose, AudioRolloffMode.Logarithmic, 5);
        }

        private static IEnumerator ShowTemporaryText(float seconds, ITileEntitySignable signableEntity, PlatformUserIdentifierAbs signingPlayer, string text)
        {
            if (signingPlayer == null)
            {
                _log.Trace("No Signing Player found; cannot update helper text on target container");
                yield return null;
            }

            var originalText = signableEntity.GetAuthoredText().Text;
            _log.Trace($"setting temporary text for {signableEntity.blockValue.Block.blockName} to:\n{text}");
            signableEntity.SetText(text, true, signingPlayer); // update with new text (and sync to players)
            yield return new WaitForSeconds(seconds);
            _log.Trace($"returning original text for {signableEntity.blockValue.Block.blockName} to:\n{originalText}");
            signableEntity?.SetText(originalText, true, signingPlayer); // sync original text to players
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

        // TODO: as a safety precaution, lock source and target when transferring items between the two of them
        private static void MarkInUse(Vector3i blockPos, int lootEntityId, int entityIdThatOpenedIt)
        {
            //GameManager.Instance.TELockServer(GameManager.Instance.World.ChunkCache.ClusterIdx, blockPos, -1, -2);
        }

        // TODO: after transfer, unlock source and target
        private static void MarkNotInUse(Vector3i blockPos, int lootEntityId)
        {
            //GameManager.Instance.TEUnlockServer(GameManager.Instance.World.ChunkCache.ClusterIdx, blockPos, -1);
        }
    }
}
