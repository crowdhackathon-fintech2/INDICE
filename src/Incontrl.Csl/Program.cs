using System;
using System.IO;
using Incontrl.Net;
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
            var api = new IncontrlApi(clientId, clientSecret);
            api.Configure(baseApiAddress);
            //api.LoginAsync()

            // i. create or get subscription
            // ii. ensure bank account for the subscription
            // iii. ensure invoices in subscription

            //i. get bank accounts by subscriptionId
            //ii. create provider concrete class through BankProviderFactory
            //iii. get bank transactions by bank account
            //iv. store bank transactions to our storage
            //v. get invoices by subscriptionId
            //vi. matching .....
        }
    }
}
