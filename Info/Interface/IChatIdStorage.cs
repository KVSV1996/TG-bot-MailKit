using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Info.Interface
{
    public interface IChatIdStorage
    {
        void AddChatId(long chatId);
        void RemoveChatId(long chatId);
        List<long> LoadChatIds();
    }
}
