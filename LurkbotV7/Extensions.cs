namespace LurkbotV7
{
    public static class Extensions
    {
        public static string DiscordValidateReason(this string reason)
        {
            return string.IsNullOrEmpty(reason) ? "???" : reason;
        }
    }
}
