using System;
using System.Collections;
using UnityEngine;

namespace RoboticInbox
{
    internal class StorageManager
    {
        private static readonly ModLog<StorageManager> _log = new ModLog<StorageManager>();

        private static readonly int yMin = 0;
        private static readonly int yMax = 253; // Block.CanPlaceBlockAt treats 253 as maximum height

        public static int InboxBlockId { get; private set; }
        public static int SecureInboxBlockId { get; private set; }
        public static int LandClaimRadius { get; private set; }
        public static int InboxRange { get; private set; } = 5;

        internal static void OnGameStartDone()
        {
            InboxBlockId = Block.nameIdMapping.GetIdForName("cntRoboticInbox");
            SecureInboxBlockId = Block.nameIdMapping.GetIdForName("cntSecureRoboticInbox");

            var size = GameStats.GetInt(EnumGameStats.LandClaimSize);
            LandClaimRadius = (size % 2 == 1 ? size - 1 : size) / 2;
            _log.Debug($"LandClaimRadius found to be {LandClaimRadius}m");
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
            if (SecureInboxBlockId != source.blockValue.Block.blockID)
            {
                return; // only focus on robotic inbox blocks which are not broken
            }
            _log.Debug($"TileEntity block id found to match {(SecureInboxBlockId != source.blockValue.Block.blockID ? InboxBlockId : SecureInboxBlockId)}");
            if (!TryCastAsContainer(source, out var sourceContainer))
            {
                _log.Debug($"TileEntity at {sourcePos} could not be converted into a TileEntityLootContainer.");
                return;
            }
            if (sourceContainer.IsUserAccessing())
            {
                _log.Debug($"TileEntity at {sourcePos} is currently being accessed.");
                return;
            }

            // Limit min/max to only points **within** the same LCB as the source
            if (!GetBoundsWithinWorldAndLandClaim(sourcePos, out var min, out var max))
            {
                _log.Debug($"GetBoundsWithinWorldAndLandClaim found that the source position was not within a land claim");
                return; // source pos was not within a land claim
            }
            _log.Debug($"GetBoundsWithinWorldAndLandClaim returned min: {min}, max: {max}");
            _ = ThreadManager.StartCoroutine(OrganizeCoroutine(clrIdx, sourcePos, sourceContainer, min, max));
        }

        internal static bool TryGetActiveLcbCoordsContainingPos(Vector3i sourcePos, out Vector3i landClaimPos)
        {
            var _world = GameManager.Instance.World;
            foreach (var kvp in GameManager.Instance.persistentPlayers.Players)
            {
                if (!_world.IsLandProtectionValidForPlayer(kvp.Value))
                {
                    continue; // this player has been offline too long
                }
                foreach (var lcb in kvp.Value.GetLandProtectionBlocks())
                {
                    if (sourcePos.x >= lcb.x - LandClaimRadius &&
                        sourcePos.x <= lcb.x + LandClaimRadius &&
                        sourcePos.z >= lcb.z - LandClaimRadius &&
                        sourcePos.z <= lcb.z + LandClaimRadius)
                    {
                        landClaimPos = lcb;
                        return true;
                    }
                }
            }
            landClaimPos = default;
            return false;
        }

        internal static bool IsRoboticInbox(int blockId)
        {
            return SecureInboxBlockId == blockId || InboxBlockId == blockId;
        }

