using GSheetTelegramBot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace GSheetTelegramBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly BotService _botService;

        public TelegramController(BotService botService)
        {
            _botService = botService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null) return BadRequest();


            return Ok();
        }
    }
}
