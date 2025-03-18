using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;

namespace LurkbotV7.Modules
{
    [Module]
    public class Utils : CustomInteractionModuleBase<UtilsModuleConfig>
    {
        [SlashCommand("getavatar", "Gets the avatar of a user.")]
        [RequireContext(ContextType.Guild)]
        public async Task GetAvatar(SocketGuildUser user = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithAuthor(Context.User);
            eb.WithColor(Color.Blue);
            eb.WithTitle("Profile Image");
            SocketUser target = user == null ? Context.User : user;
            bool result = Context.Guild.GetUser(user.Id).GetGuildAvatarUrl(size: 512) == "";
            eb.AddField("Is guild profile?", result);
            if (result)
            {
                eb.WithImageUrl(Context.Guild.GetUser(user.Id).GetGuildAvatarUrl(size: 512));
            }
            else
            {
                eb.WithImageUrl(user.GetDisplayAvatarUrl(size: 512));
            }
            //eb.ImageUrl = eb.ImageUrl.Replace("?size=128", string.Empty);
            eb.WithCurrentTimestamp();
            await RespondAsync(embed: eb.Build());
            return;
        }

        [MessageCommand("Create Role Based On Reaction")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageRoles)]
        public async Task CreateRoleFromReaction(IMessage message)
        {
            ReactionRoleCreationModal modal = new ReactionRoleCreationModal();
            modal.MessageID = message.Id.ToString();
            await Context.Interaction.RespondWithModalAsync("reacted_role_created_modal", modal);
        }

        public class ReactionRoleCreationModal : IModal
        {
            public string Title => "Reaction Role Creation";

            [InputLabel("Message ID")]
            [ModalTextInput("messageid")]
            public string MessageID { get; set; }

            [InputLabel("Reaction Name")]
            [ModalTextInput("reaction")]
            public string Reaction { get; set; }
        }

        [ModalInteraction("reacted_role_created_modal")]
        public async Task ProccessReactedRoleModal(ReactionRoleCreationModal modal)
        {
            if (Context.Guild == null)
            {
                await RespondWithErrorAsync("Guild is null.", ephemeral: true);
                return;
            }
            if (Context.Channel == null)
            {
                await RespondWithErrorAsync("Channel is null.", ephemeral: true);
                return;
            }
            if (Context.Channel is not SocketTextChannel textChannel)
            {
                await RespondWithErrorAsync("Channel is null.", ephemeral: true);
                return;
            }
            if (!ulong.TryParse(modal.MessageID, out ulong messageID))
            {
                await RespondWithErrorAsync("Illegal message ID (Not a ulong)", ephemeral: true);
                return;
            }
            IMessage message = await textChannel.GetMessageAsync(messageID);
            if (message == null)
            {
                await RespondWithErrorAsync("Illegal message (Not Found)", ephemeral: true);
                return;
            }
            IEmote emote = Emoji.Parse(modal.Reaction);
            if (emote == null)
            {
                await RespondWithErrorAsync("Illegal or invalid emoji. Use Unicode or the :name: format.", ephemeral: true);
                return;
            }
            if (!message.Reactions.TryGetValue(emote, out ReactionMetadata value))
            {
                await RespondWithErrorAsync("Emoji Not Found on Message.", ephemeral: true);
                return;
            }
            IEnumerable<IUser> users = await message.GetReactionUsersAsync(emote, value.ReactionCount).FlattenAsync();
            await DeferAsync(ephemeral: true);
            SocketGuild guild = Context.Guild;
            _ = Task.Run(async () =>
            {
                int addedCount = 0;
                RestRole role = await guild.CreateRoleAsync(Guid.NewGuid().ToString());
                foreach (IUser user in users)
                {
                    SocketGuildUser guildUser = guild.GetUser(user.Id);
                    if (guildUser == null)
                    {
                        continue;
                    }
                    try
                    {
                        await guildUser.AddRoleAsync(role);
                    }
                    catch
                    {
                        continue;
                    }
                    addedCount++;
                }
                await FollowupAsync($"Done, added {addedCount}/{users.Count()} users to role {role.Mention}", ephemeral: true);
            });
        }
    }

    public class UtilsModuleConfig : ModuleConfiguration
    {
        public override string FileName => "Utils";
    }
}
