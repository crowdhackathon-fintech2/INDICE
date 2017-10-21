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
            api.Configure("http://api-vnext.incontrl.io");
            api.LoginAsync()
        }
    }
}
