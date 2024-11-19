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

        public string LogPath { get; set; } = "./Logs/";

        public bool EnableLogging { get; set; } = true;

        public string AccountID { get; set; } = "";

        public string APIUrl { get; set; } = "";

        public int RefreshMaxCooldown { get; set; } = 60;

        public int RefreshMinCooldown { get; set; } = 20;

        public int RefreshCooldown { get; set; } = 30;
    }
}
