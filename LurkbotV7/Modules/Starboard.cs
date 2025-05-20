using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;
using System.Collections.ObjectModel;

namespace LurkbotV7.Modules
{
    [Module]
    public class Starboard : CustomInteractionModuleBase<StarboardConfiguration>
    {
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            Program.Client.MessageReceived += Client_MessageReceived;
            Program.Client.ReactionAdded += Client_ReactionAdded;
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == Program.Client.CurrentUser.Id)
            {
                return;
            }
            ReadOnlyCollection<StarboardConfiguration.StarboardChannelConfiguration> autoReacts = new(Config.StarboardChannels.Where(x => x.ChannelID == message.Channel.Id).ToList());
            if (!autoReacts.Any())
            {
                return;
            }
            foreach (StarboardConfiguration.StarboardChannelConfiguration react in autoReacts)
            {
                Log.Debug("Reacting with: " + react.ReactionEmoji);
                await message.AddReactionAsync(Emoji.Parse(react.ReactionEmoji));
            }
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, Discord.WebSocket.SocketReaction reaction)
        {
            IUserMessage msg = await message.GetOrDownloadAsync();
            IMessageChannel chnl = await channel.GetOrDownloadAsync();
            SocketGuildChannel guildChannel = chnl as SocketGuildChannel;
            if (chnl.ChannelType != ChannelType.Text)
            {
                return;
            }
            StarboardConfiguration.StarboardChannelConfiguration channelConfig = Config.StarboardChannels.Find(x => x.ChannelID == chnl.Id && x.ReactionEmoji == reaction.Emote.Name);
            if (channelConfig == default(StarboardConfiguration.StarboardChannelConfiguration))
            {
                return;
            }
            if (channelConfig.PostTopChannelID != 0 && msg.Reactions.Where(x => x.Key.Name == reaction.Emote.Name).Count() >= channelConfig.StarsNeededForForwarding)
            {
                if (msg is SocketMessage sockMsg)
                {
                    string forwardMessage = $"\"{sockMsg.Content}\" \n - {sockMsg.Author.Mention}";
                    foreach (Attachment attachment in sockMsg.Attachments)
                    {
                        forwardMessage += attachment.ProxyUrl + "\n";
                    }
                    IChannel forwardChannel = guildChannel.Guild.GetChannel(channelConfig.PostTopChannelID);
                    if (forwardChannel == null)
                    {
                        Log.Warning($"Can't find channel {channelConfig.PostTopChannelID} in guild \"{guildChannel.Guild.Name}\"");
                        return;
                    }
                    if (forwardChannel is not ITextChannel textChannel)
                    {
                        Log.Warning($"Channel \"{forwardChannel.Name}\" is not a text channel.");
                        return;
                    }
                    await textChannel.SendMessageAsync(forwardMessage, allowedMentions: AllowedMentions.None);
                    return;
                }
            }
            return;
        }

        [SlashCommand("createstarboard", "creates a starboard in the current channel.", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task CreateStarBoard(string emoji, IChannel forwardChannel = null, int starsNeededToForward = int.MaxValue)
        {
            if (!Emoji.TryParse(emoji, out Emoji result))
            {
                await RespondWithErrorAsync($"Emoji \"{emoji}\" cannot be parsed.", ephemeral: true);
                return;
            }
            if (Config.StarboardChannels.Where(x => x.ReactionEmoji == emoji && x.ChannelID == Context.Channel.Id).Count() > 0)
            {
                await RespondWithErrorAsync("Duplicate starboard for channel and emoji. Choose a different channel or emoji.", ephemeral: true);
                return;
            }
            if (forwardChannel != null)
            {
                if (forwardChannel.ChannelType != ChannelType.Text)
                {
                    await RespondWithErrorAsync("Forwarding channel must be a text channel.", ephemeral: true);
                    return;
                }
            }
            if (starsNeededToForward < 0)
            {
                await RespondWithErrorAsync("Stars needed to forward should be greater than 0. Leave argument out of command to disable.", ephemeral: true);
                return;
            }
            StarboardConfiguration.StarboardChannelConfiguration channelConfig = new()
            {
                ChannelID = Context.Channel.Id,
                StarsNeededForForwarding = starsNeededToForward,
                ReactionEmoji = emoji,
                PostTopChannelID = forwardChannel == null ? 0 : forwardChannel.Id
            };
            await RespondWithSuccesAsync("Added starboard.", ephemeral: true);
            Config.StarboardChannels.Add(channelConfig);
            SaveConfig(Config);
        }


        [SlashCommand("removestarboard", "Removes a starboard from the current channel.", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task RemoveStarBoard(string emoji)
        {
            if (!Emoji.TryParse(emoji, out Emoji parsedEmoji))
            {
                await RespondWithErrorAsync($"Emoji \"{emoji}\" cannot be parsed.", ephemeral: true);
                return;
            }
            StarboardConfiguration.StarboardChannelConfiguration channelConfig = Config.StarboardChannels.Find(x => x.ChannelID == Context.Channel.Id && x.ReactionEmoji == emoji);
            if (channelConfig == default(StarboardConfiguration.StarboardChannelConfiguration))
            {
                await RespondWithErrorAsync($"No starboard exists with emoji \"{emoji}\" in this channel.", ephemeral: true);
                return;
            }
            await RespondWithSuccesAsync("Removed starboard.", ephemeral: true);
            Config.StarboardChannels.Remove(channelConfig);
            SaveConfig(Config);
        }
    }

    public class StarboardConfiguration : ModuleConfiguration
    {
        public override string FileName => "Starboard";

        public class StarboardChannelConfiguration
        {
            public ulong ChannelID { get; set; } = 0;

            public ulong PostTopChannelID { get; set; } = 0;

            public int StarsNeededForForwarding { get; set; } = int.MaxValue;

            public string ReactionEmoji { get; set; } = ":star:";
        }

        public List<StarboardChannelConfiguration> StarboardChannels { get; set; } = new List<StarboardChannelConfiguration>();
    }
}
