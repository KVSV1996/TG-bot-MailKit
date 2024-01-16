using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Info;

namespace TelegramBot
{
    public class ProgramManager
    {
        private readonly ICommunication _communication;
        private readonly ITelegramBotClient _botClient;        
        private readonly IdleClient _imapIdle;        
        private List<long> _subscribers;
        private bool flag = true;
        private readonly IMailStorage _storage;


        public ProgramManager(ICommunication communication, ITelegramBotClient botClient, IMailStorage storage)
        {
            this._communication = communication ?? throw new ArgumentNullException(nameof(communication));
            this._botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _subscribers = new List<long>();
            _storage = new MailStorage();
            _imapIdle = new IdleClient(_storage);
           
        }       

        public void InitialiseBot()
        {                     
            _botClient.StartReceiving(UpdateAsync, Exeption);

            
            var idleTask = _imapIdle.RunAsync();

            Task.Run(() => {
                _communication.ReadLine();
            }).Wait();

            _imapIdle.Exit();

            idleTask.GetAwaiter().GetResult();
        }     


        private async Task UpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
        {           
            var massage = update.Message;

            if (massage == null)
            {
                return;
            }

            if(flag)
            {                
                StartMailKitProcessing(botClient);
                flag = false;
            }

            if (massage.ReplyToMessage != null )
            {               
                await botClient.DeleteMessageAsync(massage.Chat.Id, massage.ReplyToMessage.MessageId);
                await botClient.DeleteMessageAsync(massage.Chat.Id, massage.MessageId);

                return;
            }            

            if (massage.Text != null)
            {
                if (massage.Text == "/start")
                {
                    Log.Information($"ID[{massage.Chat.Id}]: /start");
                    if (!_subscribers.Contains(massage.Chat.Id))
                    {
                        _subscribers.Add(massage.Chat.Id);
                        await Console.Out.WriteLineAsync(massage.Chat.Id.ToString());
                    }
                    await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.Head);
                    return;
                }
                return;
            }
            
            else
            {
                Log.Error($"ID[{massage.Chat.Id}]: TypeException. User enter: {massage.Text}");
                await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.TypeException);
            }
        }

        private void StartMailKitProcessing(ITelegramBotClient botClient)
        {
            Task.Run(() => CheckAndDisplayMessagesAsync(botClient));
        }

        public async Task CheckAndDisplayMessagesAsync(ITelegramBotClient botClient)
        {
            while (true)
            {                
                if (_storage.HasNewMessages())
                {
                    var mailContent = _storage.GetMessage();
                    string fromMail;                    

                    if (mailContent.To.Contains("support@callway.com.ua") || mailContent.Cc.Contains("support@callway.com.ua"))
                    {
                        fromMail = "support@callway.com.ua";
                    }                    
                    else if (mailContent.To.Contains("support@ukrods.com.ua") || mailContent.Cc.Contains("support@ukrods.com.ua"))
                    {
                        fromMail = "support@ukrods.com.ua";
                    }
                    else
                    {
                        fromMail = "Unknown";
                    }

                    foreach (var chatId in _subscribers)
                    {                        
                        await botClient.SendTextMessageAsync(chatId, String.Format("На пошту: {0}  \nТема: {1} \nВід: {2} \nДата: {3} ", fromMail, mailContent.Subject, mailContent.From, mailContent.Date));                        
                    }

                    Log.Information("Очікування нового повідомлення");                    
                }
            }            
        }              

        private Task Exeption(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Log.Error($"An error occurred: {arg2.Message}");
            
            return Task.CompletedTask;
        }
    }
}
