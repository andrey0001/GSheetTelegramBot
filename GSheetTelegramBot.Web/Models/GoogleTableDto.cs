using GSheetTelegramBot.DataLayer.DbModels;

namespace GSheetTelegramBot.Web.Models
{
    public class GoogleTableDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GoogleSheetId { get; set; } = string.Empty;
        public string HyperLink { get; set; } = string.Empty;
    }
}
