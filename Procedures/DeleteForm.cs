using System;
using System.Data;
using System.Data.Odbc;
using System.Windows.Forms;

namespace Procedures
{
    public partial class DeleteForm : Form
    {
        public DeleteForm()
        {
            InitializeComponent();
        }
         private void btnDelete_Click(object sender, EventArgs e)
        {
            string bookIdString = txtBookId.Text.Trim();
            int bookId;

            if (!int.TryParse(bookIdString, out bookId))
            {
                MessageBox.Show("Please enter a valid integer value for Book ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!BookExists(bookId))
            {
                MessageBox.Show("Book with the specified ID does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtBookId.Clear();
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete the book with ID " + bookId + "?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    DeleteBookUsingStoredProcedure(bookId);
                    MessageBox.Show("Book deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while deleting the book: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool BookExists(int bookId)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";


            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Books WHERE book_id = ?";
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BookID", bookId);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void DeleteBookUsingStoredProcedure(int bookId)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                if (Form1.ProcedureExists("DeleteBook", connection))
                {
                    string query = "EXEC DeleteBook ?";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@p1", bookId);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    throw new Exception("Procedure DeleteBook doesn't exist");
                }
            }
        }
        
        
    }
}