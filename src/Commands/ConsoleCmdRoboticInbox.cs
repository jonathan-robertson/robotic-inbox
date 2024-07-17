using RoboticInbox.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboticInbox
{
    internal class ConsoleCmdRoboticInbox : ConsoleCmdAbstract
    {
        private static readonly string[] Commands = new string[] {
            "roboticinbox",
            "ri"
        };
        private readonly string help;

        public ConsoleCmdRoboticInbox()
        {
            var dict = new Dictionary<string, string>() {
                { "settings", "list current settings alongside default/recommended values" },
                { "horizontal-range <int>", "set how wide (x/z axes) the inbox should scan for storage containers" },
                { "vertical-range <int>", "set how high/low (y axis) the inbox should scan for storage containers" },
                { "success-notice-time <float>", "set how long to leave distribution success notice on boxes" },
                { "blocked-notice-time <float>", "set how long to leave distribution blocked notice on boxes" },
                { "base-siphoning-protection <bool>", "whether inboxes within claimed land should prevent scanning outside the bounds of their lcb" },
                //{ "base-fishing-protection", "whether inboxes outside claimed land should be blocked from scanning containers within claimed land (or other land claims)" },
                { "dm", "toggle debug logging mode" },
            };
            var i = 1; var j = 1;
            help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        public override string[] getCommands()
        {
            return Commands;
        }

        public override string getDescription()
        {
            return "Configure or adjust settings for the RoboticInbox mod.";
        }

        public override string GetHelp()
        {
            return help;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                if (_params.Count > 0)
                {
                    switch (_params[0].ToLower())
                    {
                        case "settings":
                            SdtdConsole.Instance.Output(SettingsManager.AsString());
                            return;
                        case "horizontal-range":
                            if (_params.Count == 1 || !int.TryParse(_params[1], out var hRange)) { break; }
                            try
                            {
                                var prev = SettingsManager.InboxHorizontalRange;
                                var updated = SettingsManager.SetInboxHorizontalRange(hRange);
                                SdtdConsole.Instance.Output($"horizontal-range updated from {prev} to {updated} and settings saved successfully to {SettingsManager.Filename}");
                            }
                            catch (Exception e)
                            {
                                SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                            }
                            return;
                        case "vertical-range":
                            if (_params.Count == 1 || !int.TryParse(_params[1], out var vRange)) { break; }
                            try
                            {
                                var prev = SettingsManager.InboxVerticalRange;
                                var updated = SettingsManager.SetInboxVerticalRange(vRange);
                                SdtdConsole.Instance.Output($"vertical-range updated from {prev} to {updated} and settings saved successfully to {SettingsManager.Filename}");
                            }
                            catch (Exception e)
                            {
                                SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                            }
                            return;
                        case "success-notice-time":
                            if (_params.Count == 1 || !float.TryParse(_params[1], out var successDelay)) { break; }
                            try
                            {
                                var prev = SettingsManager.DistributionSuccessNoticeTime;
                                var updated = SettingsManager.SetDistributionSuccessNoticeTime(successDelay);
                                SdtdConsole.Instance.Output($"success-notice-time updated from {prev:0.0} to {updated:0.0} and settings saved successfully to {SettingsManager.Filename}");
                            }
                            catch (Exception e)
                            {
                                SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                            }
                            return;
                        case "blocked-notice-time":
                            if (_params.Count == 1 || !float.TryParse(_params[1], out var blockedDelay)) { break; }
                            try
                            {
                                var prev = SettingsManager.DistributionBlockedNoticeTime;
                                var updated = SettingsManager.SetDistributionBlockedNoticeTime(blockedDelay);
                                SdtdConsole.Instance.Output($"blocked-notice-time updated from {prev:0.0} to {updated:0.0} and settings saved successfully to {SettingsManager.Filename}");
                            }
                            catch (Exception e)
                            {
                                SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                            }
                            return;
                        case "base-siphoning-protection":
                            try
                            {
                                var prev = SettingsManager.BaseSiphoningProtection;
                                var updated = SettingsManager.SetBaseSiphoningProtection(!prev);
                                SdtdConsole.Instance.Output($"base-siphoning-protection updated from {prev} to {updated} and settings saved successfully to {SettingsManager.Filename}");
                            }
                            catch (Exception e)
                            {
                                SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                            }
                            return;
                        //case "base-fishing-protection": // TODO: implement
                        //    try
                        //    {
                        //        var prev = SettingsManager.BaseFishingProtection;
                        //        var updated = SettingsManager.SetBaseFishingProtection(!prev);
                        //        SdtdConsole.Instance.Output($"base-fishing-protection updated from {prev} to {updated} and settings saved successfully to {SettingsManager.Filename}");
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        SdtdConsole.Instance.Output($"settings could not be saved to {SettingsManager.Filename} due to encountering an issue: {e.Message}");
                        //    }
                        //    return;
                        case "debug":
                        case "dm":
                            ModApi.DebugMode = !ModApi.DebugMode;
                            SdtdConsole.Instance.Output($"debug logging mode has been {(ModApi.DebugMode ? "enabled" : "disabled")} for Robotic Inbox.");
                            return;
                    }
                }
                SdtdConsole.Instance.Output($"Invald parameters provided; use 'help {Commands[0]}' to learn more.");
            }
            catch (Exception e)
            {
                SdtdConsole.Instance.Output($"Exception encountered: \"{e.Message}\"\n{e.StackTrace}");
            }
        }
    }
}
