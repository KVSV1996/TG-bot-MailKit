using MailKit;
using MailKit.Net.Imap;
using Serilog;
using System;
using System.Drawing;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot
{
    public class ProgramManager
    {        
        private readonly ICommunication _communication;
        private readonly ITelegramBotClient _botClient;
        private readonly ImapClient _client;
        private readonly IdleClient imapIdle = new IdleClient();
        private MailKit _mailKit;
        //private List<long> _subscribers;
        private bool flag = true;
        private Users user = new Users();

        public ProgramManager(ImapClient client, ICommunication communication, ITelegramBotClient botClient)
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            this._communication = communication ?? throw new ArgumentNullException(nameof(communication));
            this._botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            //_subscribers = new List<long>();
        }       

        public void InitialiseBot()
        {                     
            _botClient.StartReceiving(UpdateAsync, Exeption);

            
            var idleTask = imapIdle.RunAsync();

            Task.Run(() => {
                Console.ReadKey(true);
            }).Wait();

            imapIdle.Exit();

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
                //StartMailKitProcessing(botClient);
                flag = false;
            }

            if (massage.ReplyToMessage != null )
            {
                var originalMessageId = massage.ReplyToMessage.MessageId;
                //Console.WriteLine($"ID цитированного сообщения: {originalMessageId}");

                await botClient.DeleteMessageAsync(massage.Chat.Id, massage.ReplyToMessage.MessageId);
                await botClient.DeleteMessageAsync(massage.Chat.Id, massage.MessageId);


                return;
            }

            await Console.Out.WriteLineAsync("ping");

            if (massage.Text != null)
            {
                if (massage.Text == "/start")
                {
                    Log.Information($"ID[{massage.Chat.Id}]: /start");
                    if (!user.subscribers.Contains(massage.Chat.Id))
                    {
                        user.subscribers.Add(massage.Chat.Id);
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
            Task.Run(() => MailKitAsync(botClient));
        }

        private async Task MailKitAsync(ITelegramBotClient botClient)
        {
            _mailKit = new MailKit(_client);

            while (true)
            {
                try
                {
                    if (!_client.IsConnected)
                    {
                        var mailContent = _mailKit.Mail(); // Получаем содержимое почты
                        if (!(mailContent == null))
                        {
                            foreach (var chatId in user.subscribers)
                            {
                                await botClient.SendTextMessageAsync(chatId, String.Format("Subject: {0} \nFrom: {1} \nDate: {2} ", mailContent.Subject, mailContent.From, mailContent.Date));
                                await Console.Out.WriteLineAsync(chatId.ToString());
                            }
                            
                            Log.Information(String.Format("Subject: {0} \nFrom: {1} \nDate: {2} ", mailContent.Subject, mailContent.From, mailContent.Date));
                        }
                    }
                    else
                    {
                        _client.Disconnect(true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Произошла ошибка при работе с почтой: {ex.Message}");
                    // Здесь можно добавить дополнительную логику обработки ошибок, если нужно
                }
                await Task.Delay(1000); // Асинхронная задержка
            }
        }       

        private Task Exeption(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            Log.Error($"An error occurred: {arg2.Message}");

            //throw new NotImplementedException();
            return Task.CompletedTask;
        }
    }
}
