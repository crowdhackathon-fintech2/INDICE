using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Incontrl.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Incontrl.Provider.Concrete
{
    public class NbgBankProvider : IBankProvider {
        private string application_id = "in_contrl";
        private string provider;
        private string provider_id;
        private string sandbox_id;
        private string user_id;
        private string username;
        private string nbgApiUrl;
        private string bank_id;
        private string account_id;
        private HttpClient _http;
        private readonly JsonSerializerSettings _JsonSettings = new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public NbgBankProvider(dynamic settings) {
            if(settings.provider != null) {
                provider = settings.provider;
            }
            if (settings.provider_id != null) {
                provider_id = settings.provider_id;
            }
            if (settings.sandbox_id != null) {
                sandbox_id = settings.sandbox_id;
            }
            if (settings.user_id != null) {
                user_id = settings.user_id;
            }
            if (settings.username != null) {
                username = settings.username;
            }
            if (settings.nbgApiUrl != null) {
                nbgApiUrl = settings.nbgApiUrl;
            }
            if (settings.bank_id != null) {
                bank_id = settings.bank_id;
            }
            if (settings.account_id != null) {
                account_id = settings.account_id;
            }
            application_id = settings.application_id;

            _http = new HttpClient();
            _http.BaseAddress = new Uri($"{nbgApiUrl}/my/banks/{bank_id}/");
            _http.DefaultRequestHeaders.Add("provider", provider);
            _http.DefaultRequestHeaders.Add("provider_id", provider_id);
            _http.DefaultRequestHeaders.Add("username", username);
            _http.DefaultRequestHeaders.Add("user_id", user_id);
            _http.DefaultRequestHeaders.Add("application_id", application_id);
            _http.DefaultRequestHeaders.Add("sandbox_id", sandbox_id);
            _http.DefaultRequestHeaders.Add("accept", "text/json");
        }

        public async Task<IEnumerable<BankTransaction>> GetTransactionsAsync(BankTransactionSearchDocument searchDoc) {           
            var response = await _http.GetAsync($"accounts/{account_id}/transactions");
            string content = await response.Content.ReadAsStringAsync();
            var transactions = JsonConvert.DeserializeObject<IEnumerable<Transaction>>(content);
            return MapToBankTransactions(transactions);
        }

        protected List<BankTransaction> MapToBankTransactions(IEnumerable<Transaction> transactions) {
            List<BankTransaction> bankTransactions = new List<BankTransaction>();
            foreach(Transaction transaction in transactions) {
                BankTransaction bankTransaction = new BankTransaction();
                bankTransaction.Amount = Math.Abs(transaction.Details.Value.Amount);
                bankTransaction.Type = transaction.Details.Value.Amount > 0 ? BankTransactionType.Credit : BankTransactionType.Debit;
                bankTransaction.Date = transaction.Details.Completed;
                bankTransaction.Number = transaction.Id;
                bankTransaction.Hash = Guid.Parse(transaction.Id).ToByteArray();
                bankTransaction.Text = transaction.Details.Description;
                bankTransactions.Add(bankTransaction);                
            }
            return bankTransactions;
        }
    }
}
