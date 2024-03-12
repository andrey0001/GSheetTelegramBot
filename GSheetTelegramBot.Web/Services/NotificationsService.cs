using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Enums;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using GSheetTelegramBot.Web.Helpers;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Telegram.Bot.Types.Enums;

namespace GSheetTelegramBot.Web.Services;

public class NotificationsService : INotificationService
{
    private readonly IGoogleTableService _googleTableService;
    private readonly IDataRepo<Subscription> _subscriptionRepo;
    private readonly TelegramService _telegramService;
    private readonly IDataRepo<User> _userRepo;

    public NotificationsService(IDataRepo<Subscription> subscriptionRepo, IDataRepo<User> userRepo,
        TelegramService telegramService, IGoogleTableService googleTableService)
    {
        _subscriptionRepo = subscriptionRepo;
        _userRepo = userRepo;
        _telegramService = telegramService;
        _googleTableService = googleTableService;
    }

    public async Task NotifyInstantSubscribersAsync(ChangeNotificationDto notification)
    {
        var subscriptionsWithUsers = await _subscriptionRepo
            .IncludeItems(s => s.User)
            .Where(s => s.GoogleSheetId == notification.GoogleSheetId && s.InstantNotifications)
            .Select(s => new { s.User.ChatId })
            .ToListAsync();

        foreach (var item in subscriptionsWithUsers)
            try
            {
                var message = InstantNotificationMessage(notification);
                await _telegramService.SendTextMessageAsync(item.ChatId, message, ParseMode.MarkdownV2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
    }

    public async Task NotifyAdminsAboutTableAddition(GoogleTableDto table)
    {
        var adminUsers = await _userRepo.Query().Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        foreach (var admin in adminUsers)
        {
            var message = TableAddedMessage(table.Name, table.UserEmail, table.HyperLink);
            await _telegramService.SendTextMessageAsync(admin.ChatId, message, ParseMode.MarkdownV2);
        }
    }

    public async Task NotifyAdminsAboutTableDeletion(string tableName, string userEmail)
    {
        var adminUsers = await _userRepo.Query()
            .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        foreach (var admin in adminUsers)
        {
            var message = TableDeletedMessage(tableName, userEmail);
            await _telegramService.SendTextMessageAsync(admin.ChatId, message, ParseMode.MarkdownV2);
        }
    }

    public async Task NotifyUsersAboutSubscriptionDeletion(List<Subscription?> subscriptions, string tableName)
    {
        var userIds = subscriptions.Select(s => s.UserId).Distinct().ToList();

        var users = await _userRepo.Query()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            var message = SubscriptionDeletedMessage(tableName);
            await _telegramService.SendTextMessageAsync(user.ChatId, message, ParseMode.MarkdownV2);
        }
    }

    public void UpdateDailySummaryTask(int userId, string timeZoneId, TimeSpan dailySummaryTime)
    {
        var userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localTime = DateTime.Today.Add(dailySummaryTime);
        var unspecifiedTime = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);

        try
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedTime, userTimeZoneInfo).TimeOfDay;
            var cronExpression = CreateCronExpression(utcTime);

            var recurringJobId = $"DailySummary_{userId}";

