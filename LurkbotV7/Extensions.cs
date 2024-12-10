using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace LurkbotV7
{
    public static class Extensions
    {
        public static string DiscordValidateReason(this string reason)
        {
            return string.IsNullOrEmpty(reason) ? "???" : reason;
        }

        public static string DownloadAttachment(this IAttachment attachment)
        {
            using (var client = new HttpClient())
            {
                string filetype = Path.GetExtension(attachment.Url);
                HttpResponseMessage response = client.GetAsync(attachment.Url).Result;
                if(!Directory.Exists(Path.Combine(Program.Config.CachePath)))
                {
                    Directory.CreateDirectory(Program.Config.CachePath);
                }
                string path = Path.Combine(Program.Config.CachePath, $"{attachment.Id}-{attachment.Filename}");
                File.WriteAllBytes(path, response.Content.ReadAsByteArrayAsync().Result);
                return path;
            }
        }

        public static string GetMention(this IChannel channel)
        {
            return "<#" + channel.Id + ">";
        }

        private static void GetMessageTreeRecursive(IMessage message, ref List<IMessage> messages)
        {     
            if(message.Reference == null)
            {
                return;
            }
            if(!message.Reference.ReferenceType.IsSpecified)
            {
                return;
            }
            SocketGuild guild = null;
            if (message.Reference.GuildId.IsSpecified)
            {
                guild = Program.Client.GetGuild(message.Reference.GuildId.Value);
            }
            SocketChannel channel = guild == null ? Program.Client.GetChannel(message.Reference.ChannelId) : guild.GetChannel(message.Reference.ChannelId);
            if (channel == null)
            {
                return;
            }
            if(channel is not SocketTextChannel textChannel)
            {
                return;
            }
            if(!message.Reference.MessageId.IsSpecified)
            {
                return;
            }
            IMessage sockMessage = textChannel.GetMessageAsync(message.Reference.MessageId.Value).Result;
            if(sockMessage == null)
            {
                return;
            }
            //Log.Info(sockMessage.GetType().FullName);
            //Log.Debug($"Insert new Message into tree. {message.CleanContent}");
            messages.Insert(0, sockMessage);
            GetMessageTreeRecursive(sockMessage, ref messages);
        }

        public static List<IMessage> GetMessageTree(this IMessage message)
        {
            List<IMessage> messages = new();
            GetMessageTreeRecursive(message, ref messages);
            return messages;
        }

        public static MessageReference GetMessageReference(this SocketMessage message)
        {
            return new MessageReference(message.Id, message.Channel == null ? null : message.Channel.Id, message.Channel is SocketGuildChannel channel ?  channel.Id : null);
        }

        public static string StripMentions(this string messageString)
        {
            messageString = Regex.Replace(messageString, "<@!*&*[0-9A-Z#]+>", "");
            messageString = Regex.Replace(messageString, "@!*&*[0-9A-Z#]+", "");
            messageString = messageString.Trim();
            return messageString;
        }
    }
}
