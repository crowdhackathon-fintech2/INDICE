using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Incontrl.Provider.Concrete
{
    public class Transaction
    {
        public TransactionDetails Details { get; set; }   

        public string Id { get; set; }
    }
    
    public class TransactionDetails
    {
        public TransactionValue Value { get; set; }

        public DateTime Completed { get; set; }

        public string Description { get; set; }
    }

    public class TransactionValue
    {
        public string Currency { get; set; }

        public decimal Amount { get; set; }
    }
}
