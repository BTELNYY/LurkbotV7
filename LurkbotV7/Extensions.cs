using Discord;
using System.Net;

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
            }
            return attachment.Url;
        }
    }
}
