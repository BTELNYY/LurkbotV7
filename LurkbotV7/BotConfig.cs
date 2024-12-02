using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7
{
    public class BotConfig
    {
        public string BotToken { get; set; } = "";

        public bool ShowLogsInConsole { get; set; } = true;

        public string LogPath { get; set; } = "./logs/";

        public bool EnableLogging { get; set; } = true;

        public int AccountID { get; set; } = 0;

        public string APIKey { get; set; } = "";

        public string APIUrl { get; set; } = "https://api.scpslgame.com/serverinfo.php?id={id}&key={key}&list=true&nicknames=true&online=true";

        public Dictionary<int, string> IDToName {  get; set; } = new();

        public int RefreshMaxCooldown { get; set; } = 60;

        public int RefreshMinCooldown { get; set; } = 20;

        public int RefreshCooldown { get; set; } = 30;

        public List<ChannelTarget> UpdateChannelTargets { get; set; } = new List<ChannelTarget>() 
        {
            new ChannelTarget()
            {
                ServerID = 653788451380002817,
                ChannelID = 977450334047834113,
            },
            new ChannelTarget()
            {
                ServerID = 951311770843230238,
                ChannelID = 1182193099376697436
            }
        };
    }

    public class ChannelTarget
    {
        public ulong ServerID { get; set; } = 0;

        public ulong ChannelID { get; set; } = 0;
    }
}
