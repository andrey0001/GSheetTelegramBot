using GSheetTelegramBot.DataLayer.DbModels;
using Microsoft.EntityFrameworkCore;
using GSheetTelegramBot.DataLayer.Enums;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using System.Net.Mail;

namespace GSheetTelegramBot.Web.Services
{
    public class UserService:IUserService
    {
        private readonly IDataRepo<User> _userRepo;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public UserService(IDataRepo<User> userRepo, IEmailService emailService, INotificationService notificationService, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _emailService = emailService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepo.Query().ToListAsync();
        }

        public async Task ChangeUserRoleAsync(int userId, UserRole newRole)
        {
            var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Role = newRole;
                await _userRepo.UpdateAsync(user);

            }
        }

        public async Task<bool> IsSuperAdmin(long chatId)
        {
            var user = await FindByChatIdAsync(chatId);
            return user?.Role == UserRole.SuperAdmin;
        }
        public async Task<User?> FindByChatIdAsync(long chatId)
        {
            return await _userRepo.Query().FirstOrDefaultAsync(u => u.ChatId == chatId);
        }

        private async Task<User?> GetUserByChatIdAsync(long chatId)
        {
            return await _userRepo.GetByChatIdAsync(chatId);
        }

        public async Task<bool> UserExists(long chatId)
        {
            return await FindByChatIdAsync(chatId) != null;
        }

        public async Task<bool> IsAwaitingEmailInput(long chatId)
        {
            var user = await FindByChatIdAsync(chatId);
            return user != null && user.IsAwaitingEmailInput;
        }
            

        public async Task<bool> IsAwaitingEmailConfirmation(long chatId)
        {
            var user = await FindByChatIdAsync(chatId);
            return user != null && user.IsAwaitingEmailConfirmation;
        }

        public async Task<bool> IsEmailConfirmed(long chatId)
        {
            var user = await FindByChatIdAsync(chatId);
            return user != null && user.IsEmailConfirmed;
        }

        public async Task<bool> IsAdminByEmail(string email)
        {
            var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Email == email);
            return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin);
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _userRepo.Query().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task SetAwaitingEmailInputStatus(long chatId)
        {
            var user = await GetUserByChatIdAsync(chatId);
            if (user != null)
            {
                try
                {
                    user.IsAwaitingEmailInput = true;
                   await _userRepo.UpdateAsync(user);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public async Task CreateUserAsync(long chatId)
        {
            var user = new User { ChatId = chatId, IsEmailConfirmed = false, TimeZoneId = "Asia/Baku" };
            await _userRepo.AddAsync(user);

            await SetAwaitingEmailInputStatus(chatId);
        }

        public async Task<UserRole> GetUserRoleAsync(long chatId)
        {
            var user = await FindByChatIdAsync(chatId);
            return user?.Role ?? UserRole.User;
        }

        public async Task RegisterUserEmailAsync(long chatId, string? email)
        {
            var user = await GetUserByChatIdAsync(chatId);
            if (user != null)
            {
                user.Email = email;
                user.Role = (email == "v.m.nashchekin@gmail.com" || email == "Terrastruc.bim@gmail.com") ? UserRole.SuperAdmin : UserRole.User;
                user.TimeZoneId = "Asia/Baku";
                user.IsAwaitingEmailInput = false;
                await _userRepo.UpdateAsync(user);

                await SendConfirmationEmailAsync(user);
            }
        }

        private async Task SendConfirmationEmailAsync(User user)
        {
            var token = GenerateEmailConfirmationToken();
            user.EmailConfirmationToken = token;
            user.EmailConfirmationTokenCreatedAt = DateTime.UtcNow;
            user.IsAwaitingEmailConfirmation = true;
            await _userRepo.UpdateAsync(user);

            var serverBaseUrl = _configuration["WEB_SERVER_URL"];
            var confirmationLink = $"{serverBaseUrl}/api/auth/confirm-email?token={token}";

            await _emailService.SendEmailAsync(user.Email, "Подтвердите вашу почту", $"Пожалуйста, перейдите по ссылке для подтверждения вашей электронной почты: {confirmationLink}");
        }

        public bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string? GenerateEmailConfirmationToken()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<(bool IsSuccess, long ChatId, string ErrorMessage)> ConfirmEmailAsync(string token)
        {
            var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user == null || !IsTokenValid(user.EmailConfirmationTokenCreatedAt))
            {
                return (false, 0, "Неверный или устаревший токен подтверждения.");
            }

            user.IsEmailConfirmed = true;
            user.IsAwaitingEmailConfirmation = false;
            user.IsAwaitingEmailInput = false;
            user.RegisteredAt = DateTime.UtcNow;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenCreatedAt = null;

            await _userRepo.UpdateAsync(user);

            return (true, user.ChatId, "");
        }

        private bool IsTokenValid(DateTime? tokenCreatedAt)
        {
            return tokenCreatedAt.HasValue && (DateTime.UtcNow - tokenCreatedAt.Value).TotalHours <= 24; 
        }

        public async Task UpdateUserTimeSettings(int userId, string timeZoneId, TimeSpan dailySummaryTime)
        {
            var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new ArgumentException("Пользователь не найден", nameof(userId));

            user.TimeZoneId = timeZoneId;
            user.DailySummaryTime = dailySummaryTime;
            await _userRepo.UpdateAsync(user);
            _notificationService.UpdateDailySummaryTask(user.Id, timeZoneId, dailySummaryTime);
        }
    }
}
