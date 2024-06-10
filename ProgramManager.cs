using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Info;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using TelegramBot.Info.Interface;

namespace TelegramBot
{
    public class ProgramManager 
    {        
        private readonly ITelegramBotClient _botClient;
        private readonly IdleClient _imapIdle;
        private bool flag = true;
        private readonly IMailStorage _storage;
        private readonly Configuration _configuration;
        private readonly ISubscriberStorage _subscriberStorage;
        private CancellationTokenSource stoppingToken;
        private IChatIdStorage _chatIdStorage;


        public ProgramManager( ITelegramBotClient botClient, IMailStorage storage, Configuration configuration, ISubscriberStorage subscriberStorage, IChatIdStorage chatIdStorage)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _storage = new MailStorage();
            _imapIdle = new IdleClient(_storage, _configuration);
            _subscriberStorage = subscriberStorage ?? throw new ArgumentNullException(nameof(subscriberStorage));
            _chatIdStorage = chatIdStorage;
        }

        public void Start()
        {
            stoppingToken = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateAsync, Exeption);       //запускаємо бота
            var idleTask = _imapIdle.RunAsync();                //запускаємо поштовик
            AddChatIdFromFile();
            Task.Run(() => CheckAndDisplayMessagesAsync(_botClient, stoppingToken.Token), stoppingToken.Token);

            var logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
        }
        public void Stop()
        {
            _imapIdle.Exit();
            Log.CloseAndFlush();
        }

        private async Task UpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var massage = update.Message;
            DateTime now = DateTime.UtcNow;


            if (massage == null)
            {
                return;
            }

            if(flag)
            {
                flag = false;
            }

            if (massage.ReplyToMessage != null && (now - massage.ReplyToMessage.Date).TotalHours < 48 )        //видалення цитованих повідомлень
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
                    if (!_subscriberStorage.Contains(massage.Chat.Id))        //додавання чат ID в список користувачів бота
                    {
                        _subscriberStorage.AddSubscriber(massage.Chat.Id);
                        _chatIdStorage.AddChatId(massage.Chat.Id);
                    }
                    await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.Head);
                    return;
                }
                else if (massage.Text == "/stop")
                {
                    Log.Information($"ID[{massage.Chat.Id}]: /stop");
                    if (_subscriberStorage.Contains(massage.Chat.Id))
                    {
                        _subscriberStorage.RemoveSubscriber(massage.Chat.Id);
                        _chatIdStorage.RemoveChatId(massage.Chat.Id);
                    }
                    await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.Stop);
                    return;
                }
                return;
            }
        }

        private async Task CheckAndDisplayMessagesAsync(ITelegramBotClient botClient, CancellationToken stoppingToken)
        {
            string lastMassages = "";
            string currentMassages;

            while (!stoppingToken.IsCancellationRequested)        //бескінечний цикл
            {
                try
                {
                    if (_storage.HasNewMessages())      //перевіряємо на наявність повідомлень
                    {
                        var mailContent = _storage.GetMessage();        //отримуємо повідомлення
                        string fromMail;

                        if (mailContent.Subject.Substring(0, 2) != "RE")
                        {
                            if (mailContent.To.Contains("support@callway.com.ua") || mailContent.Cc.Contains("support@callway.com.ua"))     //перевіряємо, з якої пошти надішло повідомлення
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

                            foreach (var chatId in _subscriberStorage.GetSubscribers())        //виводимо повідомлення користувачам, що підписалися
                            {
                                currentMassages = String.Format("\u267F *Нове повідомлення на пошті*  \n\nНа пошту: {0}  \nТема: {1} \nВід: {2} \nДата: {3} \n\n_Нагадування про необхідність обробити почту, та відповісти на дане повідомлення_", fromMail, mailContent.Subject, mailContent.From, mailContent.Date);
                                if (currentMassages != lastMassages)
                                {
                                    await botClient.SendTextMessageAsync(chatId, currentMassages, ParseMode.Markdown);
                                    lastMassages = currentMassages;
                                }
                            }
                        }

                        //Log.Information("Очікування нового повідомлення");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Information(ex, "An error occurred in the CheckAndDisplayMessagesAsync loop. Restarting the loop.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private Task Exeption(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Log.Error($"An error occurred: {arg2.Message}");
         
            return Task.CompletedTask;
        }

        private void AddChatIdFromFile()
        {
            var loadedChatIds = _chatIdStorage.LoadChatIds();
            foreach (var chatId in loadedChatIds)
            {
                _subscriberStorage.AddSubscriber(chatId); // добавление ID в модель
            }
        }
    }
}
