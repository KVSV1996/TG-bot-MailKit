﻿using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
            _botClient.StartReceiving(UpdateAsync, Exeption);       //запускаємо бота

            
            var idleTask = _imapIdle.RunAsync();        //запускаємо поштовик

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
                StartMailKitProcessing(botClient);      //запускаємо моніторинг наявності нових листів, для розуміння див. клас IdleClient
                flag = false;
            }

            if (massage.ReplyToMessage != null )        //видалення цитованих повідомлень
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
                        await Console.Out.WriteLineAsync(massage.Chat.Id.ToString());
                    }
                    await botClient.SendTextMessageAsync(massage.Chat.Id, Constants.Head);
                    return;
                }
                return;
            }                      
        }

        private void StartMailKitProcessing(ITelegramBotClient botClient)
        {
            Task.Run(() => CheckAndDisplayMessagesAsync(botClient));
        }

        public async Task CheckAndDisplayMessagesAsync(ITelegramBotClient botClient)
        {
            while (true)        //бескінечний цикл
            {                
                if (_storage.HasNewMessages())      //перевіряємо на наявність повідомлень
                {
                    var mailContent = _storage.GetMessage();        //отримуємо повідомлення
                    string fromMail;

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
                        await botClient.SendTextMessageAsync(chatId, String.Format("\u267F *Нове повідомлення на пошті*  \n\nНа пошту: {0}  \nТема: {1} \nВід: {2} \nДата: {3} \n\n _Нагадування про необхідність обробити почту, та відповісти на дане повідомлення_", fromMail, mailContent.Subject, mailContent.From, mailContent.Date), ParseMode.Markdown);
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
