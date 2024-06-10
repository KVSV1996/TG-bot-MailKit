using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Info.Interface;

namespace TelegramBot.Info
{
    public class ChatIdStorage : IChatIdStorage
    {
        private readonly string _filePath;

        public ChatIdStorage(string filePath)
        {
            _filePath = filePath;
        }

        public void AddChatId(long chatId)
        {
            using (StreamWriter sw = File.AppendText(_filePath))
            {
                sw.WriteLine(chatId);
            }
        }

        public void RemoveChatId(long chatId)
        {
            var chatIds = File.ReadAllLines(_filePath).ToList();
            chatIds.Remove(chatId.ToString());
            File.WriteAllLines(_filePath, chatIds);
        }

        public List<long> LoadChatIds()
        {
            if (File.Exists(_filePath))
            {
                var chatIds = File.ReadAllLines(_filePath).Select(id => long.Parse(id)).ToList();
                return chatIds;
            }
            return new List<long>();
        }
    }
}
