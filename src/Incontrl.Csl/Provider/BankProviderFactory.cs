using System;
using System.Collections.Generic;
using System.Text;
using Incontrl.Provider.Concrete;

namespace Incontrl.Provider
{
    public class BankProviderFactory
    {
        public IBankProvider Get(string providerType, dynamic accountSettings) {
            // edw apo to providerType tha epistrefoume mia concrete class, swsta ?
            if ("nbg".Equals(providerType))
                return new NbgBankProvider(accountSettings);
            return null;
        }
    }
}
