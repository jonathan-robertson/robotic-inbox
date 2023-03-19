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
                { "debug", "toggle debug logging mode" },
            };

            var i = 1; var j = 1;
            help = $"Usage:\n  {string.Join("\n  ", dict.Keys.Select(command => $"{i++}. {GetCommands()[0]} {command}").ToList())}\nDescription Overview\n{string.Join("\n", dict.Values.Select(description => $"{j++}. {description}").ToList())}";
        }

        public override string[] GetCommands()
        {
            return Commands;
        }

        public override string GetDescription()
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
                    switch (_params[0])
                    {
                        case "debug":
                            ModApi.DebugMode = !ModApi.DebugMode;
                            SdtdConsole.Instance.Output($"Debug Mode has successfully been {(ModApi.DebugMode ? "enabled" : "disabled")}.");
                            return;
                    }
                    return;
                }
                SdtdConsole.Instance.Output($"Invald parameter provided; use 'help {Commands[0]}' to learn more.");
            }
            catch (Exception e)
            {
                SdtdConsole.Instance.Output($"Exception encountered: \"{e.Message}\"\n{e.StackTrace}");
            }
        }
    }
}
