using GSheetTelegramBot.DataLayer.Enums;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Telegram.Bot.Types.Enums;

namespace GSheetTelegramBot.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly TelegramService _telegramService;

        public AuthController(IUserService userService, TelegramService telegramService)
        {
            _userService = userService;
            _telegramService = telegramService;
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var result = await _userService.ConfirmEmailAsync(token);

            if (!result.IsSuccess)
            {
                await _telegramService.SendTextMessageAsync(result.ChatId, result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }

            var user = await _userService.FindByChatIdAsync(result.ChatId);
            await _userService.UpdateUserTimeSettings(user.Id, "Asia/Baku", new TimeSpan(19, 0, 0));

            var greetingMessage = "*🔔 Ваша почта подтверждена\\.*\n" +
                                  "Ваш часовой пояс установлен на \\(GMT\\+4\\) \\*Asia/Baku\\*\\.\n" +
                                  "🕰️ Время дневных уведомлений: \\*19:00\\*\\.\n" +
                                  "При необходимости вы можете изменить это в настройках\\.";

            try
            {
                await _telegramService.SendTextMessageAsync(result.ChatId, greetingMessage, ParseMode.MarkdownV2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var userRole = await _userService.GetUserRoleAsync(result.ChatId);
            try
            {
                await _telegramService.SendMainMenuAsync(result.ChatId, userRole);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        
            return Ok("Email подтвержден.");
        }
    }
}
