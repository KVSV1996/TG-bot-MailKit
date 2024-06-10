using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBot.Info;
using Topshelf;
using System.Reflection;
using TelegramBot.Info.Interface;
using Microsoft.Extensions.Hosting;
using Topshelf.Builders;


namespace TelegramBot
{
    public class Program
    {
        public static void Main()
        {
            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
            string chatIdsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "chatIds.txt");
            Configuration configuration = new(configPath);

            var serviceProvider = new ServiceCollection()
            .AddSingleton<IChatIdStorage>(provider => new ChatIdStorage(chatIdsPath))
            .AddSingleton<Configuration>(new Configuration(configPath))
            .AddSingleton<ITelegramBotClient>(t => new TelegramBotClient(configuration.Token))
            .AddSingleton<IMailStorage, MailStorage>()
            .AddSingleton<ISubscriberStorage, SubscriberStorage>()
            .AddSingleton<ProgramManager>()

            .BuildServiceProvider();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs\\log.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();


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
