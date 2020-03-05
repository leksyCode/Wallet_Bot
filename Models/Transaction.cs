using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotV3.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public int Cost { get; set; }

        public string Discription { get; set; }

        public DateTime CreatedDate { get; set; }

        public Operation Type { get; set; }
    
        public enum Operation
        {
            Income = 1,
            Expenses,

        }
    }
}
