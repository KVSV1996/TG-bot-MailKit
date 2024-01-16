using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Info
{
    public interface IMailStorage
    {
        public void AddMessage(EmailMessage message);
        public bool HasNewMessages();
        public EmailMessage GetMessage();
        
    }
}
