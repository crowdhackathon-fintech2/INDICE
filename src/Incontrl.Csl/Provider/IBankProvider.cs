using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Incontrl.Net.Models;

namespace Incontrl.Provider
{
    public interface IBankProvider
    {
        Task<IEnumerable<BankTransaction>> GetTransactionsAsync(BankTransactionSearchDocument searchDoc);
    }
}