        private static bool GetBoundsWithinWorldAndLandClaim(Vector3i source, out Vector3i min, out Vector3i max)
        {
            min = max = default;
            if (!TryGetActiveLcbCoordsContainingPos(source, out var lcb))
            {
                return false;
            }

            // The following logic comes from World.ClampToValidWorldPos (mostly).
            // May need to re-assess this code if TFP change it in the future.
            if (!GameManager.Instance.World.GetWorldExtent(out var _minMapSize, out var _maxMapSize))
            {
                _log.Debug("Typical map world extent does not seem to be present; expecting map to be playtesting or Navezgane.");
            }
            if (GamePrefs.GetString(EnumGamePrefs.GameWorld) == "Navezgane")
            {
                _minMapSize = new Vector3i(-2400, _minMapSize.y, -2400);
                _maxMapSize = new Vector3i(2400, _maxMapSize.y, 2400);
            }
            else if (!GameUtils.IsPlaytesting())
            {
                _minMapSize = new Vector3i(_minMapSize.x + 320, _minMapSize.y, _minMapSize.z + 320);
                _maxMapSize = new Vector3i(_maxMapSize.x - 320, _maxMapSize.y, _maxMapSize.z - 320);
            }
            _log.Debug($"minMapSize: {_minMapSize}, maxMapSize: {_maxMapSize}");

            min.x = FastMax(source.x - InboxRange, lcb.x - LandClaimRadius, _minMapSize.x);
            min.z = FastMax(source.z - InboxRange, lcb.z - LandClaimRadius, _minMapSize.z);
            min.y = FastMax(source.y - InboxRange, yMin, _minMapSize.y);
            max.x = FastMin(source.x + InboxRange, lcb.x + LandClaimRadius, _maxMapSize.x);
            max.z = FastMin(source.z + InboxRange, lcb.z + LandClaimRadius, _maxMapSize.z);
            max.y = FastMin(source.y + InboxRange, yMax, _maxMapSize.y);
            _log.Debug($"clampedMin: {min}, clampedMax: {max}");
            return true;
        }

        public static int FastMax(int v1, int v2, int v3)
        {
            return Utils.FastMax(v1, Utils.FastMax(v2, v3));
        }

        public static int FastMin(int v1, int v2, int v3)
        {
            return Utils.FastMin(v1, Utils.FastMin(v2, v3));
        }

        private static IEnumerator OrganizeCoroutine(int clrIdx, Vector3i sourcePos, TileEntityLootContainer source, Vector3i min, Vector3i max)
        {
            Vector3i targetPos;
            for (var x = min.x; x <= max.x; x++)
            {
                targetPos.x = x;
                for (var y = min.y; y <= max.y; y++)
                {
                    targetPos.y = y;
                    for (var z = min.z; z <= max.z; z++)
                    {
                        targetPos.z = z;
                        if (targetPos != sourcePos)
                        { // avoid targeting self (duh)
                            var target = GameManager.Instance.World.GetTileEntity(clrIdx, targetPos);
                            if (VerifyContainer(target, out var targetContainer))
                            {
                                yield return null; // free up frames just before each distribute
                                Distribute(source, targetContainer, targetPos);
                            }
                        }
                    }
                }
            }
        }

        private static bool VerifyContainer(TileEntity entity, out TileEntityLootContainer tileEntityLootContainer)
        {
            return TryCastAsContainer(entity, out tileEntityLootContainer)
                && tileEntityLootContainer.bPlayerStorage
                && !tileEntityLootContainer.bPlayerBackpack
                && !IsRoboticInbox(entity.blockValue.Block.blockID);
        }

