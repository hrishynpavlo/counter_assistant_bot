using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CounterAssistant.Bot.Payments
{
    public class LiqPayRequest
    {
        private LiqPayRequest(string publicKey, string privateKey, ActionType action, decimal amount, LiqpayCurrency currency, string description, Guid orderId, DateTime subscribeStartDate, SubscribePeriodicity subscribePeriodicity)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            Action = action;
            Amount = amount;
            Currency = currency;
            Description = description;
            OrderId = orderId;
            SubscribeStartDate = subscribeStartDate;
            SubscribePeriodicity = subscribePeriodicity;
        }

        [JsonProperty("version")]
        public string Version => "3";

        [JsonProperty("public_key")]
        public string PublicKey { get; }

        [JsonProperty("private_key")]
        public string PrivateKey { get; }

        [JsonProperty("action")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; }

        [JsonProperty("amount")]
        public decimal Amount { get; }

        [JsonProperty("currency")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LiqpayCurrency Currency { get; }

        [JsonProperty("description")]
        public string Description { get; }

        [JsonProperty("order_id")]
        public Guid OrderId { get; }

        [JsonProperty("subscribe_date_start")]
        public DateTime SubscribeStartDate { get; }
        
        [JsonProperty("subscribe_periodicity")]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public SubscribePeriodicity SubscribePeriodicity { get; }

        public static LiqPayRequest CreateMonthlySubscription(string publicKey, string privateKey, decimal amount, string description) =>
            new LiqPayRequest(publicKey, privateKey, ActionType.Subscribe, amount, LiqpayCurrency.UAH, description, Guid.NewGuid(), DateTime.UtcNow, SubscribePeriodicity.Month);

        public string ToJson() => JsonConvert.SerializeObject(this, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Utc, DateFormatString = "yyyy-MM-dd HH:mm:ss" });

        public string ToData() => Convert.ToBase64String(Encoding.UTF8.GetBytes(ToJson()));

        public string ToSignature(string data) => Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(PrivateKey+data+PrivateKey)));
    }

    public enum ActionType
    {
        Pay,
        Hold,
        Subscribe,
        Auth
    }

    public enum LiqpayCurrency
    {
        USD,
        EUR,
        RUB,
        UAH
    }

    public enum SubscribePeriodicity
    {
        Month,
        Year
    }
}
