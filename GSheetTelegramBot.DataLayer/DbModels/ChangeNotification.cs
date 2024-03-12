using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSheetTelegramBot.DataLayer.DbModels
{
    public class ChangeNotification:BaseEntity
    {
        public string GoogleSheetId { get; set; }
        public string TableName { get; set; }
        public string SheetName { get; set; }
        public string Hyperlink { get; set; }
        public string ColumnName { get; set; }
        public string CellName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangeTime { get; set; }
    }
}
