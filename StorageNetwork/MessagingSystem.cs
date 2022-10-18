using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork {

    // TODO: clean this up
    internal class MessagingSystem {

        /**
         * <summary>Send a private message to a specific player.</summary>
         * <param name="message">The message to send.</param>
         * <param name="recipients">The player entityId(s) this message is addressed to.</param>
         */
        public static void Whisper(string message, params int[] recipients) {
            Send(EChatType.Whisper, message, recipients.ToList());
        }

        /**
         * <summary>Send a private message to a specific player.</summary>
         * <param name="message">The message to send.</param>
         * <param name="recipients">The player entityId(s) this message is addressed to.</param>
         */
        public static void Whisper(string message, List<int> recipients) {
            Send(EChatType.Whisper, message, recipients);
        }

        /**
         * <summary>Send a message to all players.</summary>
         * <param name="message">The message to send.</param>
         */
        public static void Broadcast(string message) {
            Send(EChatType.Global, message, GameManager.Instance.World.Players.list.Select(p => p.entityId).ToList());
        }

        /**
         * <summary>Send a message to all players who match the given condition.</summary>
         * <param name="message">The message to send.</param>
         * <param name="condition">The condition determining whether the player will receive the given message.</param>
         */
        public static void Broadcast(string message, Func<EntityPlayer, bool> condition) {
            Send(EChatType.Global, message, GameManager.Instance.World.Players.list
                .Where(condition)
                .Select(p => p.entityId)
                .ToList());
        }

        private static void Send(EChatType chatType, string message, List<int> recipients) {
            GameManager.Instance.ChatMessageServer(
                _cInfo: null,
                _chatType: chatType,
                _senderEntityId: -1,
                _msg: message,
                _mainName: "",
                _localizeMain: false,
                _recipientEntityIds: recipients);
        }
    }
}