            RecurringJob.AddOrUpdate(recurringJobId,
                () => NotifyDailySummaryAsync(userId),
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = userTimeZoneInfo
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private string InstantNotificationMessage(ChangeNotificationDto notification)
    {
        return $"*🔔 📊 Таблица {MarkdownHelper.EscapeMarkdown(notification.TableName)}*\n\n" +
               $"📄 *Лист:* `{MarkdownHelper.EscapeMarkdown(notification.SheetName)}`\n\n" +
               $"🏷️ *Колонка:* `{MarkdownHelper.EscapeMarkdown(notification.ColumnName)}`\n" +
               $"🔲 *Ячейка:* `{MarkdownHelper.EscapeMarkdown(notification.CellName)}`\n\n" +
               $"🕰️ *Старое значение:*\n`{MarkdownHelper.EscapeMarkdown(notification.OldValue)}`\n\n" +
               $"🆕 *Новое значение:*\n`{MarkdownHelper.EscapeMarkdown(notification.NewValue)}`\n\n" +
               $"🔗 [Подробнее]({notification.Hyperlink})";
    }

    private string SubscriptionDeletedMessage(string tableName)
    {
        var escapedTableName = MarkdownHelper.EscapeMarkdown(tableName);
        return
            $"🚫 Ваша подписка на таблицу *{escapedTableName}* была удалена, поскольку таблица удалена из системы * 🗑️";
    }

    private string TableAddedMessage(string tableName, string userEmail, string hyperlink)
    {
        var escapedTableName = MarkdownHelper.EscapeMarkdown(tableName);
        return
            $"📢 *Новая таблица {escapedTableName} добавлена* 🔔 пользователем {userEmail}. [Подробнее]({hyperlink})";
    }

    private string TableDeletedMessage(string tableName, string userEmail)
    {
        var escapedTableName = MarkdownHelper.EscapeMarkdown(tableName);
        return $"📢 *Tаблица {escapedTableName} удалена из системы* 🗑️ пользователем {userEmail}.";
    }

    private string CreateCronExpression(TimeSpan utcTime)
    {
        return $"{utcTime.Minutes} {utcTime.Hours} * * *";
    }

    public async Task NotifyDailySummaryAsync(int userId)
    {
        var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId);

        var subscriptions = await _subscriptionRepo.Query()
            .Where(s => s.UserId == userId && s.DailySummary)
            .ToListAsync();

        var changesList = new List<ChangeNotification>();
        foreach (var subscription in subscriptions)
        {
            var changes =
                await _googleTableService.GetChangesForSubscriptionAsync(subscription, DateTime.UtcNow.AddDays(-1),
                    DateTime.UtcNow);
            changesList.AddRange(changes);
        }

        if (changesList.Any()) await GenerateAndSendDailySummaryPdfAsync(user.ChatId, changesList);
    }

    [Obsolete("Obsolete")]
    public async Task GenerateAndSendDailySummaryPdfAsync(long chatId, IEnumerable<ChangeNotification> changes)
    {
        var pdfFilePath = "daily_summary.pdf";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(ComposeHeader);
                page.Content().Element(container =>
                {
                    var groupedChanges = changes.GroupBy(change => change.GoogleSheetId);
                    foreach (var group in groupedChanges)
                    {
                        var firstChange = group.First();

                        container.PaddingVertical(5).Row(row =>
                        {
                            row.RelativeItem().Stack(stack =>
                            {
                                stack.Item().Text($"🔔 📊 Таблица: {firstChange.TableName}",
                                    TextStyle.Default.Size(16).Bold());
                                stack.Item().Text($"🔗 Открыть таблицу: {firstChange.Hyperlink}");

                                foreach (var change in group)
                                {
                                    stack.Item().Text($"📄 Лист: {change.SheetName}");
                                    stack.Item().Text($"🏷️ Колонка: {change.ColumnName}");
                                    stack.Item().Text($"🔲 Ячейка: {change.CellName}");
                                    stack.Item().Text($"🕰️ Старое значение: {change.OldValue}",
                                        TextStyle.Default.Italic());
                                    stack.Item().Text($"🆕 Новое значение: {change.NewValue}");
                                    stack.Item().PaddingBottom(5)
                                        .Text("------------------------------------------------");
                                }
                            });
                        });
                    }
                });
            });
        }).GeneratePdf(pdfFilePath);
        var message = "✉️ *Файл с Вашими девными уведомлениями:";
        await _telegramService.SendTextMessageAsync(chatId, message, ParseMode.MarkdownV2);
        await SendDailySummaryPdfToTelegramAsync(chatId, pdfFilePath);

        File.Delete(pdfFilePath);
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row => { row.ConstantItem(100).Height(40).Text("Daily Summary").DirectionAuto().SemiBold(); });
    }

    private async Task SendDailySummaryPdfToTelegramAsync(long chatId, string pdfFilePath)
    {
        using (var fileStream = new FileStream(pdfFilePath, FileMode.Open))
        {
            await _telegramService.SendPdfDocumentAsync(chatId, fileStream, "daily_summary.pdf",
                CancellationToken.None);
        }
    }
}