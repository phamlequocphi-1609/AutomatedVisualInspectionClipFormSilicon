using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedVisualInspectionClipFormSilicon
{
    internal class Account
    {
        private string account_name;
        public string AccountName
        {
            get => account_name;
            set => account_name = value;
        }
        private string password;
        public string Password
        {
            get => password;
            set => password = value;
        }
        public enum AccountTypes
        {
            mechanic,
            engineer,
            worker
        }
        private AccountTypes accountType;
        public AccountTypes AccountType
        {
            get => accountType;
            set => accountType = value;
        }
        private string nameShow;
        public string NameShow
        {
            get
            {
                switch(accountType)
                {
                    case AccountTypes.mechanic:
                        nameShow = "Mechanic";
                        break;
                    case AccountTypes.engineer:
                        nameShow = "Engineer";
                        break;
                    case AccountTypes.worker:
                        nameShow = "Worker";
                        break;
                }
                return nameShow;
            }
            set
            {
                nameShow = value;
            }
        }
        private static int currentId = 1;
        private int id; 
        public int Id
        {
            get => id;
            set => id = value;
        }
        private Bitmap avatar;
        public Bitmap Avatar
        {
            get
            {
                switch(accountType)
                {
                    case AccountTypes.mechanic:
                        avatar = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.mechanic;
                        break;
                    case AccountTypes.engineer:
                        avatar = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.engineer;
                        break;
                    case AccountTypes.worker:
                        avatar = AutomatedVisualInspectionClipFormSilicon.Properties.Resources.factory_worker;
                        break;
                }
                return avatar;
            }
            set
            {
                avatar = value;
            }
        }
        public Account(string account_name, string password, AccountTypes accountType)
        {
            this.id = currentId++;
            this.account_name = account_name;
            this.password = password;
            this.accountType = accountType;
        }
    }
}
