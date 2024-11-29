using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    public class Utils : CustomInteractionModuleBase<UtilsModuleConfig>
    {
        [SlashCommand("getavatar", "Gets the avatar of a user.")]
        public async Task GetAvatar(SocketGuildUser user = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor(Context.User);
            eb.WithColor(Color.Blue);
            eb.WithTitle("Profile Image");
            SocketUser target = user == null ? Context.User : user;
            bool result = Context.Guild.GetUser(user.Id).GetGuildAvatarUrl(size: 512) != "";
            eb.AddField("Is guild profile?", result);
            if(!result)
            {
                eb.WithImageUrl(Context.Guild.GetUser(user.Id).GetGuildAvatarUrl(size: 512));
            }
            else
            {
                eb.WithImageUrl(user.GetDefaultAvatarUrl());
            }
            eb.WithCurrentTimestamp();
            await RespondAsync(embed: eb.Build());
            return;
        }
    }

    public class UtilsModuleConfig : ModuleConfiguration
    {
        public override string FileName => "Utils";
    }
}
