using Newtonsoft.Json;

namespace TelegramBot
{
    public class Header
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("bank")]
        public string Bank { get; set; }

        [JsonProperty("baseCurrency")]
        public int BaseCurrency { get; set; }

        [JsonProperty("baseCurrencyLit")]
        public string BaseCurrencyLit { get; set; }

        [JsonProperty("ExchangeRate")]
        public IList<ExchangeRate> ExchangeRate { get; set; }
    }
}
