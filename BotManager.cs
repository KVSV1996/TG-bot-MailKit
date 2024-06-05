using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using TelegramBot.Info;
using TelegramBot.Info.Interface;

namespace TelegramBot
{
    public class BotManager : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IMailStorage _storage;
        private readonly ISubscriberStorage _subscriberStorage;
        public BotManager(ITelegramBotClient botClient, IMailStorage storage, ISubscriberStorage subscriberStorage) 
        {
            _botClient = botClient;
            _storage = storage;
            _subscriberStorage = subscriberStorage;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => CheckAndDisplayMessagesAsync(_botClient, stoppingToken), stoppingToken);
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

                    Log.Information("Очікування нового повідомлення");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
