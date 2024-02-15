namespace GSheetTelegramBot.Models
{
    public class ChangeModel
    {
        private string ProjectId { get; set; }
        private string Cell { get; set; }
        private string OldValue { get; set; }
        private string NewValue { get; set; }
        private string ModifiedBy { get; set; }
        private DateTime ModifiedTime{ get; set; }
    }
}
