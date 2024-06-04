using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Info;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace TelegramBot
{
    public class ProgramManager : BackgroundService
    {        
        private readonly ITelegramBotClient _botClient;
        private readonly IdleClient _imapIdle;
        private List<long> _subscribers;
        private bool flag = true;
        private readonly IMailStorage _storage;
        private readonly Configuration _configuration;



        public ProgramManager( ITelegramBotClient botClient, IMailStorage storage, Configuration configuration)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _subscribers = new List<long>();
            _storage = new MailStorage();
            _imapIdle = new IdleClient(_storage, _configuration);
           
        }

        public void Start()
        {          
            _botClient.StartReceiving(UpdateAsync, Exeption);       //запускаємо бота
            var idleTask = _imapIdle.RunAsync();                //запускаємо поштовик

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
                    if (!_subscribers.Contains(massage.Chat.Id))        //додавання чат ID в список користувачів бота
                    {
                        _subscribers.Add(massage.Chat.Id);
                    }
                    await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.Head);
                    return;
                }
                else if (massage.Text == "/stop")
                {
                    Log.Information($"ID[{massage.Chat.Id}]: /stop");
                    if (_subscribers.Contains(massage.Chat.Id))
                    {
                        _subscribers.Remove(massage.Chat.Id);
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
                if (_storage.HasNewMessages())      //перевіряємо на наявність повідомлень
                {
                    var mailContent = _storage.GetMessage();        //отримуємо повідомлення
                    string fromMail;

                    if (mailContent.Subject.Substring(0,2) != "RE")
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

                        foreach (var chatId in _subscribers)        //виводимо повідомлення користувачам, що підписалися
                        {
                            currentMassages = String.Format("\u267F *Нове повідомлення на пошті*  \n\nНа пошту: {0}  \nТема: {1} \nВід: {2} \nДата: {3} \n\n_Нагадування про необхідність обробити почту, та відповісти на дане повідомлення_", fromMail, mailContent.Subject, mailContent.From, mailContent.Date);
                            if(currentMassages != lastMassages)
                            {
                                await botClient.SendTextMessageAsync(chatId, currentMassages, ParseMode.Markdown);
                                lastMassages = currentMassages;
                            }
                        }
                    }

                    Log.Information("Очікування нового повідомлення");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private Task Exeption(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Log.Error($"An error occurred: {arg2.Message}");
         
            return Task.CompletedTask;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => CheckAndDisplayMessagesAsync(_botClient, stoppingToken), stoppingToken);
        }
    }
}
