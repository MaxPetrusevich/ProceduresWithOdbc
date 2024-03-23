using System;
using System.Windows.Forms;

namespace Procedures
{
    public partial class InputForm : Form
    {
   
        private System.Windows.Forms.Label lblBookID;
        private Label lblBookName;
        private Label lblAuthor;
        private Label lblYear;

        private TextBox txtBookID;
        private TextBox txtBookName;
        private TextBox txtAuthor;
        private TextBox txtYear;

        private Button btnOK;
        private Button btnCancel;
        public int BookID { get; private set; }
        public string BookName { get; private set; }
        public string Author { get; private set; }
        public int Year { get; private set; }
        public InputForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtBookID.Text, out int bookID) &&
                !string.IsNullOrEmpty(txtBookName.Text) &&
                !string.IsNullOrEmpty(txtAuthor.Text) &&
                int.TryParse(txtYear.Text, out int year))
            {
                BookID = bookID;
                BookName = txtBookName.Text;
                Author = txtAuthor.Text;
                Year = year;
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please enter valid data for all fields.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}