using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Incontrl.Net;
using Incontrl.Net.Models;
using Incontrl.Net.Types;
using Incontrl.Provider;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Incontrl
{
    class Program
    {
        public static async Task Main(string[] args) {
            var builder = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();

            var configuration = builder.Build();
            var clientId = configuration["Client:ClientId"];
            var clientSecret = configuration["Client:ClientSecret"];
            var baseApiAddress = configuration["BaseApiAddress"]; //http://api-vnext.incontrl.io
            var subscriptionId = configuration["SubscriptionId"];
            var subscriptionGuid = string.IsNullOrWhiteSpace(subscriptionId) ? Guid.Empty : new Guid(subscriptionId);
            var api = new IncontrlApi(clientId, clientSecret);
            api.Configure(baseApiAddress);
            api.LoginAsync(true).Wait();

            //cool ... ensure subscription here ...
            subscriptionGuid = await EnsureSubscriptionData(subscriptionGuid, api);
            //var subscriptionApi = api.Subscription(subscriptionGuid);

            var bankAccounts = api.Subscription(subscriptionGuid).BankAccounts().ListAsync().Result;
            var factory = new BankProviderFactory();
            foreach (var bankAccount in bankAccounts.Items) {
                IBankProvider provider = factory.Get(bankAccount.Provider.Name, bankAccount.Provider.Settings);
                var transactions = await provider.GetTransactionsAsync(new BankTransactionSearchDocument());
                // i. save transactions to storage
                var savedTransactions = new List<BankTransaction>();
                foreach (var transaction in transactions) {
                    var savedTrans = await api.Subscription(subscriptionGuid).BankAccount(bankAccount.Id.Value).Transactions().CreateAsync(transaction);

                    // ii. get active invoices
                    var pendingInvoices = await api.Subscription(subscriptionGuid).Invoices().ListAsync(new ListOptions<InvoiceListFilter> { Filter = new InvoiceListFilter { Status = InvoiceStatus.Issued } });

                    foreach (var invoice in pendingInvoices.Items.ToList()) {
                        // iii. try to match (exact match please ...) invoices & transactions -> invoice.PaymentCode = transaction.Description

                        if (invoice.PaymentCode.Trim().Equals(savedTrans.Text.Trim())) {
                            // a. add payments here ..
                            var payment = await api.Subscription(subscriptionGuid).BankAccount(bankAccount.Id.Value).Transaction(savedTrans.Id.Value).Payments().CreateAsync(new Payment { InvoiceId = invoice.Id.Value, Amount = savedTrans.Amount });

                            if (invoice.TotalPayable.Value == savedTrans.Amount) {
                                // b. update the status now
                                InvoiceStatus invoiceStatus = await api.Subscription(subscriptionGuid).Invoice(invoice.Id.Value).Status().UpdateAsync(InvoiceStatus.Paid);
                                var url = $"http://api-vnext.incontrl.io/{invoice.PermaLink}";
                                OpenBrowser(url);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"We found a match !!! {payment.Amount:N2}");
                            }
                        }
                    }

                }

                //i. get bank accounts by subscriptionId
                //ii. create provider concrete class through BankProviderFactory
                //iii. get bank transactions by bank account
                //iv. store bank transactions to our storage
                //v. get invoices by subscriptionId
                //vi. matching .....

                Console.ReadKey();
            }
        }

        public static async Task<Guid> EnsureSubscriptionData(Guid subscriptionGuid, IncontrlApi api) {
            // i. create subscription, deserialize subscription from disk
            Subscription subscription = null;
            if (Guid.Empty.Equals(subscriptionGuid)) {
                var subscriptionJson = File.ReadAllText(@"Resources\create_subscription.json");
                var subscriptionRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateSubscriptionRequest>(subscriptionJson);
                subscriptionRequest.Company.LegalName += DateTime.Now.Second;
                subscriptionRequest.Company.Name += DateTime.Now.Second;
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

            var existingInvoices = await subscriptionApi.Invoices().ListAsync(new ListOptions<InvoiceListFilter> { Size = 3, Sort = "Date-" });
            if (existingInvoices.Count == 0) {
                var product = await subscriptionApi.Products().CreateAsync(new CreateProductRequest {
                    Amount = 450,
                    Name = "My Precious",
                    Taxes = new List<Tax> {
                    new Tax { Name = "VAT", Rate = 0.24M, IsSalesTax = true }
                }
                });
                var company = await subscriptionApi.Organisations().CreateAsync(new CreateOrganisationRequest {
                    Email = "support@indice.gr",
                    LegalName = "INDICE OE",
                    Name = "Indice",
                    LineOfBusiness = "Independent Software Vendor",
                    TaxCode = "GR99",
                    TaxOffice = "ΣΤ' ΑΘΗΝΩΝ",
                    Website = "http://www.indice.gr",
                    Address = new Address {
                        CountryCode = "GR",
                        Line1 = "22 Iakchou str.",
                        City = "Athens",
                        ZipCode = "11854"
                    }
                });
                await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                    PaymentCode = "171021000001",
                    CurrencyCode = "EUR",
                    Date = DateTime.Now.AddSeconds(-4),
                    Status = InvoiceStatus.Issued,
                    Recipient = new Recipient { Organisation = company },
                    Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item", DiscountRate = 0.05, UnitAmount = 450, Product = product, Taxes = product.Taxes }
                }
                });
                await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                    PaymentCode = "171021000001",
                    CurrencyCode = "EUR",
                    Date = DateTime.Now.AddSeconds(-2),
                    Status = InvoiceStatus.Issued,
                    Recipient = new Recipient { Organisation = company },
                    Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item 2", DiscountRate = 0.05, UnitAmount = 450, Product = product, Taxes = product.Taxes, Quantity = 2 }
                }
                });
                var incoive = await subscriptionApi.Invoices().CreateAsync(new CreateInvoiceRequest {
                    PaymentCode = "171021000001",
                    CurrencyCode = "EUR",
                    Date = DateTime.Now,
                    Status = InvoiceStatus.Issued,
                    Recipient = new Recipient { Organisation = company },
                    Lines = new List<InvoiceLine> {
                    new InvoiceLine { Description = "This is a Nice expensive item 3", DiscountRate = 0.05, UnitAmount = 450, Product = product, Taxes = product.Taxes, Quantity = 0.5 }
                }
                });

                var invoices = await subscriptionApi.Invoices().ListAsync(new ListOptions<InvoiceListFilter> { Size = 3, Sort = "Date-" });
                foreach (var invoice in invoices.Items) {
                    var url = $"{api.ApiAddress}/{invoice.PermaLink}";
                    OpenBrowser(url);
                }
            }

            return subscription.Id.Value;
        }


        public static void OpenBrowser(string url) {
            try {
                Process.Start(url);
            } catch {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                } else {
                    throw;
                }
            }
        }
    }
}
