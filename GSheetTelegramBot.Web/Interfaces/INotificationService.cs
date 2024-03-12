using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.Web.Models;

namespace GSheetTelegramBot.Web.Interfaces
{
    public interface INotificationService
    {
        Task NotifyInstantSubscribersAsync(ChangeNotificationDto notification);
        Task NotifyAdminsAboutTableAddition(GoogleTableDto table);
        Task NotifyAdminsAboutTableDeletion(string tableName, string userEmail);
        Task NotifyUsersAboutSubscriptionDeletion(List<Subscription?> subscriptions, string tableName);
        void UpdateDailySummaryTask(int userId, string timeZoneId, TimeSpan dailySummaryTime);
    }
}
