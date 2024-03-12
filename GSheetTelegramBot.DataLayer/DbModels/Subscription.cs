using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSheetTelegramBot.DataLayer.DbModels
{
    public class Subscription:BaseEntity
    {
        public string? GoogleSheetId { get; set; }
        public bool InstantNotifications { get; set; }
        public bool DailySummary { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? TableName { get; set; }
    }
}
