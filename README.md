# INDICE (incontrl.io) ![alt text](https://github.com/indice-co/Incontrl.Net/blob/master/icon/icon-64.png?raw=true "Incontrl logo")


## intro TODO 
We are devoloping a worker (Console app) that will 
integrate with [NBG Open Bank API](https://apis.nbg.gr/public/) and owr own [incontrl.io](https://incontrl.io) acting as 

1. transaction provider 
2. a reconcilliation algorithm for `incontrl` issued invoices (maching)

- Bulk import στις τραπεζικές κινήσεις
- NBG Provider
  - Get bank account
  - Resolve provider
  - Call provider with settings (θα μπορούσαμε να ρυθμίσουμε ποιες σειρές τιμολογίων θα κοιτάει ή για πόσο πίσω στο χρόνο θα κοιτάει για κινήσεις)
  - Output transactions to API transactions
  - For each imported transaction run reconciliation
  - Get matching invoices by payment code
  - Create link (payment) between invoice and transaction
  - Add payment options to (mock) payments through the invoice match
 
## Tech 
- [dotnet core](https://dot.net) 
- Opensource [incontrol.io .Net SDK](https://github.com/indice-co/Incontrl.Net) on github
- ibank [open bank apis](http://developer.nbg.gr) (PSD2) 