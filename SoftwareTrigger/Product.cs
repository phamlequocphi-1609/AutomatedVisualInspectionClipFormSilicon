using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace AutomatedVisualInspectionClipFormSilicon
{
   
    internal class Product
    {
        private string product_name;
        public string ProductName
        {
            get => product_name;
            set => product_name = value;
        }
        private string status;
        public string Status
        {
            get => status;
            set => status = value;
        }
        private string description;
        public string Description
        {
            get => description;
            set => description = value;
        }
        private string created_at;
        public string CreatedAt
        {
            get => created_at;
            set => created_at = value;
        }
        public Product(string product_name, string status,string description, string created_at)
        {
            this.product_name = product_name;
            this.status = status;
            this.description = description;
            this.created_at = created_at;
        }
    }
}