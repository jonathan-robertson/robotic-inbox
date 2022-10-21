using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoboticInbox {
    internal class StorageManager {
        private static readonly ModLog log = new ModLog(typeof(StorageManager));
        private static readonly Dictionary<Vector3i, string> OriginalText = new Dictionary<Vector3i, string>();

        private static readonly int yMin = 0;
        private static readonly int yMax = 253;

        public static int InboxBlockId { get; private set; }
        public static int SecureInboxBlockId { get; private set; }
        public static int LandClaimRadius { get; private set; }
        public static int InboxRange { get; private set; } = 5;

        internal static void OnGameStartDone() {
            try {
                InboxBlockId = Block.nameIdMapping.GetIdForName("cntRoboticInbox");
                SecureInboxBlockId = Block.nameIdMapping.GetIdForName("cntSecureRoboticInbox");

                var size = GameStats.GetInt(EnumGameStats.LandClaimSize);
                LandClaimRadius = (size % 2 == 1 ? size - 1 : size) / 2;
            } catch (Exception e) {
                log.Error("Error OnGameStartDone", e);
            }
        }

        internal static void Distribute(int clrIdx, Vector3i sourcePos) {
            var source = GameManager.Instance.World.GetTileEntity(clrIdx, sourcePos);
            if (source == null || source.blockValue.Block == null) {
                return;
            }
            if (SecureInboxBlockId != source.blockValue.Block.blockID
                && InboxBlockId != source.blockValue.Block.blockID) {
                return;
            }
            if (!ToContainer(source, out var sourceContainer)) {
                return;
            }
            if (sourceContainer.IsUserAccessing()) {
                return;
            }

            // Limit min/max to only points **within** the same LCB as the source
            if (!GetBoundsWithinLandClaim(sourcePos, out var min, out var max)) {
                return; // source pos was not within a land claim
            }
            Vector3i targetPos;
            for (int x = min.x; x <= max.x; x++) {
                targetPos.x = x;
                for (int y = min.y; y <= max.y; y++) {
                    targetPos.y = y;
                    for (int z = min.z; z <= max.z; z++) {
                        targetPos.z = z;
                        if (targetPos != sourcePos) { // avoid targeting self (duh)
                            var target = GameManager.Instance.World.GetTileEntity(clrIdx, targetPos);
                            if (VerifyContainer(target, targetPos)) {
                                Distribute(source, target, targetPos);
                            }
                        }
                    }
                }
            }
        }

        private static bool GetBoundsWithinLandClaim(Vector3i source, out Vector3i min, out Vector3i max) {
            min = max = Vector3i.zero;
            foreach (var kvp in GameManager.Instance.persistentPlayers.Players) {
                foreach (var lcb in kvp.Value.GetLandProtectionBlocks()) {
                    if (source.x >= lcb.x - LandClaimRadius &&
                        source.x <= lcb.x + LandClaimRadius &&
                        source.z >= lcb.y - LandClaimRadius &&
                        source.z <= lcb.z + LandClaimRadius) {
                        min.x = Utils.FastMax(source.x - InboxRange, lcb.x - LandClaimRadius);
                        min.z = Utils.FastMax(source.z - InboxRange, lcb.z - LandClaimRadius);
                        min.y = Utils.FastMax(source.y - InboxRange, yMin);
                        max.x = Utils.FastMin(source.x + InboxRange, lcb.x + LandClaimRadius);
                        max.z = Utils.FastMin(source.z + InboxRange, lcb.z + LandClaimRadius);
                        max.y = Utils.FastMin(source.y + InboxRange, yMax);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void Distribute(TileEntity sourceTileEntity, TileEntity targetTileEntity, Vector3i targetPos) {
            if (!CanAccess(sourceTileEntity, targetTileEntity, targetPos)) {
                //log.Debug($"Access Denied for {targetPos}");
                GameManager.Instance.PlaySoundAtPositionServer(targetPos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
                return;
            }
            //log.Debug($"Access Granted to {targetPos}");

            var source = sourceTileEntity as TileEntityLootContainer;
            var target = targetTileEntity as TileEntityLootContainer;

            try {
                var totalItemsTransferred = 0;

                source.SetUserAccessing(true);
                target.SetUserAccessing(true);
                //log.Debug("[L] added access lock to source and target");

                for (int s = 0; s < source.items.Length; s++) {
                    if (ItemStack.Empty.Equals(source.items[s])) { continue; }
                    var foundMatch = false;
                    var fullyMoved = false;
                    var startCount = source.items[s].count;
                    // try to stack source itemStack into any matching target itemStacks
                    for (int t = 0; t < target.items.Length; t++) {
                        if (target.items[t].itemValue.ItemClass != source.items[s].itemValue.ItemClass) {
                            // Move on to next target if this target doesn't match source type
                            continue;
                        }
                        foundMatch = true;
                        if (target.TryStackItem(t, source.items[s])) {
                            // All items could be stacked
                            totalItemsTransferred += startCount;
                            fullyMoved = true;
                            break;
                        }
                    }
                    // for any items left over in source itemStack, place in a new target slot
                    if (foundMatch && !fullyMoved) {

                        // Not all items could be stacked
                        if (target.AddItem(source.items[s])) {
                            // Remaining items could be moved to empty slot
                            source.UpdateSlot(s, ItemStack.Empty);
                            totalItemsTransferred += startCount;
                        } else {
                            // Remaining items could not be moved to empty slot
                            totalItemsTransferred += startCount - source.items[s].count;
                        }
                    }
                }
                if (totalItemsTransferred > 0) {
                    target.items = StackSortUtil.CombineAndSortStacks(target.items);
                    ThreadManager.StartCoroutine(ShowTemporaryText(2, targetPos, targetTileEntity, $"Added + Sorted\n{totalItemsTransferred} Item{(totalItemsTransferred > 1 ? "s" : "")}"));
                    GameManager.Instance.PlaySoundAtPositionServer(targetPos, "vehicle_storage_close", AudioRolloffMode.Logarithmic, 5);
                }
            } catch (Exception e) {
                log.Error("encountered issues organizing with inbox", e);
            } finally {
                source.SetUserAccessing(false);
                target.SetUserAccessing(false);
                //log.Debug("[U] removed access lock from source and target");
            }
        }

        private static bool VerifyContainer(TileEntity entity, Vector3i pos) {
            var targetIsContainer = ToContainer(entity, out var targetContainer);
            if (!targetIsContainer ||
                targetContainer.bPlayerBackpack ||
                !targetContainer.bPlayerStorage) {
                return false;
            }
            if (targetContainer.IsUserAccessing()) {
                ThreadManager.StartCoroutine(ShowTemporaryText(3, pos, entity, "Can't Distribute: Currently In Use"));
                GameManager.Instance.PlaySoundAtPositionServer(pos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
                return false;
            }
            return true;
        }

        private static IEnumerator ShowTemporaryText(float seconds, Vector3i pos, TileEntity entity, string text) {
            if (entity.GetTileEntityType() == TileEntityType.SecureLootSigned) {
                var container = entity as TileEntitySecureLootContainerSigned;
                if (!OriginalText.ContainsKey(pos)) {
                    OriginalText.Add(pos, container.GetText());
                }
                container.SetText(text);
                yield return new WaitForSeconds(seconds);
                if (OriginalText.TryGetValue(pos, out var originalText)) {
                    OriginalText.Remove(pos);
                    container.SetText(originalText);
                }
            }
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
            //log.Debug($"{BlockFace.Top}: {originalTextures[(uint)BlockFace.Top]}, {BlockFace.Bottom}: {originalTextures[(uint)BlockFace.Bottom]}, {BlockFace.North}: {originalTextures[(uint)BlockFace.North]}, {BlockFace.West}: {originalTextures[(uint)BlockFace.West]}, {BlockFace.South}: {originalTextures[(uint)BlockFace.South]}, {BlockFace.East}: {originalTextures[(uint)BlockFace.East]}");
            yield return new WaitForSeconds(seconds);
            foreach (BlockFace _side in blockFaces) {
                GameManager.Instance.SetBlockTextureServer(pos, _side, originalTextures[(uint)_side], -1);
            }
        }
        */

        private static bool CanAccess(TileEntity source, TileEntity target, Vector3i targetPos) {
            var sourceIsLockable = ToLock(source, out var sourceLock);
            var targetIsLockable = ToLock(target, out var targetLock);

            if (!targetIsLockable) {
                return true;
            }

            if (!targetLock.IsLocked()) {
                return true;
            }

            // so target is both lockable and currently locked...

            if (!targetLock.HasPassword()) {
                ThreadManager.StartCoroutine(ShowTemporaryText(3, targetPos, target, "Can't Distribute: Locked and has no password"));
                return false;
            }

            if (!sourceIsLockable || !sourceLock.IsLocked()) {
                ThreadManager.StartCoroutine(ShowTemporaryText(3, targetPos, target, "Can't Distribute: Locked but Inbox isn't"));
                return false;
            }

            if (sourceLock.GetPassword().Equals(targetLock.GetPassword())) {
                return true;
            }

            ThreadManager.StartCoroutine(ShowTemporaryText(3, targetPos, target, "Can't Distribute: Passwords Don't match"));
            return false;
        }

        private static bool ToContainer(TileEntity entity, out TileEntityLootContainer typed) {
            if (entity != null && (
                entity.GetTileEntityType() == TileEntityType.Loot ||
                entity.GetTileEntityType() == TileEntityType.SecureLoot ||
                entity.GetTileEntityType() == TileEntityType.SecureLootSigned
            )) {
                typed = entity as TileEntityLootContainer;
                return true;
            }
            typed = null;
            return false;
        }

        private static bool ToLock(TileEntity entity, out ILockable typed) {
            if (entity.GetTileEntityType() == TileEntityType.SecureLoot ||
                entity.GetTileEntityType() == TileEntityType.SecureLootSigned) {
                typed = entity as ILockable;
                return true;
            }
            typed = null;
            return false;
        }
    }
}
