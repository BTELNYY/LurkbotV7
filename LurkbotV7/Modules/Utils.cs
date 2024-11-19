using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    public class Utils : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("getavatar", "Gets the avatar of a user.")]
        public Task GetAvatar(SocketGuildUser user = null)
        {
            return Task.CompletedTask;
        }
    }
}