        private static void Distribute(TileEntityLootContainer source, TileEntityLootContainer target, Vector3i targetPos)
        {
            if (target.IsUserAccessing())
            {
                HandleUserAccessing(targetPos, target);
                return;
            }

            if (!CanAccess(source, target, targetPos))
            {
                GameManager.Instance.PlaySoundAtPositionServer(targetPos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
                return;
            }

            try
            {
                var totalItemsTransferred = 0;

                source.SetUserAccessing(true);
                target.SetUserAccessing(true);

                for (var s = 0; s < source.items.Length; s++)
                {
                    if (ItemStack.Empty.Equals(source.items[s])) { continue; }
                    var foundMatch = false;
                    var fullyMoved = false;
                    var startCount = source.items[s].count;
                    // try to stack source itemStack into any matching target itemStacks
                    for (var t = 0; t < target.items.Length; t++)
                    {
                        if (target.items[t].itemValue.ItemClass != source.items[s].itemValue.ItemClass)
                        {
                            // Move on to next target if this target doesn't match source type
                            continue;
                        }
                        foundMatch = true;
                        if (target.TryStackItem(t, source.items[s]))
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
                        if (target.AddItem(source.items[s]))
                        {
                            // Remaining items could be moved to empty slot
                            source.UpdateSlot(s, ItemStack.Empty);
                            totalItemsTransferred += startCount;
                        }
                        else
                        {
                            // Remaining items could not be moved to empty slot
                            totalItemsTransferred += startCount - source.items[s].count;
                        }
                    }
                }
                if (totalItemsTransferred > 0)
                {
                    target.items = StackSortUtil.CombineAndSortStacks(target.items);
                    HandleTransferred(targetPos, target, totalItemsTransferred);
                }
            }
            catch (Exception e)
            {
                _log.Error("encountered issues organizing with Inbox", e);
            }
            finally
            {
                source.SetUserAccessing(false);
                target.SetUserAccessing(false);
            }
        }

        private static bool CanAccess(TileEntity source, TileEntity target, Vector3i targetPos)
        {
            var sourceIsLockable = ToLock(source, out var sourceLock);
            var targetIsLockable = ToLock(target, out var targetLock);

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

        private static void HandleUserAccessing(Vector3i pos, TileEntity target)
        {
            switch (target)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    _ = ThreadManager.StartCoroutine(ShowTemporaryText(3, signedContainer, "Can't Distribute: Currently In Use"));
                    break;
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandleTargetLockedWithoutPassword(Vector3i pos, TileEntity target)
        {
            switch (target)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    _ = ThreadManager.StartCoroutine(ShowTemporaryText(3, signedContainer, "Can't Distribute: Locked and has no password"));
                    break;
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandleTargetLockedWhileSourceIsNot(Vector3i pos, TileEntity target)
        {
            switch (target)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    _ = ThreadManager.StartCoroutine(ShowTemporaryText(3, signedContainer, "Can't Distribute: Locked but Inbox isn't"));
                    break;
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandlePasswordMismatch(Vector3i pos, TileEntity target)
        {
            switch (target)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    _ = ThreadManager.StartCoroutine(ShowTemporaryText(3, signedContainer, "Can't Distribute: Passwords Don't match"));
                    break;
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
        }

        private static void HandleTransferred(Vector3i pos, TileEntityLootContainer target, int totalItemsTransferred)
        {
            switch (target)
            {
                case TileEntitySecureLootContainerSigned signedContainer:
                    // target.GetTileEntityType() == TileEntityType.SecureLootSigned
                    _ = ThreadManager.StartCoroutine(ShowTemporaryText(2, signedContainer, $"Added + Sorted\n{totalItemsTransferred} Item{(totalItemsTransferred > 1 ? "s" : "")}"));
                    break;
            }
            GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_close", AudioRolloffMode.Logarithmic, 5);
        }

        private static IEnumerator ShowTemporaryText(float seconds, TileEntitySecureLootContainerSigned container, string text)
        {
            _log.Debug("called coroutine");

            var originalText = container.GetText();
            container.SetText(text, true); // update with new text (and sync to players)

            // update server with original text again in case of shutdown or container destroy before yield completes
            container.SetText(originalText, false); // update with original text (and do NOT sync to players)
            yield return new WaitForSeconds(seconds);
            container?.SetText(container.GetText(), true); // sync original text to players
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

        private static bool TryCastAsContainer(TileEntity entity, out TileEntityLootContainer typed)
        {
            if (entity != null && (
                entity.GetTileEntityType() == TileEntityType.Loot ||
                entity.GetTileEntityType() == TileEntityType.SecureLoot ||
                entity.GetTileEntityType() == TileEntityType.SecureLootSigned
            ))
            {
                typed = entity as TileEntityLootContainer;
                return true;
            }
            typed = null;
            return false;
        }

        private static bool ToLock(TileEntity entity, out ILockable typed)
        {
            if (entity.GetTileEntityType() == TileEntityType.SecureLoot ||
                entity.GetTileEntityType() == TileEntityType.SecureLootSigned)
            {
                typed = entity as ILockable;
                return true;
            }
            typed = null;
            return false;
        }
    }
}
