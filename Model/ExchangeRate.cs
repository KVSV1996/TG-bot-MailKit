using Newtonsoft.Json;

namespace TelegramBot
{
    public class ExchangeRate
    {
        [JsonProperty("baseCurrency")]
        public string BaseCurrency { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("saleRateNB")]
        public double SaleRateNB { get; set; }

        [JsonProperty("purchaseRateNB")]
        public double PurchaseRateNB { get; set; }

        [JsonProperty("saleRate")]
        public double? SaleRate { get; set; }

        [JsonProperty("purchaseRate")]
        public double? PurchaseRate { get; set; }

    }
}
