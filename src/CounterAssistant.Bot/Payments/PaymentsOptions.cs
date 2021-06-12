namespace CounterAssistant.Bot.Payments
{
    public class PaymentsOptions
    {
        public decimal MontlySubscriptionPrice { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}
