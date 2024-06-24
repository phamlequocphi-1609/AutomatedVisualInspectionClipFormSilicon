using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutomatedVisualInspectionClipFormSilicon
{
    public partial class CheckedProductList : Form
    {
        public CheckedProductList()
        {
            InitializeComponent();
        }
        private void pictureBox_Home_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Do you want to return to the home page", "Notification", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            this.Hide();
            Home home = new Home();
            home.Show();        
        }
        public void ExportFile(DataTable dataTable, string fileName, string title)
        {
            // create excel object
            Microsoft.Office.Interop.Excel.Application workExcel = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbooks oBooks;
            Microsoft.Office.Interop.Excel.Sheets oSheets;
            Microsoft.Office.Interop.Excel.Workbook oBook;
            Microsoft.Office.Interop.Excel.Worksheet oSheet;
            // tạo 1 excel workbook
            workExcel.Visible = true;
            workExcel.DisplayAlerts = false;
            workExcel.Application.SheetsInNewWorkbook = 1;
            oBooks = workExcel.Workbooks;
            oBook = (Microsoft.Office.Interop.Excel.Workbook)(oBooks.Add(Type.Missing));
            oSheets = oBook.Worksheets;
            oSheet = (Microsoft.Office.Interop.Excel.Worksheet)oSheets.get_Item(1);
            oSheet.Name = fileName;
            // create title
            Microsoft.Office.Interop.Excel.Range head = oSheet.get_Range("A1", "E1"); 
            head.MergeCells = true;
            head.Value2 = title;
            head.Font.Bold = true;
            head.Font.Name = "Times New Roman";
            head.Font.Size = "18";
            head.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter; // căn giữa
            // Tạo tiêu đề cột
            Microsoft.Office.Interop.Excel.Range cl1 = oSheet.get_Range("A3", "A3");
            cl1.Value2 = "ID";
            cl1.ColumnWidth = 3;
            Microsoft.Office.Interop.Excel.Range cl2 = oSheet.get_Range("B3", "B3");
            cl2.Value2 = "Product Name";
            cl2.ColumnWidth = 25;
            Microsoft.Office.Interop.Excel.Range cl3 = oSheet.get_Range("C3", "C3");
            cl3.Value2 = "Status";
            cl3.ColumnWidth = 18;
            Microsoft.Office.Interop.Excel.Range cl4 = oSheet.get_Range("D3", "D3");
            cl4.Value2 = "Description";
            cl4.ColumnWidth = 20;
            Microsoft.Office.Interop.Excel.Range cl5 = oSheet.get_Range("E3", "E3");
            cl5.Value2 = "Created At";
            cl5.ColumnWidth = 20;
            Microsoft.Office.Interop.Excel.Range rowHead = oSheet.get_Range("A3", "E3");
            rowHead.Font.Bold = true;
            // kẻ viền
            rowHead.Borders.LineStyle = Microsoft.Office.Interop.Excel.Constants.xlSolid;
            // thiết lập màu nền
            rowHead.Interior.ColorIndex = 6;
            rowHead.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
            // tạo mảng 
            // Tạo 1 mảng 2 chiều kiểu Object 
            object [,] arr = new object[dataTable.Rows.Count, dataTable.Columns.Count];
            // chuyển dữ liệu từ datagripview vào mảng đối tượng
            for(int row = 0; row <dataTable.Rows.Count; row++)
            {
                DataRow dataRow = dataTable.Rows[row];
                for(int col = 0; col < dataTable.Columns.Count; col++)
                {
                    arr[row, col] = dataRow[col];
                }
            }
            // thiết lập vùng điền dữ liệu 
            int rowStart = 4;
            int columnStart = 1;
            int rowEnd = rowStart + dataTable.Rows.Count - 1;
            int columnEnd = dataTable.Columns.Count;
            // ô bất đầu điền dữ liệu
            Microsoft.Office.Interop.Excel.Range c1 = (Microsoft.Office.Interop.Excel.Range)oSheet.Cells[rowStart, columnStart];
            // ô kết thúc điền dữ liệu
            Microsoft.Office.Interop.Excel.Range c2 = (Microsoft.Office.Interop.Excel.Range)oSheet.Cells[rowEnd, columnEnd];
            // lấy về vùng điền dữ liệu
            Microsoft.Office.Interop.Excel.Range range = oSheet.get_Range(c1, c2);
            // điền dữ liệu vào vùng đã thiết lập
            range.Value2 = arr;
            // kẻ viền
            range.Borders.LineStyle = Microsoft.Office.Interop.Excel.Constants.xlSolid;
            // căn giữa cả bảng
            oSheet.get_Range(c1, c2).HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;           
        }
        // show data
        int count = 0;
        private void button2_Click_1(object sender, EventArgs e)
        {
            string path = Application.StartupPath + "\\dataresult.csv";
            List<Product> products = Savedata.FileRead(path);

            foreach (Product product in products)
            {
                count++;
                dataGridView1.Rows.Add(count, product.ProductName, product.Status, product.Description, product.CreatedAt);
            }
        }
        // export data
        private void button_exportdata_Click(object sender, EventArgs e)
        {
            DataTable dataTable = new DataTable();
            DataColumn col1 = new DataColumn("ID");
            DataColumn col2 = new DataColumn("Product Name");
            DataColumn col3 = new DataColumn("Status");
            DataColumn col4 = new DataColumn("Description");
            DataColumn col5 = new DataColumn("Created at");
            dataTable.Columns.Add(col1);
            dataTable.Columns.Add(col2);
            dataTable.Columns.Add(col3);
            dataTable.Columns.Add(col4);
            dataTable.Columns.Add(col5);
            foreach (DataGridViewRow dataGridViewRow in dataGridView1.Rows)
            {
                DataRow datarow = dataTable.NewRow();

                datarow[0] = dataGridViewRow.Cells[0].Value;
                datarow[1] = dataGridViewRow.Cells[1].Value;
                datarow[2] = dataGridViewRow.Cells[2].Value;
                datarow[3] = dataGridViewRow.Cells[3].Value;
                datarow[4] = dataGridViewRow.Cells[4].Value;
                dataTable.Rows.Add(datarow);
            }
            ExportFile(dataTable, "List", "CHECKED PRODUCT LIST");
        }
    }
}
