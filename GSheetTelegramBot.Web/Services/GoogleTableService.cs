using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GSheetTelegramBot.Web.Services;

public class GoogleTableService:IGoogleTableService
{
    private readonly IDataRepo<GoogleTable> _tableRepo;

    public GoogleTableService(
        IDataRepo<GoogleTable> tableRepo)
    {
        _tableRepo = tableRepo;
    }

    public async Task AddChangeToDailySummaryAsync(ChangeNotificationDto notification)
    {
        var googleTable = await _tableRepo.IncludeItems(gt => gt.DailyChanges)
            .FirstOrDefaultAsync(gt => gt.GoogleSheetId == notification.GoogleSheetId);

        if (googleTable != null)
        {
            var changeNotification = new ChangeNotification
            {
                GoogleSheetId = notification.GoogleSheetId,
                TableName = notification.TableName,
                SheetName = notification.SheetName,
                Hyperlink = notification.Hyperlink,
                ColumnName = notification.ColumnName,
                CellName = notification.CellName,
                OldValue = notification.OldValue,
                NewValue = notification.NewValue,
                ChangeTime = DateTime.UtcNow
            };

            googleTable.DailyChanges.Add(changeNotification);
            await _tableRepo.UpdateAsync(googleTable);
        }
    }

    public async Task<GoogleTable?> FindByGoogleSheetIdAsync(string googleSheetId)
    {
        return await _tableRepo.Query()
            .FirstOrDefaultAsync(gt => gt.GoogleSheetId == googleSheetId);
    }

    public async Task<bool> AddGoogleTableAsync(GoogleTableDto tableDto)
    {
        if (_tableRepo.Query().Any(gt => gt.GoogleSheetId == tableDto.GoogleSheetId)) return false;

        var newTable = new GoogleTable
        {
            Name = tableDto.Name,
            GoogleSheetId = tableDto.GoogleSheetId,
            HyperLink = tableDto.HyperLink
        };

        await _tableRepo.AddAsync(newTable);

        return true;
    }

    public async Task<bool> DeleteGoogleTableAsync(string googleSheetId)
    {
        var googleTable = await _tableRepo.Query().FirstOrDefaultAsync(gt => gt.GoogleSheetId == googleSheetId);
        if (googleTable == null) return false;

        await _tableRepo.DeleteAsync(googleTable.Id);

        return true;
    }

    public async Task<List<GoogleTable>> GetAllTablesAsync()
    {
        return await _tableRepo.Query().ToListAsync();
    }

    public async Task<string?> GetGoogleSheetIdByIdAsync(int tableId)
    {
        var googleTable = await _tableRepo.Query()
            .Where(gt => gt.Id == tableId)
            .FirstOrDefaultAsync();

        return googleTable?.GoogleSheetId;
    }

    public async Task<string?> GetGoogleSheetNameByIdAsync(int tableId)
    {
        var googleTable = await _tableRepo.Query()
            .Where(gt => gt.Id == tableId)
            .FirstOrDefaultAsync();

        return googleTable?.Name;
    }

    public async Task<List<ChangeNotification>> GetChangesForSubscriptionAsync(Subscription subscription, DateTime startDate, DateTime endDate)
    {
        var googleTable = await _tableRepo.IncludeItems(gt => gt.DailyChanges)
            .FirstOrDefaultAsync(gt => gt.GoogleSheetId == subscription.GoogleSheetId);

        if (googleTable == null) return new List<ChangeNotification>();

        var changes = googleTable.DailyChanges
            .Where(change => change.ChangeTime >= startDate && change.ChangeTime <= endDate)
            .ToList();

        return changes;
    }
}