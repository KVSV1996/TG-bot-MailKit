using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class EmailMessageInfo
    {
        public string Subject { get; set; }
        public string From { get; set; }
        public DateTimeOffset Date { get; set; }
        
    }
}
