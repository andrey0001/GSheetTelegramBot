using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.Web.Models;

namespace GSheetTelegramBot.Web.Interfaces
{
    public interface IGoogleTableService
    {
        Task AddChangeToDailySummaryAsync(ChangeNotificationDto notification);
        Task<GoogleTable?> FindByGoogleSheetIdAsync(string googleSheetId);
        Task<bool> AddGoogleTableAsync(GoogleTableDto tableDto);
        Task<bool> DeleteGoogleTableAsync(string googleSheetId);
        Task<List<GoogleTable>> GetAllTablesAsync();
        Task<string?> GetGoogleSheetIdByIdAsync(int tableId);
        Task<List<ChangeNotification>> GetChangesForSubscriptionAsync(Subscription subscription, DateTime startDate,
            DateTime endDate);

        Task<string?> GetGoogleSheetNameByIdAsync(int tableId);
    }
}
