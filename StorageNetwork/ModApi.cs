using HarmonyLib;
using System.Reflection;

namespace StorageNetwork {
    public class ModApi : IModApi {
        //private static readonly ModLog log = new ModLog(typeof(ModApi));

        public void InitMod(Mod _modInstance) {
            Harmony harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            /* TODO
             * Determine if storage OnClose triggers a Net Package
             *  GameManager.TEUnlockServer(int, Vector3i, int) : void @06005F3E
             * Discover how to trigger a sort
             * Start by testing just the storage block below the Inbox
             * 
             * TileEntity, TileEntityForge, and TileEntityWorkstation each have a networked "SetModified" method one can call from server on item change (maybe?)
             * 
             * 
             * On Inbox Closed: Possibly trigger just before GameManager.TEUnlockServer fires
             * 
             * Open Other Chests: To check if destination boxes are currently in use or are inaccessible, see GameManager.TELockServer for an example of safely opening/locking a chest
             */

        }
    }
}
