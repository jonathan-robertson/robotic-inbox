using System.Collections;
using UnityEngine;

namespace StorageNetwork {
    internal class StorageManager {
        private static readonly ModLog log = new ModLog(typeof(StorageManager));
        private static readonly BlockFace[] blockFaces = new BlockFace[] { BlockFace.Top, BlockFace.Bottom, BlockFace.North, BlockFace.West, BlockFace.South, BlockFace.East };

        private static readonly int TextureChalkboard = 115;
        private static readonly int TextureRedConcrete = 156;
        private static readonly int TextureMetalRed = 88;
        private static readonly int TextureConcreteYellow = 152;
        private static readonly int TextureMetalStainlessSteel = 77; // blue
        private static readonly int TextureConcreteBlue = 153;
        private static readonly int TextureConcreteGreen = 160;

        internal static void Distribute(TileEntity sourceTileEntity, TileEntity targetTileEntity, Vector3i targetPos) {
            log.Debug($"Distribute called for {sourceTileEntity?.EntityId} and {targetTileEntity?.EntityId}");
            if (!CanAccess(sourceTileEntity, targetTileEntity)) {
                log.Debug("Access Denied");
                GameManager.Instance.PlaySoundAtPositionServer(targetPos, "vehicle_storage_open", AudioRolloffMode.Logarithmic, 5);
                return;
            }
            log.Debug("Access Granted");

            var source = sourceTileEntity as TileEntityLootContainer;
            var target = targetTileEntity as TileEntityLootContainer;

            try {
                var targetModified = false;
                var totalItemsTransferred = 0;

                source.SetUserAccessing(true);
                target.SetUserAccessing(true);
                log.Debug("[L] added access lock to source and target");

                // TODO: find a way to highlight/glow/flash blocks when items transferred or unable to transfer for lock
                // BlockHighlighter
                // OOOH! temp change textures and storage text! :D
                //source.blockValue.Block.text

                //targetContainer.SetContainerSize
                //targetContainer.SetUserAccessing(true);
                //targetContainer.TryStackItem()

                for (int s = 0; s < source.items.Length; s++) {
                    if (ItemStack.Empty.Equals(source.items[s])) { continue; }
                    for (int t = 0; t < target.items.Length; t++) {
                        if (target.items[t].itemValue.ItemClass != source.items[s].itemValue.ItemClass) {
                            // Move on to next target if this target doesn't match source type
                            continue;
                        }
                        log.Debug("source item type is present in target");
                        var startCount = source.items[s].count;
                        if (!target.TryStackItem(t, source.items[s])) {
                            // All items could be stacked
                            totalItemsTransferred += startCount;
                            log.Debug($"(+{totalItemsTransferred}) able to move entire item count from source stack to target stack");
                            targetModified = true;
                            break;
                        }

                        // Not all items could be stacked
                        if (target.AddItem(source.items[s])) {
                            // Remaining items could be moved to empty slot
                            source.items[s].Clear();
                            totalItemsTransferred += startCount;
                            log.Debug($"(+{totalItemsTransferred}) able to remove remaining item count to new slot in target");
                            targetModified = true;
                            break;
                        }

                        // Remaining items could not be moved to empty slot
                        totalItemsTransferred += startCount - source.items[s].count;
                        log.Debug($"(+{totalItemsTransferred}) could not move remaining item stack; checking for duplicate stacks in target to see if we can fit any more");
                        targetModified = true;
                    }
                }

                if (targetModified) {
                    StackSortUtil.CombineAndSortStacks(target.items);
                    log.Debug("combined and sorted target stacks");
                    ThreadManager.StartCoroutine(ShowTemporaryText(2, targetTileEntity, $"Added + Sorted\n{totalItemsTransferred} Item{(totalItemsTransferred > 0 ? "s" : "")}"));

                    // TODO: play sound on this entity?
                    // _blockPos.ToVector3(), this.TriggerSound, AudioRolloffMode.Linear, 5
                    // pipe_shotgun_breech_open
                    // pipe_pistol_breech_close
                    // vehicle_storage_open
                    // vehicle_storage_close
                    // open_inventory
                    // open_block_menu
                    // map_zoom_in
                    // map_zoom_out
                    GameManager.Instance.PlaySoundAtPositionServer(targetPos, "vehicle_storage_close", AudioRolloffMode.Logarithmic, 5);
                }
            } finally {
                source.SetUserAccessing(false);
                target.SetUserAccessing(false);
                log.Debug("[U] removed access lock from source and target");
            }
        }

