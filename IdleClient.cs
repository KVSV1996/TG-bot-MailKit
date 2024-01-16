using MailKit;
using MailKit.Net.Imap;
using Serilog;
using TelegramBot.Info;

namespace TelegramBot
{
    public class IdleClient : IDisposable
    {
        private readonly List<IMessageSummary> messages;
        private CancellationTokenSource cancel;
        private CancellationTokenSource done;
        private readonly ImapClient client;
        private bool messagesArrived;        
        private readonly Configuration configuration = new Configuration();
        private readonly IMailStorage _storage;

        public IdleClient(IMailStorage storage)
        {
            this._storage = storage ?? throw new ArgumentNullException(nameof(storage));
            client = new ImapClient(new ProtocolLogger(Console.OpenStandardError()));            
            messages = new List<IMessageSummary>();
            cancel = new CancellationTokenSource();            

        }

        private async Task ReconnectAsync()
        {
            if (!client.IsConnected)
                await client.ConnectAsync(configuration.Provaider, configuration.Port, true, cancel.Token);

            if (!client.IsAuthenticated)
            {
                await client.AuthenticateAsync(configuration.Loging, configuration.Password, cancel.Token);

                await client.Inbox.OpenAsync(FolderAccess.ReadOnly, cancel.Token);
            }
        }

        private async Task Mail()
        {
            try
            {
                var inbox = client.Inbox;
                inbox.Open(FolderAccess.ReadOnly);                 
                
                var message = inbox.GetMessage(inbox.Count - 1);              

                var emailInfo = new EmailMessage
                {
                    To = message.To.ToString(),
                    Cc = message.Cc.ToString(),
                    Subject = message.Subject,
                    From = message.From.ToString(),
                    Date = message.Date
                };

                _storage.AddMessage(emailInfo);

                Log.Information(String.Format("Subject: {0} \nFrom: {1} \nDate: {2} \nTo: {3} \nCc: {4}", message.Subject, message.From, message.Date, message.To, message.Cc));                              

                return; 
            }
            catch (Exception ex)
            {                
                Log.Error($"An error occurred: {ex.Message}");
                return; 
            }
        }

        private async Task WaitForNewMessagesAsync()
        {
            do
            {
                try
                {
                    if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                    {                        
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
                        await Task.Delay(new TimeSpan(0, 1, 0), cancel.Token);
                        await client.NoOpAsync(cancel.Token);
                    }
                    break;
                }
                catch (ImapProtocolException)
                {                    
                    await ReconnectAsync();
                }
                catch (IOException)
                {                    
                    await ReconnectAsync();
                }
            } while (true);
        }

        private async Task IdleAsync()
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
            try
            {
                await ReconnectAsync();                
            }
            catch (OperationCanceledException)
            {
                await client.DisconnectAsync(true);
                return;
            }
            
            var inbox = client.Inbox;
            
            inbox.CountChanged += OnCountChanged;            

            await IdleAsync();

            inbox.CountChanged -= OnCountChanged;

            await client.DisconnectAsync(true);
        }

        private void OnCountChanged(object sender, EventArgs e)
        {
            var folder = (ImapFolder)sender;
            
            if (folder.Count > messages.Count)
            {
                int arrived = folder.Count - messages.Count;

                if (arrived > 1)
                    Log.Information("\t{0} new messages have arrived.", arrived);
                else
                    Log.Information("\t1 new message has arrived.");
                
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