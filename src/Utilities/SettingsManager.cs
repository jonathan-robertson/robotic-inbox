using RoboticInbox.Data;
using System;
using System.IO;

namespace RoboticInbox.Utilities
{
    internal class SettingsManager
    {
        public const int H_DIST_MIN = 0;
        public const int H_DIST_MAX = 128;
        public const int V_DIST_MIN = -1;
        public const int V_DIST_MAX = StorageManager.Y_MAX;
        public const float SUCCESS_NOTICE_TIME_MIN = 0.0f;
        public const float SUCCESS_NOTICE_TIME_MAX = 10.0f;
        public const float BLOCKED_NOTICE_TIME_MIN = 0.0f;
        public const float BLOCKED_NOTICE_TIME_MAX = 10.0f;

        private static readonly ModLog<SettingsManager> _log = new ModLog<SettingsManager>();
        private static ModSettings Settings = null;

        public static string Filename { get; private set; } = Path.Combine(GameIO.GetSaveGameDir(), "robotic-inbox.json");

        public static int InboxHorizontalRange => Settings.InboxHorizontalRange;
        public static int InboxVerticalRange => Settings.InboxVerticalRange;
        public static float DistributionSuccessNoticeTime => Settings.DistributionSuccessNoticeTime;
        public static float DistributionBlockedNoticeTime => Settings.DistributionBlockedNoticeTime;
        public static bool BaseSiphoningProtection => Settings.BaseSiphoningProtection;
        //public static bool BaseFishingProtection => Settings.BaseFishingProtection; // TODO: implement

        internal static string AsString()
        {
            // TODO; add under 
            //  - base-fishing-protection - lcb: { BaseFishingProtection}
            //    - [recommended: true]
            return $@"
=== Current Settings for Robotic Inbox
=== These settings are retained in a file on the host system: {Filename}
Distribution Scanning Range: these values have an impact on server/host performance; extremely high values may cause lag for all players
- horizontal-range: {InboxHorizontalRange} [recommended: 5; must be: >= {H_DIST_MIN} & <= {H_DIST_MAX}; impact: very high]
- vertical-range: {InboxVerticalRange} [recommended: 5; must be: >= {V_DIST_MIN} & <= {V_DIST_MAX}; -1 = bedrock-to-sky; impact: high]
Notices: keeping these as low as possible is best because a server restart while displaying a message forgets the original
- success-notice-time: {DistributionSuccessNoticeTime:0.0} [recommended: 2.0; must be >= {SUCCESS_NOTICE_TIME_MIN} & <= {SUCCESS_NOTICE_TIME_MAX}; disable with 0.0]
- blocked-notice-time: {DistributionBlockedNoticeTime:0.0} [recommended: 3.0; must be >= {BLOCKED_NOTICE_TIME_MIN} & <= {BLOCKED_NOTICE_TIME_MAX}; disable with 0.0]
Security: these options are meant to protect the players from fishing or siphoning, but might not be preferred by everyone
- base-siphoning-protection: {BaseSiphoningProtection} [recommended: True]
Temporary Values: server starts with this in a default state and does not save them to the settings file
- debug mode: {(ModApi.DebugMode ? "enabled" : "disabled")} [recommended: False - enabling this adds a lot of overhead and should only be running for debugging purposes]";
        }

        internal static void Load()
        {
            CreatePathIfMissing();
            try
            {
                var input = File.ReadAllText(Filename);
                if (input != null)
                {
                    Settings = Json<ModSettings>.Deserialize(input);
                    _log.Info($"Successfully loaded settings for Robotic Inbox mod; filename: {Filename}.");
                    _log.Info(AsString());
                }
                _log.Warn($"Unable to load settings for Robotic Inbox mod; falling back to default values; filename: {Filename}");
            }
            catch (FileNotFoundException)
            {
                _log.Info($"No file detected for Robotic Inbox mod; creating a config with defaults in {Filename}");
                Settings = new ModSettings();
                try
                {
                    Save();
                }
                catch (Exception)
                {
                    // swollow exception since we already logged it
                    // exception is thrown from Save on failure to make it easier for ConsoleCmdRoboticInbox to also display an error to admin
                }
            }
            catch (Exception e)
            {
                _log.Warn($"Unhandled exception encountered when attempting to load settings for Robotic Inbox mod; filename: {Filename}", e);
                throw e;
            }
        }

        internal static void Save()
        {
            try
            {
                if (!Directory.Exists(GameIO.GetSaveGameDir()))
                {
                    _ = Directory.CreateDirectory(GameIO.GetSaveGameDir());
                }
                File.WriteAllText(Filename, Json<ModSettings>.Serialize(Settings));
            }
            catch (Exception e)
            {
                _log.Error($"Unable to save Robotic Inbox mod settings to {Filename}.", e);
                throw e;
            }
        }

        internal static void CreatePathIfMissing()
        {
            _ = Directory.CreateDirectory(GameIO.GetSaveGameDir());
        }

        internal static int SetInboxHorizontalRange(int value)
        {
            Settings.InboxHorizontalRange = Clamp(value, H_DIST_MIN, H_DIST_MAX);
            Save();
            PropagateHorizontalRange();
            return Settings.InboxHorizontalRange;
        }

        internal static void PropagateHorizontalRange(EntityPlayer player)
        {
            player.SetCVar("roboticInboxRangeH", Settings.InboxHorizontalRange);
        }

        private static void PropagateHorizontalRange()
        {
            for (var i = 0; i < GameManager.Instance.World.GetLocalPlayers().Count; i++)
            {
                PropagateHorizontalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
            }
            for (var i = 0; i < GameManager.Instance.World.Players.list.Count; i++)
            {
                PropagateHorizontalRange(GameManager.Instance.World.Players.list[i]);
            }
        }

        internal static int SetInboxVerticalRange(int value)
        {
            Settings.InboxVerticalRange = Clamp(value, V_DIST_MIN, V_DIST_MAX);
            Save();
            PropagateVerticalRange();
            return Settings.InboxVerticalRange;
        }

        internal static void PropagateVerticalRange(EntityPlayer player)
        {
            player.SetCVar("roboticInboxRangeV", Settings.InboxVerticalRange);
        }

        private static void PropagateVerticalRange()
        {
            for (var i = 0; i < GameManager.Instance.World.GetLocalPlayers().Count; i++)
            {
                PropagateVerticalRange(GameManager.Instance.World.GetLocalPlayers()[i]);
            }
            for (var i = 0; i < GameManager.Instance.World.Players.list.Count; i++)
            {
                PropagateVerticalRange(GameManager.Instance.World.Players.list[i]);
            }
        }

        internal static bool SetBaseSiphoningProtection(bool value)
        {
            Settings.BaseSiphoningProtection = value;
            Save();
            return Settings.BaseSiphoningProtection;
        }

        //internal static bool SetBaseFishingProtection(bool value)
        //{
        //    Settings.BaseFishingProtection = value;
        //    Save();
        //    return Settings.BaseFishingProtection;
        //}

        internal static float SetDistributionSuccessNoticeTime(float value)
        {
            Settings.DistributionSuccessNoticeTime = Clamp(value, SUCCESS_NOTICE_TIME_MIN, SUCCESS_NOTICE_TIME_MAX);
            Save();
            return Settings.DistributionSuccessNoticeTime;
        }

        internal static float SetDistributionBlockedNoticeTime(float value)
        {
            Settings.DistributionBlockedNoticeTime = Clamp(value, BLOCKED_NOTICE_TIME_MIN, BLOCKED_NOTICE_TIME_MAX);
            Save();
            return Settings.DistributionBlockedNoticeTime;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
