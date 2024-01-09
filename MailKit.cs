using MailKit;
using MailKit.Net.Imap;
using Serilog;

namespace TelegramBot
{
    public class MailKit
    {
        private readonly ImapClient _client;
        private readonly Configuration configuration = new Configuration();
        private bool flagCount = true;
        private int countOfMails;
        public MailKit(ImapClient client) 
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
        }
        public MimeKit.MimeMessage? Mail()
        {
            try
            {
                if (!_client.IsConnected)
                {
                    _client.Connect(configuration.Provaider, configuration.Port, true); // Подключение к IMAP серверу
                    _client.Authenticate(configuration.Loging, configuration.Password); // Аутентификация
                }

                var inbox = _client.Inbox;
                inbox.Open(FolderAccess.ReadOnly); // Открытие почтового ящика

                if (flagCount)
                {
                    countOfMails = inbox.Count;
                    flagCount = false;
                    Log.Verbose(inbox.Count + "  " + "false");                    
                }               

                for (int i = countOfMails; i < inbox.Count;)
                {
                    var message = inbox.GetMessage(i);
                    countOfMails++;
                    return message; // Может возникнуть исключение, если i вне диапазона
                }

                return null; // Если новых сообщений нет
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                return null; // Возвращаем null или соответствующее сообщение об ошибке
            }
            finally
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect(true); // Отключение от сервера
                }
            }

        }
    }
}
