using System;
using System.Collections.Generic;
using System.Text;
using Incontrl.Net.Models;

namespace Incontrl.Provider.Concrete
{
    public class NbgBankProvider : IBankProvider
    {
        public NbgBankProvider(dynamic settings) {
            // TOTO :
        }

        public IEnumerable<BankTransaction> GetTransactions(BankTransactionSearchDocument searchDoc) {
            throw new NotImplementedException();
        }
    }
}
