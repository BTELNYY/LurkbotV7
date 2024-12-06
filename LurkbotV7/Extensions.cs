using Discord;
using System.Net;
using System.Reflection.Metadata.Ecma335;

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
    }
}
