using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSheetTelegramBot.DataLayer.DbModels
{
    public class GoogleTable:BaseEntity
    {
        public string Name { get; set; }
        public string GoogleSheetId { get; set; }
        public string HyperLink { get; set; }
        public ICollection<ChangeNotification> DailyChanges { get; set; } = new List<ChangeNotification>();
    }
}
