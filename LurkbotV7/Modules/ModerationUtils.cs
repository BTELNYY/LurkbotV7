using Discord;
using Discord.Interactions;
using Discord.Net.Converters;
using Discord.WebSocket;
using LurkbotV7.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LurkbotV7.Modules
{
    public class ModerationUtils : CustomInteractionModuleBase<ModerationUtilsConfig>
    {
        public override void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            base.OnModuleBuilding(commandService, module);
            Program.Client.AuditLogCreated += AuditLog;
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

        private async Task AuditLog(SocketAuditLogEntry audit, SocketGuild guild)
        {
            if(!Config.ServerTargets.Contains(guild.Id))
            {
                return;
            }
            EmbedBuilder builder = GetAuditEmbedTemplate(audit, guild);
            switch (audit.Action)
            {
                case ActionType.Kick:
                    builder.WithColor(Color.Orange);
                    SocketKickAuditLogData kickData = audit.Data as SocketKickAuditLogData;
                    builder.AddField("Target", kickData.Target.Value.Mention);
                    builder.AddField("Reason", audit.Reason);
                    break;
                case ActionType.Ban:
                    builder.WithColor(Color.Red);
                    SocketBanAuditLogData banData = audit.Data as SocketBanAuditLogData;
                    builder.AddField("Target", banData.Target.Value.Mention);
                    builder.AddField("Reason", audit.Reason);
                    break;
                case ActionType.Unban:
                    builder.WithColor(Color.Green);
                    SocketUnbanAuditLogData unbanData = audit.Data as SocketUnbanAuditLogData;
                    builder.AddField("Target", unbanData.Target.Value.Mention);
                    break;
                case ActionType.MemberUpdated:
                    SocketMemberUpdateAuditLogData memberUpdateData = audit.Data as SocketMemberUpdateAuditLogData;
                    if(memberUpdateData.After.TimedOutUntil.HasValue && !memberUpdateData.Before.TimedOutUntil.HasValue)
                    {
                        builder.WithColor(Color.LightOrange);
                        builder.AddField("Update Action", "Timeout");
                        builder.AddField("Target", memberUpdateData.Target.Value.Mention);
                        builder.AddField("Until",  new TimestampTag(memberUpdateData.After.TimedOutUntil.Value.DateTime, TimestampTagStyles.Relative));
                    }
                    else if(!memberUpdateData.After.TimedOutUntil.HasValue && memberUpdateData.Before.TimedOutUntil.HasValue)
                    {
                        builder.WithColor(Color.Green);
                        builder.AddField("Update Action", "Timeout Expired");
                        builder.AddField("Target", memberUpdateData.Target.Value.Mention);
                    }
                    break;
            }
            foreach(ChannelTarget target in Config.AuditLogTargets)
            {
                SocketGuild targetGuild = Program.Client.GetGuild(target.ServerID);
                if(targetGuild == null)
                {
                    Log.Error($"Invalid Guild Target in ModerationUtils. ID: {target.ServerID}");
                    continue;
                }
                SocketGuildChannel targetChannel = targetGuild.GetChannel(target.ChannelID);
                if (targetGuild == null)
                {
                    Log.Error($"Invalid Channel Target in ModerationUtils. ID: {target.ChannelID}, Server: {guild.Name}");
                    continue;
                }
                if(targetChannel is not SocketTextChannel targetTextChannel)
                {
                    Log.Error($"Invalid Channel Target in ModerationUtils. Not a text channel. Name: {targetChannel.Name}, Server: {guild.Name}");
                    continue;
                }
                await targetTextChannel.SendMessageAsync(embed: builder.Build());
            }
        }
    }

    public class ModerationUtilsConfig : ModuleConfiguration
    {
        public override string FileName => "ModerationConfig";

        public List<ChannelTarget> AuditLogTargets { get; set; } = new List<ChannelTarget>();

        public List<ulong> ServerTargets { get; set; } = new();
    }
}
