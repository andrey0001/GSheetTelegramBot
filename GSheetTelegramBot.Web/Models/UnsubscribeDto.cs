namespace GSheetTelegramBot.Web.Models
{
    public class UnsubscribeDto
    {
        public long ChatId { get; set; }
        public string GoogleSheetId { get; set; }
    }
}
