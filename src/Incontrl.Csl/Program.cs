using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Incontrl.Net;
using Incontrl.Net.Models;
using Incontrl.Net.Types;
using Incontrl.Provider;
using Microsoft.Extensions.Configuration;

namespace Incontrl.Console
{
    class Program
    {
        public static async Task Main(string[] args)
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
            api.LoginAsync(true).Wait();

            //cool ... ensure subscription here ...
            subscriptionGuid = await EnsureSubscriptionData(subscriptionGuid, api);

            var bankAccounts = api.Subscription(subscriptionGuid).BankAccounts().ListAsync().Result;
            BankProviderFactory factory = new BankProviderFactory();
            foreach (var bankAccount in bankAccounts.Items) {
                IBankProvider provider = factory.Get(bankAccount.Provider.Name, bankAccount.Provider.Settings);
                var transactions = await provider.GetTransactionsAsync(new BankTransactionSearchDocument());
            }

            //i. get bank accounts by subscriptionId
            //ii. create provider concrete class through BankProviderFactory
            //iii. get bank transactions by bank account
            //iv. store bank transactions to our storage
            //v. get invoices by subscriptionId
            //vi. matching .....
        }

        public static async Task<Guid> EnsureSubscriptionData(Guid subscriptionGuid, IncontrlApi api) {
            // i. create subscription, deserialize subscription from disk
            Subscription subscription = null;
            if (Guid.Empty.Equals(subscriptionGuid)) {
                var subscriptionJson = File.ReadAllText(@"Resources\create_subscription.json");
                var subscriptionRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateSubscriptionRequest>(subscriptionJson);
                subscription = await api.Subscriptions().CreateAsync(subscriptionRequest);
            } else {
                subscription = await api.Subscription(subscriptionGuid).GetAsync();
            }
            var subscriptionApi = api.Subscription(subscription.Id.Value);

            // ii. ensure bank account for the subscription
            var bankAccounts = await subscriptionApi.BankAccounts().ListAsync();
            if (null == bankAccounts || 0 == bankAccounts.Count) {
                var bankAccount = await subscriptionApi.BankAccounts().CreateAsync(new BankAccount {
                    Bank = "NBG", Baseline = new Balance { Amount = 1000, Date = DateTime.Now }, Code = "1234", Name = "Main Bank Account", Number = "edw tha paei to iban",
                    Provider = new TransactionProviderConfig {
                        Name = "nbg",
                        Settings = new {
                            provider = "NBG", provider_id = "NBG.gr",
                            user_id = "9d7f2ef4-7262-4429-a487-7979e4a76447",
                            username = "User1", sandbox_id = "inContrl",
                            application_id = "inContrl", account_id = "2cebef85-bf5b-4d1d-8727-d4441750d21d",
                            bank_id = "DB173089-A8FE-43F1-8947-F1B2A8699829",
                            nbgApiUrl = "https://apis.nbg.gr/public/nbgapis/obp/v3.0.1"
                        }
                    }
                });
            }
            // iii. ensure invoices in subscription
            var product = await subscriptionApi.Products().CreateAsync(new CreateProductRequest {
                Amount = 450,
                Name = "My Precious",
                Taxes = new List<Tax> {
                    new Tax { Name = "VAT", Rate = 0.24M }
                }
            });
            await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                CurrencyCode = "EUR",
                Date = DateTime.Now.AddHours(-2),
                Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item", DiscountRate = 0.5, UnitAmount = 450, Product = product }
                }
            });
            await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                CurrencyCode = "EUR",
                Date = DateTime.Now.AddHours(-1),
                Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item 2", DiscountRate = 0.5, UnitAmount = 450, Product = product, Quantity = 2 }
                }
            });
            var incoive = await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                CurrencyCode = "EUR",
                Date = DateTime.Now,
                Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item 3", DiscountRate = 0.5, UnitAmount = 450, Product = product, Quantity = 0.5 }
                }
            });


            var invoices = await subscriptionApi.Invoices().ListAsync();

            return subscription.Id.Value;
        }
    }
}
