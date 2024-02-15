namespace GSheetTelegramBot.Models
{
    public class ProjectModel
    {
        private string Id { get; set; }
        private string Name { get; set; }
        private string SheetUrl { get; set; }
        private DateTime LastChecked { get; set; }
    }
}
