using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedVisualInspectionClipFormSilicon
{
    internal class Product_list
    {
        private static Product_list instance;
        public static Product_list Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new Product_list();
                }
                return instance;
            }
            set => instance = value;
        }

        List<Product> productsList;
        public List<Product> ProductList
        {
            get => productsList;
            set => productsList = value;
        }
        public void AddProduct(Product product)
        {
            productsList.Add(product);
        }
        public Product_list()
        {
            productsList = new List<Product>();
        }  
    }
}
