
using Microsoft.Extensions.Configuration;

namespace TelegramBot
{
    public class Configuration
    {
        public Configuration()
        {
            var builder = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            Token = config.GetSection("Token").Get<string>();
            Provaider = config.GetSection("Provaider").Get<string>();
            Port = int.Parse(config.GetSection("Port").Get<string>());
            Loging = config.GetSection("Loging").Get<string>();
            Password = config.GetSection("Password").Get<string>();

        }

        public string Token { get; set; }
        public string Provaider { get; set; }
        public int Port { get; set; }
        public string Loging { get; set; }
        public string Password { get; set; }
        
    }
}
