using Discord;
using Discord.Interactions;
using Discord.Net.Converters;
using Discord.Rest;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    [Module]
    public class ModerationUtils : CustomInteractionModuleBase<ModerationUtilsConfig>
    {
        public class LockedChannel
        {
            public ulong ChannelID = 0;
            public ulong ServerID = 0;
            public List<Overwrite> Overwrites = new List<Overwrite>();
        }

        public static List<LockedChannel> Channels = new List<LockedChannel>();


        [SlashCommand("lockdown", "Locks the current channel", runMode: RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task LockdownChannel(string reason = null)
        {
            if(string.IsNullOrWhiteSpace(reason) || string.IsNullOrEmpty(reason))
            {
                reason = "No reason given.";
            }
            await RespondAsync("Done", ephemeral: true);
            await Task.Run(async () =>
            {
                IChannel channel = Context.Channel;
                if (channel is not SocketTextChannel textChannel)
                {
                    await RespondWithErrorAsync("Channel is not a text channel and therefore is not supported.", ephemeral: true);
                    return;
                }
                EmbedBuilder builder = GetAuditEmbedTemplate(Context.User, Context.Guild);
                LockedChannel lockedChannel = Channels.FirstOrDefault(x => x.ChannelID == textChannel.Id && x.ServerID == Context.Guild.Id);
                //await DeferAsync(ephemeral: true);
                if (lockedChannel != default(LockedChannel))
                {
                    foreach (Overwrite overwrite in lockedChannel.Overwrites)
                    {
                        if (overwrite.Permissions.ManageChannel == PermValue.Allow)
                        {
                            continue;
                        }
                        if (overwrite.TargetType == PermissionTarget.Role)
                        {
                            IRole role = Program.Client.GetGuild(lockedChannel.ServerID)?.GetRole(overwrite.TargetId);
                            if (role == default(IRole))
                            {
                                Log.Warning($"Can't find role {overwrite.TargetId}!");
                                continue;
                            }
                            await textChannel.AddPermissionOverwriteAsync(role, overwrite.Permissions);
                        }
                        else
                        {
                            IUser user = Program.Client.GetGuild(lockedChannel.ServerID)?.GetUser(overwrite.TargetId);
                            if (user == default(IUser))
                            {
                                continue;
                            }
                            await textChannel.AddPermissionOverwriteAsync(user, overwrite.Permissions);
                        }
                    }
                    Channels.Remove(lockedChannel);
                    await textChannel.SendMessageAsync(Config.LockdownReleasedMessage);
                    builder.AddField("Action", "Unlock Channel");
                    builder.AddField("Channel", Context.Channel.GetMention());
                    builder.WithColor(Color.Green);
                    await SendAuditLog(builder.Build());
                }
                else
                {
                    LockedChannel lockedChannelCreated = new LockedChannel();
                    lockedChannelCreated.ServerID = textChannel.Guild.Id;
                    lockedChannelCreated.ChannelID = textChannel.Id;
                    SocketRole defaultRole = Program.Client.GetGuild(lockedChannelCreated.ServerID)?.EveryoneRole;
                    if (defaultRole == default(SocketRole))
                    {
                        await RespondWithErrorAsync("Command failed: defaultRole couldn't be found.");
                        return;
                    }
                    foreach (Overwrite overwrite in textChannel.PermissionOverwrites)
                    {
                        if (overwrite.Permissions.ManageChannel == PermValue.Allow)
                        {
                            continue;
                        }
                        lockedChannelCreated.Overwrites.Add(overwrite);
                        if (overwrite.TargetType == PermissionTarget.Role)
                        {
                            IRole role = Program.Client.GetGuild(lockedChannelCreated.ServerID)?.GetRole(overwrite.TargetId);
                            if (role == default(IRole))
                            {
                                continue;
                            }
                            await textChannel.RemovePermissionOverwriteAsync(role);
                        }
                        else
                        {
                            IUser user = Program.Client.GetGuild(lockedChannelCreated.ServerID)?.GetUser(overwrite.TargetId);
                            if (user == default(IUser))
                            {
                                continue;
                            }
                            await textChannel.RemovePermissionOverwriteAsync(user);
                        }
                    }
                    OverwritePermissions permissions = new OverwritePermissions(sendMessages: PermValue.Deny, attachFiles: PermValue.Deny, embedLinks: PermValue.Deny, addReactions: PermValue.Deny);
                    await textChannel.AddPermissionOverwriteAsync(defaultRole, permissions);
                    Channels.Add(lockedChannelCreated);
                    await textChannel.SendMessageAsync(Config.LockdownMessage + $"\nReason: {reason}");
                    builder.AddField("Action", "Lock Channel");
                    builder.AddField("Channel", Context.Channel.GetMention());
                    builder.WithColor(Color.Red);
                    await SendAuditLog(builder.Build());
                }
            });
        }

        [MessageCommand("Delete and Audit")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task DeleteMessageAuditLog(IMessage message)
        {
            DeletionModal modal = new DeletionModal();
            modal.MessageID = message.Id.ToString();
            await Context.Interaction.RespondWithModalAsync<DeletionModal>("deletion_confirmation", modal);
        }

        public class DeletionModal : IModal
        {
            public string Title => "Delete Confirmation";

            [InputLabel("Message ID")]
            [ModalTextInput("messageid")]
            public string MessageID { get; set; }

            [InputLabel("Reason for deletion")]
            [ModalTextInput("reason", initValue: "None")]
            public string Reason { get; set; } 
        }

        [ModalInteraction("deletion_confirmation")]
        public async Task ProcessDelete(DeletionModal modal)
        {
            IMessage message = Context.Channel.GetMessageAsync(ulong.Parse(modal.MessageID)).Result;
            EmbedBuilder eb = new EmbedBuilder();
            if (Context.Channel is not SocketGuildChannel channel)
            {
                await RespondAsync("This command can only be ran in a guild.");
                return;
            }
            //wtf kind of retarded ass logic is this
            //If RespondAsync() is not called before the other methods it just never fires?
            //???
            await RespondWithSuccesAsync("Done", ephemeral: true);
            _ = Task.Run(async () =>
            {
                eb.WithTitle("Message was deleted");
                eb.WithColor(Color.LightOrange);
                eb.WithDescription("Since this was done via the application command, it is counted as an audit. **Deleting through the right click context menu will not produce this message.** \nDeleted content is attached below.");
                eb.WithCurrentTimestamp();
                eb.AddField("Created", new TimestampTag(DateTime.Now, TimestampTagStyles.Relative));
                eb.AddField("Server", channel.Guild.Name);
                eb.AddField("Action", "Message Delete");
                eb.AddField("Channel", "<#" + channel.Id + ">");
                string content = string.IsNullOrEmpty(message.Content) ? "<No Content, likely an embed>" : message.Content;
                eb.AddField("Content", content);
                eb.AddField("Reason", modal.Reason);
                eb.AddField("Message Author", message.Author.Mention);
                eb.WithAuthor(Context.User);
                Embed[] embeds = new Embed[1 + message.Embeds.Count + message.Attachments.Count];
                embeds[0] = eb.Build();
                int counter = 1;
                foreach (Embed embed in message.Embeds)
                {
                    EmbedBuilder builder = new EmbedBuilder();
                    switch (embed.Type)
                    {
                        case EmbedType.Video:
                            if (embed.Video.HasValue)
                            {
                                builder.WithUrl(embed.Video.Value.Url);
                            }
                            break;
                        case EmbedType.Gifv:
                            if (embed.Image.HasValue)
                            {
                                builder.WithUrl(embed.Image.Value.Url);
                            }
                            break;
                        case EmbedType.Image:
                            if (embed.Image.HasValue)
                            {
                                builder.WithUrl(embed.Image.Value.Url);
                            }
                            break;
                        case EmbedType.Link:
                            builder.WithUrl(embed.Url);
                            break;
                        default:
                            builder.AddField("Emebed Type", embed.Type.ToString());
                            break;
                    }
                    builder.WithColor(Color.LightOrange);
                    builder.WithAuthor(message.Author);
                    embeds[counter] = builder.Build();
                    counter++;
                }
                await message.DeleteAsync();
                await SendAuditLog(embeds);
            });
        }

        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            Program.Client.AuditLogCreated += (audit, guild) =>
            {
                Task.Run(async () => 
                {
                    await AuditLog(audit, guild);
                });
                return Task.CompletedTask;
            };
        }

        private EmbedBuilder GetAuditEmbedTemplate(SocketAuditLogEntry audit, SocketGuild guild)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Audit Log Created");
            eb.WithColor(Color.LighterGrey);
            eb.WithCurrentTimestamp();
            eb.AddField("Created", new TimestampTag(audit.CreatedAt, TimestampTagStyles.Relative));
            eb.AddField("Server", guild.Name);
            eb.AddField("Action", audit.Action);
            eb.WithAuthor(audit.User);
            return eb;
        }

        private EmbedBuilder GetAuditEmbedTemplate(SocketUser author, SocketGuild guild)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Audit Log Created");
            eb.WithColor(Color.LighterGrey);
            eb.WithCurrentTimestamp();
            eb.AddField("Created", new TimestampTag(DateTime.UtcNow, TimestampTagStyles.Relative));
            eb.AddField("Server", guild.Name);
            eb.WithAuthor(author);
            return eb;
        }

        private Task AuditLog(SocketAuditLogEntry audit, SocketGuild guild)
        {
            if (!Config.ServerTargets.Contains(guild.Id))
            {
                return Task.CompletedTask;
            }
            EmbedBuilder builder = GetAuditEmbedTemplate(audit, guild);
            switch (audit.Action)
            {
                case ActionType.Kick:
                    builder.WithColor(Color.Orange);
                    SocketKickAuditLogData kickData = audit.Data as SocketKickAuditLogData;
                    builder.AddField("Target", kickData.Target.DownloadAsync().Result.Mention);
                    builder.AddField("Reason", audit.Reason.DiscordValidateReason());
                    break;
                case ActionType.Ban:
                    builder.WithColor(Color.Red);
                    SocketBanAuditLogData banData = audit.Data as SocketBanAuditLogData;
                    builder.AddField("Target", banData.Target.DownloadAsync().Result.Mention);
                    builder.AddField("Reason", audit.Reason.DiscordValidateReason());
                    break;
                case ActionType.Unban:
                    builder.WithColor(Color.Green);
                    SocketUnbanAuditLogData unbanData = audit.Data as SocketUnbanAuditLogData;
                    builder.AddField("Target", unbanData.Target.DownloadAsync().Result.Mention);
                    break;
                case ActionType.MemberUpdated:
                    SocketMemberUpdateAuditLogData memberUpdateData = audit.Data as SocketMemberUpdateAuditLogData;
                    if (memberUpdateData.After.TimedOutUntil.HasValue && memberUpdateData.After.TimedOutUntil.Value.DateTime.ToUniversalTime() > DateTime.Now.ToUniversalTime())
                    {
                        builder.WithColor(Color.LightOrange);
                        builder.AddField("Update Action", "Timeout");
                        builder.AddField("Target", memberUpdateData.Target.DownloadAsync().Result.Mention);
                        builder.AddField("Reason", audit.Reason.DiscordValidateReason());
                        builder.AddField("Until", new TimestampTag(memberUpdateData.After.TimedOutUntil.Value.DateTime, TimestampTagStyles.Relative));
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                    break;
                default:
                    return Task.CompletedTask;
            }
            SendAuditLog(builder.Build()).RunSynchronously();
            return Task.CompletedTask;
        }

       
        public Task SendAuditLog(Embed embed)
        {
            return SendAuditLog(new Embed[] { embed });
        }

        public Task SendAuditLog(Embed[] embeds)
        {
            foreach (ChannelTarget target in Config.AuditLogTargets)
            {
                SocketGuild targetGuild = Program.Client.GetGuild(target.ServerID);
                if (targetGuild == null)
                {
                    Log.Error($"Invalid Guild Target in ModerationUtils. ID: {target.ServerID}");
                    continue;
                }
                SocketGuildChannel targetChannel = targetGuild.GetChannel(target.ChannelID);
                if (targetGuild == null)
                {
                    Log.Error($"Invalid Channel Target in ModerationUtils. ID: {target.ChannelID}, Server: {targetGuild.Name}");
                    continue;
                }
                if (targetChannel is not SocketTextChannel targetTextChannel)
                {
                    Log.Error($"Invalid Channel Target in ModerationUtils. Not a text channel. Name: {targetChannel.Name}, Server: {targetGuild.Name}");
                    continue;
                }
                targetTextChannel.SendMessageAsync(embeds: embeds).RunSynchronously();
            }
            return Task.CompletedTask;
        }
    }

    public class ModerationUtilsConfig : ModuleConfiguration
    {
        public override string FileName => "ModerationConfig";

        public List<ChannelTarget> AuditLogTargets { get; set; } = new List<ChannelTarget>()
        {
            new ChannelTarget()
            {
                ServerID = 951311770843230238,
                ChannelID = 1044707676371820545
            }
        };

        public List<ulong> ServerTargets { get; set; } = new()
        {
            653788451380002817
        };

        public string LockdownMessage { get; set; } = "This channel has been locked down.";

        public string LockdownReleasedMessage { get; set; } = "This channel has been unlocked.";
    }
}
