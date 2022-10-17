using System.Linq;
using System.Net.Http.Headers;

namespace StorageNetwork {
    internal class StorageManager {

        internal static void TestAccess() {
            var secured = new TileEntitySecureLootContainer(new Chunk());
            var insecured = new TileEntityLootContainer(new Chunk());

            TileEntity entity1 = GameManager.Instance.World.GetTileEntity(0, Vector3i.zero);
            TileEntity entity2 = GameManager.Instance.World.GetTileEntity(0, Vector3i.zero);

            /*
            if (typeof(TileEntitySecureLootContainer).IsInstanceOfType(entity1) && typeof(TileEntitySecureLootContainer).IsInstanceOfType(entity2)) {
                CanAccess(())
            }
            */

            //CanAccess(entity1, entity2);


            CanAccess(secured, insecured);
        }

        internal static void Distribute(TileEntity sourceTileEntity, TileEntity targetTileEntity) {
            if (!CanAccess(sourceTileEntity, targetTileEntity)) { return; }

            var source = sourceTileEntity as TileEntityLootContainer;
            var target = targetTileEntity as TileEntityLootContainer;
            
            foreach (var sourceItem in source.items) {
                foreach (var targetItem in target.items) {
                    if (sourceItem.itemValue == targetItem.itemValue) {
                        if (target.AddItem(sourceItem)) {
                            source.RemoveItem(sourceItem.itemValue);
                        }

                        //source.SetModified();
                        //target.SetModified();
                    }
                }
            }
        }

        internal static bool CanAccess(TileEntity source, TileEntity target) {
            var sourceIsInsecure = ToInsecureContainer(source, out _);
            var targetIsInsecure = ToInsecureContainer(target, out _);
            var sourceIsSecure = ToSecureContainer(source, out var secureSource);
            var targetIsSecure = ToSecureContainer(target, out var secureTarget);

            if (!sourceIsInsecure && !sourceIsSecure) { return false; }
            if (!targetIsInsecure && !targetIsSecure) { return false; }

            if (sourceIsInsecure && targetIsInsecure) { return true; }
            if (sourceIsSecure && targetIsInsecure) { return true; }
            if (sourceIsInsecure && targetIsSecure) { return !secureTarget.IsLocked(); }
            if (sourceIsSecure && targetIsSecure) {

                // unlocked targets are always accessible
                if (!secureTarget.IsLocked()) {
                    return true;
                }

                // source cannot use lock passthrough on target if source is not locked
                if (!secureSource.IsLocked()) {
                    return false;
                }

                // target without a password is secured and cannot be accessed by source
                if (!secureTarget.HasPassword()) {
                    return false;
                }

                // lock passthrough is possible so long as source and target share the same password
                if (secureSource.HasPassword() && secureTarget.GetPassword().Equals(secureSource.GetPassword())) {
                    return true;
                }

                // otherwise, access is not supported
                return false;
            }
            
            return false;
        }


        internal static bool ToSecureContainer(TileEntity entity, out TileEntitySecureLootContainer typed) {
            if (typeof(TileEntitySecureLootContainer).IsInstanceOfType(entity)) {
                typed = entity as TileEntitySecureLootContainer;
                return true;
            }
            typed = null;
            return false;
        }

        internal static bool ToInsecureContainer(TileEntity entity, out TileEntityLootContainer typed) {
            if (typeof(TileEntityLootContainer).IsInstanceOfType(entity)) {
                typed = entity as TileEntityLootContainer;
                return true;
            }
            typed = null;
            return false;
        }
    }
}
