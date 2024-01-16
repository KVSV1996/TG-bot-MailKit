namespace TelegramBot.Info
{
    public class EmailMessage
    {
        public string? To { get; set; }
        public string? Cc { get; set; }
        public string? Subject { get; set; }
        public string? From { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
