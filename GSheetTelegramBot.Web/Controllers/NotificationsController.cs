using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Models;
using GSheetTelegramBot.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GSheetTelegramBot.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly IGoogleTableService _tableService;
        private readonly INotificationService _notificationService;

        public NotificationsController(IGoogleTableService tableService, INotificationService notificationService)
        {
            _tableService = tableService;
            _notificationService = notificationService;

        }

        [HttpPost("add")]
        public async Task<IActionResult> Post([FromBody] ChangeNotificationDto notification)
        {
            var googleTable = await _tableService.FindByGoogleSheetIdAsync(notification.GoogleSheetId);
            if (googleTable == null)
            {
                return NotFound($"Таблица с GoogleSheetId: {notification.GoogleSheetId} не найдена. Администратору необходимо добавить данную таблицу в список отслеживаемых.");
            }

            await _tableService.AddChangeToDailySummaryAsync(notification);

            await _notificationService.NotifyInstantSubscribersAsync(notification);

            return Ok();
        }
    }
}
