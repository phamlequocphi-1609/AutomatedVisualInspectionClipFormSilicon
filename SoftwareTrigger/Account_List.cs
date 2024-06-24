using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedVisualInspectionClipFormSilicon
{
    internal class Account_List
    {
        /// <summary>
        /// </summary>
        private static Account_List instance;
        // properties
        public static Account_List Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new Account_List();
                }
                return instance;
            }
            set => instance = value;
        }
        List<Account> accountList;
        public List<Account> AccountList
        {
            get => accountList; 
            set => accountList = value;
        }
        public Account_List()
        {
            accountList = new List<Account>();
            accountList.Add(new Account("premomechanic", "mechanic", Account.AccountTypes.mechanic));
            accountList.Add(new Account("premoengineer", "engineer", Account.AccountTypes.engineer));
            accountList.Add(new Account("premoworker", "worker", Account.AccountTypes.worker));
        }
    }
}