namespace GSheetTelegramBot.Web.Models
{
    public class ChangeNotificationDto
    {
        public string GoogleSheetId { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string SheetName { get; set; } = string.Empty;
        public string Hyperlink { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string CellName { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public DateTime ChangeTime { get; set; }
    }
}
