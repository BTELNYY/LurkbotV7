using Discord;
using Discord.Interactions;
using Discord.Net.Converters;
using Discord.Rest;
using Discord.WebSocket;
using LurkbotV7.Attributes;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    [Module]
    public class ModerationUtils : CustomInteractionModuleBase<ModerationUtilsConfig>
    {
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
                builder.WithUrl(embed.Url);
                builder.WithColor(Color.LightOrange);
                builder.WithAuthor(message.Author);
                embeds[counter] = builder.Build();
                counter++;
            }
            //wtf kind of retarded ass logic is this
            //If RespondAsync() is not called before the other methods it just never fires?
            //???
            await RespondWithSuccesAsync("Done", ephemeral: true);
            await message.DeleteAsync();
            await SendAuditLog(embeds);
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
                    else if (memberUpdateData.After.TimedOutUntil.HasValue && memberUpdateData.After.TimedOutUntil.Value.DateTime.ToUniversalTime() < DateTime.Now.ToUniversalTime())
                    {
                        builder.WithColor(Color.Green);
                        builder.AddField("Update Action", "Timeout Expired");
                        builder.AddField("Target", memberUpdateData.Target.DownloadAsync().Result.Mention);
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
    }
}
