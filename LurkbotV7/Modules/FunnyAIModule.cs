using Discord.Interactions;
using LurkbotV7.Attributes;
using Discord;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace LurkbotV7.Modules
{
    [Module]
    public class FunnyAIModule : CustomInteractionModuleBase<FunnyAIModuleConfig>
    {
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
        }

        static bool _aiState = false;

        [SlashCommand("aisetstate", "Sets the AI state", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetAIState(bool state)
        {
            if(_aiState == state)
            {
                await RespondAsync($"No change, state is already `{state}`", ephemeral: true);
                return;
            }
            _aiState = state;
            await RespondWithSuccesAsync("Set AI State to: " + state, ephemeral: true);
        }

        [SlashCommand("aisetblacklist", "Set the blacklist of a user", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task BlacklistUser(SocketUser user, bool state = true)
        {
            if(Config.UserBlacklist.Contains(user.Id) && state)
            {
                await RespondWithErrorAsync("That user is already blacklisted.", ephemeral: true);
            }
            else if(Config.UserBlacklist.Contains(user.Id) && !state)
            {
                Config.UserBlacklist.Remove(user.Id);
                SaveConfig();
                await RespondWithSuccesAsync("User has been removed from the blacklist.", ephemeral: true);
            }
            else if(!Config.UserBlacklist.Contains(user.Id) && state)
            {
                Config.UserBlacklist.Add(user.Id);
                SaveConfig();
                await RespondWithSuccesAsync("User has been added to the blacklist.", ephemeral: true);
            }
            else
            {
                await RespondWithErrorAsync("That user is not blacklisted.", ephemeral: true);
            }
        }
    }

    public class FunnyAIModuleConfig : ModuleConfiguration
    {
        public override string FileName => "AIConfig";

        public string Url { get; set; } = "";

        public string Memory { get; set; } = "";

        public int MaxGenerationSize { get; set; } = 128;

        public int MaxContextSize { get; set; } = 2048;

        public float Temperature { get; set; } = 0.7f;

        public List<ulong> UserBlacklist { get; set; } = new List<ulong>();
    }
}