        private static IEnumerator ShowTemporaryText(float seconds, TileEntity entity, string text) {
            if (entity.GetTileEntityType() == TileEntityType.SecureLootSigned) {
                var container = entity as TileEntitySecureLootContainerSigned;
                var originalText = container.GetText();
                container.SetText(text);
                yield return new WaitForSeconds(seconds);
                container.SetText(originalText);
            }
        }

        private static IEnumerator DelayUpdateTextures(float seconds, Vector3i pos, int[] originalTextures) {
            //log.Debug($"{BlockFace.Top}: {originalTextures[(uint)BlockFace.Top]}, {BlockFace.Bottom}: {originalTextures[(uint)BlockFace.Bottom]}, {BlockFace.North}: {originalTextures[(uint)BlockFace.North]}, {BlockFace.West}: {originalTextures[(uint)BlockFace.West]}, {BlockFace.South}: {originalTextures[(uint)BlockFace.South]}, {BlockFace.East}: {originalTextures[(uint)BlockFace.East]}");
            yield return new WaitForSeconds(seconds);
            foreach (BlockFace _side in blockFaces) {
                GameManager.Instance.SetBlockTextureServer(pos, _side, originalTextures[(uint)_side], -1);
            }
        }

        internal static bool CanAccess(TileEntity source, TileEntity target) {
            var sourceIsContainer = ToContainer(source, out var sourceContainer);
            if (!sourceIsContainer) {
                log.Debug("Denied: source is not a container... wat?");
                return false;
            }
            if (sourceContainer.IsUserAccessing()) {
                log.Debug("Denied: source container is currently being used by another player");
                return false;
            }

            var targetIsContainer = ToContainer(target, out var targetContainer);
            if (!targetIsContainer) {
                log.Debug("Denied: target is not a container");
                return false;
            }
            if (targetContainer.bPlayerBackpack) {
                log.Debug("Denied: target is player backpack");
                return false;
            }
            if (!targetContainer.bPlayerStorage) {
                log.Debug("Denied: target is not player storage");
                return false;
            }
            if (targetContainer.IsUserAccessing()) {
                log.Debug("Denied: target container is currently being used by another player");
                ThreadManager.StartCoroutine(ShowTemporaryText(2, target, "Can't Distribute: Currently In Use"));
                return false;
            }

            var sourceIsLockable = ToLock(source, out var sourceLock);
            var targetIsLockable = ToLock(target, out var targetLock);

            if (!targetIsLockable) {
                log.Debug("Allowed: target is not lockable");
                return true;
            }

            if (!targetLock.IsLocked()) {
                log.Debug("Allowed: target is not locked");
                return true;
            }

            // so target is both lockable and currently locked...

            if (!targetLock.HasPassword()) {
                log.Debug("Denied: target is locked but has no password set");
                ThreadManager.StartCoroutine(ShowTemporaryText(2, target, "Can't Distribute: Locked and has no password"));
                return false;
            }

            if (!sourceIsLockable || !sourceLock.IsLocked()) {
                log.Debug("Denied: source does not have a lock but target does and is locked");
                ThreadManager.StartCoroutine(ShowTemporaryText(2, target, "Can't Distribute: Locked but Inbox isn't"));
                return false;
            }

            if (sourceLock.GetPassword().Equals(targetLock.GetPassword())) {
                log.Debug("Allowed: source and target are each locked but also shared the same password");
                return true;
            }

            log.Debug("Denied: source and target are locked with different passwords");
            ThreadManager.StartCoroutine(ShowTemporaryText(2, target, "Can't Distribute: Passwords Don't match"));
            return false;
        }

        internal static bool ToContainer(TileEntity entity, out TileEntityLootContainer typed) {
            if (entity != null
                && (entity.GetTileEntityType() == TileEntityType.Loot
                || entity.GetTileEntityType() == TileEntityType.SecureLoot
                || entity.GetTileEntityType() == TileEntityType.SecureLootSigned)) {
                typed = entity as TileEntityLootContainer;
                return true;
            }
            typed = null;
            return false;
        }

        internal static bool ToLock(TileEntity entity, out ILockable typed) {
            if (entity.GetTileEntityType() == TileEntityType.SecureLoot
                || entity.GetTileEntityType() == TileEntityType.SecureLootSigned) {
                typed = entity as ILockable;
                return true;
            }
            typed = null;
            return false;
        }
    }
}
