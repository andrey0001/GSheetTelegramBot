using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Models;
using GSheetTelegramBot.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GSheetTelegramBot.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IGoogleTableService _googleTableService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly TelegramService _telegramService;

        public TablesController(IUserService userService, IGoogleTableService googleTableService,
            TelegramService telegramService, ISubscriptionService subscriptionTableService, INotificationService notificationService)
        {
            _userService = userService;
            _googleTableService = googleTableService;
            _telegramService = telegramService;
            _subscriptionService = subscriptionTableService;
            _notificationService = notificationService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddTable([FromBody] GoogleTableDto tableDto)
        {
            var isAdmin = await _userService.IsAdminByEmail(tableDto.UserEmail);
            if (!isAdmin)
            {
                return new ObjectResult(new { message = "Только администраторы могут добавлять таблицы." })
                {
                    StatusCode = 403
                };
            }

            var result = await _googleTableService.AddGoogleTableAsync(tableDto);
            if (!result)
            {
                return BadRequest(new { message = "Таблица уже существует в системе." });
            }

            await _notificationService.NotifyAdminsAboutTableAddition(tableDto);

            return Ok(new { message = "Таблица успешно добавлена." });
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveTable([FromBody] GoogleTableDto tableDto)
        {
        
            var isAdmin = await _userService.IsAdminByEmail(tableDto.UserEmail);
            if (!isAdmin)
            {
                return Forbid("Только администраторы могут удалять таблицы.");
            }

            var isSuccess = await _googleTableService.DeleteGoogleTableAsync(tableDto.GoogleSheetId);
            if (isSuccess)
            {
                return Ok("Таблица и связанные подписки удалены.");
            }
            else
            {
                return NotFound("Таблица не найдена.");
            }
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeToTable([FromBody] GoogleTableDto tableDto)
        {
            var user = await _userService.FindByEmailAsync(tableDto.UserEmail);
            if (user == null)
            {
                return NotFound(new { message = "Ошибка. Для подписки на таблицу Вы должны быть зарегистрированы в ТГ боте." });
            }

            var subscriptionMessage = await _subscriptionService.AddSubscriptionAsync(tableDto, user.Id);

            if (subscriptionMessage == "Подписка успешно оформлена.")
            {
                await _telegramService.SendTextMessageAsync(user.ChatId, $"Вы подписались на мгновенные уведомления таблицы: {tableDto.Name}/n{tableDto.HyperLink}");
            }

            return Ok(new { message = subscriptionMessage });
        }
    }
}
