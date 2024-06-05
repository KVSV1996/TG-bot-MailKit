using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Info.Interface
{
    public interface ISubscriberStorage
    {
        public void AddSubscriber(long chatId);
        public void RemoveSubscriber(long chatId);
        public bool Contains(long chatId);
        public IEnumerable<long> GetSubscribers();
    }
}
