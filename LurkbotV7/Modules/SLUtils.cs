using Discord.Interactions;
using LurkbotV7.Config;
using LurkbotV7.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    public class SLUtils : CustomInteractionModuleBase<SLUtilsConfiguration>
    {
        [SlashCommand("GetPlayers", "Gets online players")]
        [RequireContext(ContextType.Guild)]
        public async Task GetPlayers()
        {
            await RespondAsync(embeds: SLManager.GetEmbeds());
        }
    }

    public class SLUtilsConfiguration : ModuleConfiguration
    {
        public override string FileName => "SLUtilsConfig";
    }
}
