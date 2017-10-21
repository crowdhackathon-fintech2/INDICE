using System;
using System.Collections.Generic;
using System.Text;
using Incontrl.Net.Models;

namespace Incontrl.Provider
{
    public interface IBankProvider
    {
        IEnumerable<BankTransaction> GetTransactions(BankTransactionSearchDocument searchDoc);
    }
}
