using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramBot.Info.Interface;

namespace TelegramBot.Info
{
    public class SubscriberStorage : ISubscriberStorage
    {
        private readonly List<long> _subscribers;

        public SubscriberStorage()
        {
            _subscribers = new List<long>();
        }

        public void AddSubscriber(long chatId)
        {
            if (!_subscribers.Contains(chatId))
            {
                _subscribers.Add(chatId);
            }
        }

        public void RemoveSubscriber(long chatId)
        {
            if (_subscribers.Contains(chatId))
            {
                _subscribers.Remove(chatId);
            }
        }
        public bool Contains(long chatId)
        {
            return _subscribers.Contains(chatId);
        }

        public IEnumerable<long> GetSubscribers()
        {
            return _subscribers.AsReadOnly();
        }
    }
}
