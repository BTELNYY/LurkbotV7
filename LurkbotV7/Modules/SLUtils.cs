using Discord;
using Discord.Interactions;
using LurkbotV7.Attributes;
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
    [Module]
    public class SLUtils : CustomInteractionModuleBase<SLUtilsConfiguration>
    {
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            SLManager.ListRefreshed += (response) =>
            {
                _ = Task.Run(async () => 
                {
                    string chosenMessage = "";
                    if (!response.Success)
                    {
                        chosenMessage = "Ping btelnyy because something is broken (bad)";
                    }
                    else if(!response.Servers.Any(x => x.PlayersList.Count() > 0))
                    {
                        chosenMessage = "All servers are empty.";
                    }
                    else
                    {
                        Server chosenServer = new Server();
                        int players = 0;
                        foreach (var item in response.Servers)
                        {
                            if (players < item.PlayersList.Count())
                            {
                                chosenServer = item;
                                players = item.PlayersList.Count();
                            }
                        }
                        Program.Config.IDToName.TryGetValue(chosenServer.ID, out string serverName);
                        serverName = serverName == string.Empty ? "(No Name)" : serverName;
                        chosenMessage = $"{players}/30 playing on {serverName}!";
                    }
                    Game game = new CustomStatusGame(chosenMessage);
                    await Program.Client.SetActivityAsync(game);
                });
            };
        }

        [SlashCommand("getplayers", "Gets online players")]
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
