using GSheetTelegramBot.DataLayer.Enums;

namespace GSheetTelegramBot.DataLayer.DbModels;

public class User:BaseEntity
{
    public long TelegramId { get; set; }
    public long ChatId { get; set; }
    public string? Email { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsAwaitingEmailConfirmation { get; set; }
    public bool IsAwaitingEmailInput { get; set; }
    public DateTime RegisteredAt { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public UserRole Role { get; set; }
    public string TimeZoneId { get; set; }
    public TimeSpan DailySummaryTime { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenCreatedAt { get; set; } 
}