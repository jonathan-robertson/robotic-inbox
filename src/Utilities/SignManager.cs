using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace RoboticInbox.Utilities
{
    internal class SignManager
    {
        private static readonly ModLog<SignManager> _log = new ModLog<SignManager>();
        private static Coroutine _signManagerCoroutine;

        private static ConcurrentDictionary<Vector3i, DateTime> MessageExpirations { get; } = new ConcurrentDictionary<Vector3i, DateTime>();
        private static ConcurrentDictionary<Vector3i, string> OriginalMessages { get; } = new ConcurrentDictionary<Vector3i, string>();

        internal static void OnGameStartDone()
        {
            _signManagerCoroutine = ThreadManager.StartCoroutine(MonitorContainerSigns());
        }

        internal static void OnGameManagerApplicationQuit()
        {
            if (_signManagerCoroutine != null)
            {
                ThreadManager.StopCoroutine(_signManagerCoroutine);
            }
            RestoreAllMessagesBeforeShutdown(GameManager.Instance.World);
        }

        internal static void HandleTargetLockedWithoutPassword(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked without password");
            }
            NotificationManager.PlaySoundVehicleStorageOpen(pos);
        }

        internal static void HandleTargetLockedWhileSourceIsNot(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Container Locked but Inbox is not");
            }
            NotificationManager.PlaySoundVehicleStorageOpen(pos);
        }

        internal static void HandlePasswordMismatch(Vector3i pos, TileEntity target)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                ShowTemporaryText(pos, SettingsManager.DistributionBlockedNoticeTime, signable, owner, "Can't Distribute: Password Does not match Inbox");
            }
            NotificationManager.PlaySoundVehicleStorageOpen(pos);
        }

        internal static void HandleTransferred(Vector3i pos, TileEntity target, int totalItemsTransferred)
        {
            if (TryCastToITileEntitySignable(target, out var signable) && TryGetOwner(target, out var owner))
            {
                ShowTemporaryText(pos, SettingsManager.DistributionSuccessNoticeTime, signable, owner, $"Added + Sorted\n{totalItemsTransferred} Item{(totalItemsTransferred > 1 ? "s" : "")}");
            }
            NotificationManager.PlaySoundVehicleStorageClose(pos);
        }

        private static IEnumerator MonitorContainerSigns()
        {
            var world = GameManager.Instance.World;
            while (true)
            {
                RestoreExpiredMessages(world);
                yield return new WaitForSeconds(0.1f);
            }
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
                    return signable != null;
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

        private static void ShowTemporaryText(Vector3i pos, float seconds, ITileEntitySignable signableEntity, PlatformUserIdentifierAbs signingPlayer, string text)
        {
            if (signingPlayer == null)
            {
                _log.Error($"[{pos}] no signing player found on target container; cannot update with info text");
                return;
            }


            if (OriginalMessages.TryAdd(pos, signableEntity.GetAuthoredText().Text))
            {
                _log.Trace($"[{pos}] added message");
            }
            else
            {
                _log.Trace($"[{pos}] extended with updated message");
            }
            _ = MessageExpirations.TryRemove(pos, out _);
            _ = MessageExpirations.TryAdd(pos, DateTime.Now.AddSeconds(seconds));

            signableEntity.SetText(text, true, signingPlayer); // update with new text (and sync to players)
        }

        private static void RestoreExpiredMessages(World world)
        {
            var now = DateTime.Now;
            foreach (var pos in MessageExpirations.Keys)
            {
                if (MessageExpirations.TryGetValue(pos, out var expiration)
                    && expiration < now)
                {
                    if (!MessageExpirations.TryRemove(pos, out _))
                    {
                        _log.Trace($"[{pos}] failed to remove expiration date record, but continuing on; this is unexpected but would suggest the value has already been removed.");
                    }
                    if (TryRestoreOriginalMessage(world, pos, true))
                    {
                        _log.Trace($"[{pos}] restored original message to container.");
                    }
                    else
                    {
                        _log.Warn($"[{pos}] failed to restore original message to container.");
                    }
                }
            }
        }

        private static void RestoreAllMessagesBeforeShutdown(World world)
        {
            MessageExpirations.Clear(); // immediately clear all expiration timestamps
            foreach (var pos in OriginalMessages.Keys)
            {
                if (TryRestoreOriginalMessage(world, pos, false))
                {
                    _log.Info($"Successfully restored original message to container at {pos} during shutdown.");
                }
                else
                {
                    _log.Info($"Failed to restore original message to container at {pos} during shutdown.");
                }
            }
        }


        private static bool TryRestoreOriginalMessage(World world, Vector3i pos, bool broadcastToPlayers)
        {
            if (!OriginalMessages.TryRemove(pos, out var originalText))
            {
                return false;
            }
            var tileEntity = world.GetTileEntity(pos);
            if (tileEntity == null)
            {
                return false;
            }
            if (!TryGetOwner(tileEntity, out var signingPlayer))
            {
                return false;
            }
            if (!TryCastToITileEntitySignable(tileEntity, out var signable))
            {
                return false;
            }
            signable.SetText(originalText, broadcastToPlayers, signingPlayer);
            return true;
        }
    }
}
