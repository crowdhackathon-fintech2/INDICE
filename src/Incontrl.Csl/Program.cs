using System;
using System.IO;
using System.Threading.Tasks;
using Incontrl.Net;
using Incontrl.Net.Models;
using Incontrl.Net.Types;
using Microsoft.Extensions.Configuration;

namespace Incontrl.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();

            var configuration = builder.Build();

            var clientId = configuration["Client:ClientId"];
            var clientSecret = configuration["Client:ClientSecret"];
            var baseApiAddress = configuration["BaseApiAddress"]; //http://api-vnext.incontrl.io
            var subscriptionId = configuration["SubscriptionId"];
            Guid subscriptionGuid = string.IsNullOrWhiteSpace(subscriptionId) ? Guid.Empty : new Guid(subscriptionId);
            var api = new IncontrlApi(clientId, clientSecret);
            api.Configure(baseApiAddress);
            api.LoginAsync().Wait();

            //cool ... ensure subscription here ...
            EnsureSubscriptionData(subscriptionGuid, api);

            //i. get bank accounts by subscriptionId
            //ii. create provider concrete class through BankProviderFactory
            //iii. get bank transactions by bank account
            //iv. store bank transactions to our storage
            //v. get invoices by subscriptionId
            //vi. matching .....
        }

        public static void EnsureSubscriptionData(Guid subscriptionGuid, IncontrlApi api) {
            // i. create subscription, deserialize subscription from disk
            Subscription subscription = null;
            if (Guid.Empty.Equals(subscriptionGuid)) {
                var subscriptionJson = File.ReadAllText(@"Resources\create_subscription.json");
                var subscriptionRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<Net.Models.CreateSubscriptionRequest>(subscriptionJson);
                subscription = api.Subscriptions().CreateAsync(subscriptionRequest).Result;
            } else {
                subscription = api.Subscription(subscriptionGuid).GetAsync().Result;
            }
            // ii. ensure bank account for the subscription
            ResultSet<BankAccount> bankAccounts = api.Subscription(subscription.Id.Value).BankAccounts().ListAsync().Result;
            if (0 == bankAccounts.Count) {
                var bankAccount = api.Subscription(subscription.Id.Value).BankAccounts().CreateAsync(new BankAccount {
                    Bank = "NBG", Baseline = new Balance { Amount = 1000, Date = DateTime.Now }, Code = "1234", Name = "Main Bank Account", Number = "edw tha paei to iban",
                    Provider = new TransactionProviderConfig {
                        Name = "baseProvider",
                        Settings = new { }
                    }
                }).Result;
            }
            // iii. ensure invoices in subscription
        }
    }
}
