using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Info
{
    public class MailStorage
    {
        private readonly List<EmailMessageInfo> messages = new List<EmailMessageInfo>();

        public void AddMessage(EmailMessageInfo message)
        {
            messages.Add(message);
        }      

        // Метод для проверки наличия новых сообщений
        public bool HasNewMessages()
        {
            return messages.Count > 0;
        }

        // Метод для получения и удаления сообщений из хранилища
        public EmailMessageInfo GetMessage()
        {
            if (messages.Count == 0)
            {
                return null;
            }

            var message = messages[0];
            messages.RemoveAt(0);
            return message;
        }
    }
}
