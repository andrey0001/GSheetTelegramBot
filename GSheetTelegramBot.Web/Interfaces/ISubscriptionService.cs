using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.Web.Models;

namespace GSheetTelegramBot.Web.Interfaces
{
    public interface ISubscriptionService
    {
        Task<string> AddSubscriptionAsync(GoogleTableDto tableDto, int userId);
        Task<bool> RemoveSubscriptionAsync(string googleSheetId, int userId);
        Task<List<Subscription?>> GetSubscriptionsByGoogleSheetId(string googleSheetId);
        Task<List<Subscription?>> GetSubscriptionsByUserId(int userId);
        Task<bool> UpdateSubscriptionAsync(Subscription? subscription);
        Task<Subscription?> FindSubscriptionAsync(string googleSheetId, int userId);
        Task<Subscription?> FindSubscriptionByIdAsync(int subscriptionId);
        Task<bool> RemoveSubscriptionByIdAsync(int subscriptionId);
    }
}
