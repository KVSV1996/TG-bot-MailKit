using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using Serilog;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Info;

namespace TelegramBot
{
    public class IdleClient : IDisposable
    {
        readonly List<IMessageSummary> messages;
        CancellationTokenSource cancel;
        CancellationTokenSource done;
        readonly FetchRequest request;
        readonly ImapClient client;
        bool messagesArrived;
        private bool flagCount = true;
        private int countOfMails;
        private readonly Configuration configuration = new Configuration();
        ITelegramBotClient botClient;
        private Users _user;
        MailStorage _storage;


        public IdleClient(MailStorage storage)
        {
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            client = new ImapClient(new ProtocolLogger(Console.OpenStandardError()));
            request = new FetchRequest(MessageSummaryItems.Full | MessageSummaryItems.UniqueId);
            messages = new List<IMessageSummary>();
            cancel = new CancellationTokenSource();
            

        }

        async Task ReconnectAsync()
        {
            if (!client.IsConnected)
                await client.ConnectAsync(configuration.Provaider, configuration.Port, true, cancel.Token);

            if (!client.IsAuthenticated)
            {
                await client.AuthenticateAsync(configuration.Loging, configuration.Password, cancel.Token);

                await client.Inbox.OpenAsync(FolderAccess.ReadOnly, cancel.Token);
            }
        }

        async Task Mail()
        {
            try
            {

                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly); // Открытие почтового ящика

                if (flagCount)
                {
                    countOfMails = inbox.Count;
                    flagCount = false;
                    //Log.Verbose(inbox.Count + "  " + "false");
                }
                //Console.WriteLine(inbox.Count + " " + countOfMails);
                var message = inbox.GetMessage(inbox.Count - 1);

                //if (!(message == null))
                //{

                ///user.MimeMessage = message;
                //foreach (var chatId in user.subscribers)
                //{
                //    await botClient.SendTextMessageAsync(chatId, String.Format("Subject: {0} \nFrom: {1} \nDate: {2} ", message.Subject, message.From, message.Date));
                //    await Console.Out.WriteLineAsync(chatId.ToString());
                //}
                //await Console.Out.WriteLineAsync("Cc" + message.Cc.ToString());
                //await Console.Out.WriteLineAsync("To" + message.To.ToString());
                //_user.Subject = String.Format("Subject: {0} \nFrom: {1} \nDate: {2} ", message.Subject, message.From, message.Date);


                var emailInfo = new EmailMessageInfo
                {
                    To = message.To.ToString(),
                    Cc = message.Cc.ToString(),
                    Subject = message.Subject,
                    From = message.From.ToString(),
                    Date = message.Date
                };

                _storage.AddMessage(emailInfo);

                Log.Information(String.Format("Subject: {0} \nFrom: {1} \nDate: {2} \nTo: {3} \nCc: {4}", message.Subject, message.From, message.Date, message.To, message.Cc));
                //}               

                return; // Если новых сообщений нет
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                //Log.Error($"An error occurred: {ex.Message}");
                return; // Возвращаем null или соответствующее сообщение об ошибке
            }


        }

        async Task WaitForNewMessagesAsync()
        {
            do
            {
                try
                {
                    if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                    {
                        // Note: IMAP servers are only supposed to drop the connection after 30 minutes, so normally
                        // we'd IDLE for a max of, say, ~29 minutes... but GMail seems to drop idle connections after
                        // about 10 minutes, so we'll only idle for 9 minutes.
                        done = new CancellationTokenSource(new TimeSpan(0, 9, 0));
                        try
                        {
                            await client.IdleAsync(done.Token, cancel.Token);
                        }
                        finally
                        {
                            done.Dispose();
                            done = null;
                        }
                    }
                    else
                    {
                        // Note: we don't want to spam the IMAP server with NOOP commands, so lets wait a minute
                        // between each NOOP command.
                        await Task.Delay(new TimeSpan(0, 1, 0), cancel.Token);
                        await client.NoOpAsync(cancel.Token);
                    }
                    break;
                }
                catch (ImapProtocolException)
                {
                    // protocol exceptions often result in the client getting disconnected
                    await ReconnectAsync();
                }
                catch (IOException)
                {
                    // I/O exceptions always result in the client getting disconnected
                    await ReconnectAsync();
                }
            } while (true);
        }

        async Task IdleAsync()
        {
            do
            {
                try
                {
                    await WaitForNewMessagesAsync();

                    if (messagesArrived)
                    {
                        await Mail();
                        messagesArrived = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            } while (!cancel.IsCancellationRequested);
        }

        public async Task RunAsync()
        {
            // connect to the IMAP server and get our initial list of messages
            try
            {
                await ReconnectAsync();
                //await FetchMessageSummariesAsync(false);
            }
            catch (OperationCanceledException)
            {
                await client.DisconnectAsync(true);
                return;
            }

            // Note: We capture client.Inbox here because cancelling IdleAsync() *may* require
            // disconnecting the IMAP client connection, and, if it does, the `client.Inbox`
            // property will no longer be accessible which means we won't be able to disconnect
            // our event handlers.
            var inbox = client.Inbox;

            // keep track of changes to the number of messages in the folder (this is how we'll tell if new messages have arrived).
            inbox.CountChanged += OnCountChanged;

            // keep track of messages being expunged so that when the CountChanged event fires, we can tell if it's
            // because new messages have arrived vs messages being removed (or some combination of the two).


            // keep track of flag changes


            await IdleAsync();

            inbox.CountChanged -= OnCountChanged;

            await client.DisconnectAsync(true);
        }

        // Note: the CountChanged event will fire when new messages arrive in the folder and/or when messages are expunged.
        void OnCountChanged(object sender, EventArgs e)
        {
            var folder = (ImapFolder)sender;

            // Note: because we are keeping track of the MessageExpunged event and updating our
            // 'messages' list, we know that if we get a CountChanged event and folder.Count is
            // larger than messages.Count, then it means that new messages have arrived.
            if (folder.Count > messages.Count)
            {
                int arrived = folder.Count - messages.Count;

                if (arrived > 1)
                    Console.WriteLine("\t{0} new messages have arrived.", arrived);
                else
                    Console.WriteLine("\t1 new message has arrived.");

                // Note: your first instinct may be to fetch these new messages now, but you cannot do
                // that in this event handler (the ImapFolder is not re-entrant).
                //
                // Instead, cancel the `done` token and update our state so that we know new messages
                // have arrived. We'll fetch the summaries for these new messages later...
                messagesArrived = true;
                done?.Cancel();
            }
        }

        public void Exit()
        {
            cancel.Cancel();
        }

        public void Dispose()
        {
            client.Dispose();
            cancel.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}