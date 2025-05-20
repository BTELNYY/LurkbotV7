using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;

namespace LurkbotV7.Modules
{
    [Module]
    public class ChannelUtility : CustomInteractionModuleBase<ChannelUtilityConfig>
    {
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            Program.Client.MessageReceived += Client_MessageReceived;
        }


        [RequireContext(ContextType.Guild)]
        [SlashCommand("addautoreaction", "Adds an auto reaction for a channel.", runMode: RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async void AddAutoReaction(SocketChannel channel, string reaction)
        {
            if (channel is not SocketTextChannel textChannel)
            {
                await RespondWithErrorAsync("Channel must be a text channel.", ephemeral: true);
                return;
            }
            if (!GuildEmote.TryParse(reaction, out Emote guildEmote))
            {
                await RespondWithErrorAsync($"Unable to find emote \"{reaction}\".");
                return;
            }
            if (!Config.ChannelToAutoReaction.ContainsKey(channel.Id))
            {
                Config.ChannelToAutoReaction.Add(channel.Id, new List<ulong> { guildEmote.Id });
            }
            else
            {
                if (Config.ChannelToAutoReaction[channel.Id].Contains(guildEmote.Id))
                {
                    await RespondWithErrorAsync("That emote is already set up to be reacted with for new messages.");
                    return;
                }
                Config.ChannelToAutoReaction[channel.Id].Add(guildEmote.Id);
                await RespondWithSuccesAsync("Done.");
            }
            SaveConfig();
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("removeautoreaction", "Adds an auto reaction for a channel.", runMode: RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async void RemoveAutoReaction(SocketChannel channel, string reaction)
        {
            if (channel is not SocketTextChannel textChannel)
            {
                await RespondWithErrorAsync("Channel must be a text channel.", ephemeral: true);
                return;
            }
            if (!GuildEmote.TryParse(reaction, out Emote guildEmote))
            {
                await RespondWithErrorAsync($"Unable to find emote \"{reaction}\".");
                return;
            }
            if (!Config.ChannelToAutoReaction.ContainsKey(channel.Id))
            {
                await RespondWithErrorAsync($"Channel does not have any auto reactions.");
                return;
            }
            else
            {
                if (!Config.ChannelToAutoReaction[channel.Id].Contains(guildEmote.Id))
                {
                    await RespondWithErrorAsync("That emote is not registered on that channel.");
                    return;
                }
                Config.ChannelToAutoReaction[channel.Id].Remove(guildEmote.Id);
                await RespondWithSuccesAsync("Done.");
            }
            SaveConfig();
        }

        private Task Client_MessageReceived(SocketMessage msg)
        {
            _ = Task.Run(async () =>
            {
                if (!Config.ChannelToAutoReaction.TryGetValue(msg.Channel.Id, out List<ulong> value))
                {
                    return;
                }
                if (value.Count == 0)
                {
                    return;
                }
                foreach (ulong emojiId in value)
                {
                    if (msg.Channel is SocketTextChannel guildTextChannel)
                    {
                        GuildEmote emote = await guildTextChannel.Guild.GetEmoteAsync(emojiId);
                        if (emote == null)
                        {
                            return;
                        }
                        await msg.AddReactionAsync(emote);
                    }
                }
            });
            return Task.CompletedTask;
        }
    }

    public class ChannelUtilityConfig : ModuleConfiguration
    {
        public override string FileName => "ChannelUtilsConfig";

        public Dictionary<ulong, List<ulong>> ChannelToAutoReaction = new();
    }
}
