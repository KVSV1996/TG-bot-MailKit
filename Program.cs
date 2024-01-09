using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Telegram.Bot;
using MailKit.Net.Imap;

namespace TelegramBot
{
    public class Program
    {        
        public static void Main(string[] args)
        {
            
            Configuration configuration = new ();
            var serviceProvider = new ServiceCollection()             
            .AddSingleton<ITelegramBotClient>(t => new TelegramBotClient(configuration.Token))            
            .AddSingleton<ICommunication, ConsoleCommunication>()
            .AddSingleton<ImapClient>()
            .AddSingleton<ProgramManager>()
            .BuildServiceProvider();

            var manager = serviceProvider.GetRequiredService<ProgramManager>();

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            manager.InitialiseBot();            

            Log.CloseAndFlush();
        }
    }
}
