using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace AutomatedVisualInspectionClipFormSilicon
{
    internal class Savedata
    {
        public static void SaveFile(List<Product> products, string FilePath)
        {
            try
            {
                StreamWriter write = new StreamWriter(FilePath, true, Encoding.UTF8);
                foreach (Product product in products)
                {
                    write.WriteLine($"{product.ProductName},{product.Status},{product.Description},{product.CreatedAt}");
                }
                write.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi lưu file: " + ex.Message);
            }
        }
        public static List<Product> FileRead(string path)
        {
            List<Product> products = new List<Product>();
            try
            {
                StreamReader reader = new StreamReader(path);
                string line = reader.ReadLine();
                while (line != null)
                {
                    string[] data = line.Split(',');
                    if (data.Length > 0)
                    {
                        Product product = new Product(data[0], data[1], data[2], data[3]);
                        products.Add(product);
                    }
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return products;
        }
    }
}

