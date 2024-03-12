using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GSheetTelegramBot.Web.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IDataRepo<Subscription> _subscriptionRepo;

        public SubscriptionService(IDataRepo<Subscription> subscriptionRepo)
        {
            _subscriptionRepo = subscriptionRepo;
        }

        public async Task<string> AddSubscriptionAsync(GoogleTableDto tableDto, int userId)
        {
            var tableExists = await _subscriptionRepo.Query().AnyAsync(s => s.GoogleSheetId == tableDto.GoogleSheetId);

            if (!tableExists)
                return "Вы не можете подписаться на таблицу, поскольку она не добавлена в приложение. Попросите администратора добавить ее.";

            var existingSubscription = await _subscriptionRepo.Query()
                .FirstOrDefaultAsync(s => s.GoogleSheetId == tableDto.GoogleSheetId && s.UserId == userId);

            if (existingSubscription != null)
                return "Вы уже подписаны на эту таблицу.";

            var newSubscription = new Subscription
            {
                GoogleSheetId = tableDto.GoogleSheetId,
                InstantNotifications = true,
                DailySummary = false,
                UserId = userId,
                TableName = tableDto.Name
            };

           await _subscriptionRepo.AddAsync(newSubscription);

            return "Подписка успешно оформлена.";
        }

        public async Task<bool> RemoveSubscriptionAsync(string googleSheetId, int userId)
        {
            var subscription = await FindSubscriptionAsync(googleSheetId, userId);

            try
            {
                if (subscription != null)
                   await _subscriptionRepo.DeleteAsync(subscription.Id);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateSubscriptionAsync(Subscription? subscription)
        {
            try
            {
               await _subscriptionRepo.UpdateAsync(subscription);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Subscription?>> GetSubscriptionsByGoogleSheetId(string googleSheetId)
        {
            return await _subscriptionRepo.Query()
                .Where(s => s.GoogleSheetId == googleSheetId)
                .ToListAsync();
        }

        public async Task<List<Subscription?>> GetSubscriptionsByUserId(int userId)
        {
            var subscriptions = await _subscriptionRepo.Query()
                .Where(s => s.UserId == userId)
                .ToListAsync();

            return subscriptions;
        }

        public async Task<Subscription?> FindSubscriptionAsync(string googleSheetId, int userId)
        {
            return await _subscriptionRepo.Query()
                .FirstOrDefaultAsync(s => s.GoogleSheetId == googleSheetId && s.UserId == userId);
        }

        public Task<Subscription?> FindSubscriptionByIdAsync(int subscriptionId)
        {
            return _subscriptionRepo.GetAsync(subscriptionId);
        }

        public async Task<bool> RemoveSubscriptionByIdAsync(int subscriptionId)
        {
            try
            {
               await _subscriptionRepo.DeleteAsync(subscriptionId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
