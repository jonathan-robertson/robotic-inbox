using UnityEngine;

namespace RoboticInbox.Utilities
{
    internal class NotificationManager
    {
        private static string SoundVehicleStorageOpen { get; } = "vehicle_storage_open";
        private static string SoundVehicleStorageClose { get; } = "vehicle_storage_close";
        private static string MessageTargetContainerInUse { get; } = "Robotic Inbox was [ff8000]unable to organize this container[-] as it was in use.";

        public static void PlaySoundVehicleStorageOpen(Vector3 pos)
        {
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageOpen, AudioRolloffMode.Logarithmic, 5);
        }

        public static void PlaySoundVehicleStorageClose(Vector3 pos)
        {
            GameManager.Instance.PlaySoundAtPositionServer(pos, SoundVehicleStorageClose, AudioRolloffMode.Logarithmic, 5);
        }

        internal static void NotifyInUse(int entityIdInTargetContainer, Vector3 targetPos)
        {
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
        }
    }
}
