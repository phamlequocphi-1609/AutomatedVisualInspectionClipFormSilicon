using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;


namespace AutomatedVisualInspectionClipFormSilicon
{
    public partial class Login : Form
    {
        List<Account> listAccounts = Account_List.Instance.AccountList;
        public Login()
        {
            InitializeComponent();
          
        }
        // function to check login
        bool checkLogin(string accountname, string pass)
        {
            for (int i = 0; i < listAccounts.Count; i++)
            {
                if (accountname == listAccounts[i].AccountName && pass == listAccounts[i].Password)
                {
                    Const.Account = listAccounts[i];
                    return true;
                }
            }
            return false;
        }
        private void button_login_Click(object sender, EventArgs e)
        {
            string accountName = textBox_accountname.Text;
            string password = textBox_password.Text;
            bool isCheck = true;
            if(accountName.Trim() == "")
            {
                label_error1.Visible = true;
                label_error1.Text = "This is required";
                isCheck = false;
            }
            else
            {
                label_error1.Visible = false;
                label_error1.Text = "";  
                isCheck = true;
            }
            if(password.Trim() == "")
            {
                label_error2.Visible = true;
                label_error2.Text = "This is required";
                isCheck = false;
            }
            else
            {
                label_error2.Visible = false;
                label_error2.Text = "";
                isCheck = true;
            }
            if(isCheck == true)
            {
                if(checkLogin(accountName, password))
                {
                    MessageBox.Show("Logged in successfully", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Hide();
                    Home home = new Home();
                    home.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Incorrect username or password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // function to show or hide password
        private void checkBox_showPass_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_showPass.Text = (checkBox_showPass.Checked == true) ? "Hide Password" : "Show Password";
            textBox_password.UseSystemPasswordChar = (checkBox_showPass.Checked == true) ? true : false;
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }
    }
}
