using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CounterAssistant.Bot.Payments
{
    public interface IPaymentService
    {
        Task<string> GetMonthlySubscriptionUrl();
    }

    [ExcludeFromCodeCoverage]
    public class LiqpayService : IPaymentService
    {
        private readonly static Uri PaymentBaseUrl = new Uri("https://www.liqpay.ua");

        private readonly HttpClient _http;
        private readonly PaymentsOptions _options;

        public LiqpayService(PaymentsOptions options)
        {
            _options = options;

            var notRedirectHandler = new HttpClientHandler { AllowAutoRedirect = false };
            _http = new HttpClient(notRedirectHandler) 
            { 
                BaseAddress = PaymentBaseUrl, 
                Timeout = TimeSpan.FromSeconds(5) 
            };
        }

        public async Task<string> GetMonthlySubscriptionUrl()
        {
            var request = LiqPayRequest.CreateMonthlySubscription(_options.PublicKey, _options.PrivateKey, _options.MontlySubscriptionPrice, "Ежемесячная подписка на бота");
            var data = request.ToData();
            var signature = request.ToSignature(data);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            { 
                [nameof(data)] = data, 
                [nameof(signature)] = signature
            });

            var response = await _http.PostAsync("/api/3/checkout", body);
            if(response.StatusCode != HttpStatusCode.Found)
            {
                throw new ApplicationException("UNSUPPORTED STATUS CODE IN PAYMENT");
            }

            return response.Headers.Location.OriginalString;
        }
    }
}
