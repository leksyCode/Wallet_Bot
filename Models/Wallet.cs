using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotV3.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public int Sum { get; set; }

        public List<Transaction> History { get; set; } = new List<Transaction>();

        public Task AddTransaction(Transaction tran)
        {
            if (tran.Type == Transaction.Operation.Income)
            {
                this.Sum += tran.Cost;
            }
            else if (tran.Type == Transaction.Operation.Expenses)
            {
                this.Sum -= tran.Cost;
            }

            History.Add(tran);
            return Task.CompletedTask;
        }
    }
}
