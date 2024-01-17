namespace TelegramBot.Info
{
    public class MailStorage : IMailStorage
    {
        private readonly List<EmailMessage> messages = new List<EmailMessage>();        //лист з листами :)

        public void AddMessage(EmailMessage message)        //метод, що дадє лист
        {
            messages.Add(message);
        }     
        
        public bool HasNewMessages()        //перевірка на наявність листа по кількості
        {
            return messages.Count > 0;
        }
        
        public EmailMessage GetMessage()        //отримання повідомлення
        {
            if (messages.Count == 0)
            {
                return null;
            }

            var message = messages[0];
            messages.RemoveAt(0);       //видалення повідомлення, що отримуємо
            return message;
        }
    }    
}
