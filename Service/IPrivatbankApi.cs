using Refit;

namespace TelegramBot
{
    public interface IPrivatbankApi
    {
        [Get("/p24api/exchange_rates?date={date}")]
        Task<Header> GetApiData(string date);        
    }
}
