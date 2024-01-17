using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Telegram.Bot;
using MailKit.Net.Imap;
using TelegramBot.Info;

namespace TelegramBot
{
    public class Program
    {        
        public static void Main()
        {            
            Configuration configuration = new ();
            var serviceProvider = new ServiceCollection()             
            .AddSingleton<ITelegramBotClient>(t => new TelegramBotClient(configuration.Token))            
            .AddSingleton<ICommunication, ConsoleCommunication>()
            .AddSingleton<IMailStorage, MailStorage>()            
            .AddSingleton<ProgramManager>()
            .BuildServiceProvider();

            var manager = serviceProvider.GetRequiredService<ProgramManager>();

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            manager.InitialiseBot();            

            Log.CloseAndFlush();
        }
    }
}
