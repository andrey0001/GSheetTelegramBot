using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Enums;

namespace GSheetTelegramBot.Web.Interfaces;

public interface IUserService
{
    Task<bool> UserExists(long chatId);
    Task<User?> FindByChatIdAsync(long chatId);
    Task<bool> IsAwaitingEmailInput(long chatId);
    Task<bool> IsAwaitingEmailConfirmation(long chatId);
    Task<bool> IsEmailConfirmed(long chatId);
    Task<bool> IsAdminByEmail(string email);
    Task<User?> FindByEmailAsync(string email);
    Task SetAwaitingEmailInputStatus(long chatId);
    Task CreateUserAsync(long chatId);
    Task<UserRole> GetUserRoleAsync(long chatId);
    Task RegisterUserEmailAsync(long chatId, string? email);
    Task<(bool IsSuccess, long ChatId, string ErrorMessage)> ConfirmEmailAsync(string token);
    bool IsValidEmail(string email);
    Task UpdateUserTimeSettings(int userId, string timeZoneId, TimeSpan dailySummaryTime);
    Task<List<User>> GetAllUsersAsync();
    Task ChangeUserRoleAsync(int userId, UserRole newRole);
    Task<bool> IsSuperAdmin(long chatId);
}