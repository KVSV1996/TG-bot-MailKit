namespace TelegramBot.Info
{
    public class MailStorage : IMailStorage
    {
        private readonly List<EmailMessage> messages = new List<EmailMessage>();

        public void AddMessage(EmailMessage message)
        {
            messages.Add(message);
        }     
        
        public bool HasNewMessages()
        {
            return messages.Count > 0;
        }
        
        public EmailMessage GetMessage()
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
