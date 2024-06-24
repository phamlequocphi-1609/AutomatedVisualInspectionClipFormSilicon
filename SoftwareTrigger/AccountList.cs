using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace AutomatedVisualInspectionClipFormSilicon
{
    public partial class AccountList : Form
    {
        List<Account> listAccounts = Account_List.Instance.AccountList;
        public AccountList()
        {
            InitializeComponent();    
        }
        private void AccountList_Load(object sender, EventArgs e)
        {
            showUserData();         
        }
        private void showUserData()
        {
            foreach (Account account in listAccounts)
            {
                Bitmap avatar = account.Avatar;
                dataGridView1.Rows.Add(account.Id, account.AccountName, account.Password, account.NameShow, avatar);
            }
        }
        private void pictureBox_Home_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Do you want to return to the home page", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            this.Hide();
            Home home = new Home();
            home.Show();
        }    
    }
}