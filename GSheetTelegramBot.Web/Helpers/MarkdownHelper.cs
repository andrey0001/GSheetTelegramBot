namespace GSheetTelegramBot.Web.Helpers
{
    public static class MarkdownHelper
    {
        public static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var escapedChars = "_*[]()~`>#+-=|{}.!";
            foreach (var c in escapedChars)
            {
                text = text.Replace($"{c}", $"\\{c}");
            }
            return text;
        }
    }
}
