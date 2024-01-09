namespace TelegramBot
{
    public class ConsoleCommunication : ICommunication
    {
        public string ReadLine()
        {
            return Console.ReadLine();
        }

    }
}
