using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Telegram.Bot;
using MailKit.Net.Imap;
using TelegramBot.Info;
using Topshelf;
using System.Reflection;


namespace TelegramBot
{
    public class Program
    {        
        public static void Main()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");            
            Configuration configuration = new (configPath);
            var serviceProvider = new ServiceCollection()
            .AddSingleton<Configuration>(new Configuration(configPath))
            .AddSingleton<ITelegramBotClient>(t => new TelegramBotClient(configuration.Token))                   
            .AddSingleton<IMailStorage, MailStorage>()            
            .AddSingleton<ProgramManager>()
            .BuildServiceProvider();
            

            HostFactory.Run(x =>
            {
                x.Service<ProgramManager>(s =>
                {                    
                    s.ConstructUsing(name => serviceProvider.GetRequiredService<ProgramManager>());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Телеграм бот для для работы с почтой");
                x.SetDisplayName("TGBotMail");
                x.SetServiceName("TGBotMail");
            });
            
        }
    }
}
