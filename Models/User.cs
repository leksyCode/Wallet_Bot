using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotV3.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }

        public Wallet Wallet;
        public bool SetTransaction { get; set; } 
        public bool DeleteTransaction { get; set; }
        public bool ShareWithUser { get; set; }        
        public Transaction.Operation TransactionType { get; set; }

        

        public User(int id, string name, string userName, Wallet wallet)
        {
            Id = id;
            Name = name;
            UserName = userName;
            Wallet = wallet;
        }

        public void SetToSharedWalled(ref Wallet wallet)
        {
            Wallet = wallet;
        }
    }
}
