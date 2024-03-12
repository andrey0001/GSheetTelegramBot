namespace GSheetTelegramBot.Web.Models
{
    public class UpdateSubscriptionDto
    {
        public long ChatId { get; set; }
        public string GoogleSheetId { get; set; }
        public bool InstantNotifications { get; set; }
        public bool DailySummary { get; set; }
    }
}
