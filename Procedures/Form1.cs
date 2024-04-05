using System;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Windows.Forms;

namespace Procedures
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            createInsertProcedure();
            createDeleteProcedure();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Input();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                DeleteForm deleteForm = new DeleteForm();
                deleteForm.ShowDialog();
            }
            catch (Exception ex)
            {
                showException(new Exception(ex.Message));
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();


                    string query = "SELECT * FROM dbo.Book";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataAdapter adapter = new OdbcDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();

                            adapter.Fill(dataTable);

                            Form dataForm = new Form();
                            dataForm.Text = "Data from SQL Server";
                            dataForm.Size = new Size(600, 400);

                            DataGridView dataGridView = new DataGridView();
                            dataGridView.Dock = DockStyle.Fill;
                            dataGridView.DataSource = dataTable;

                            dataForm.Controls.Add(dataGridView);

                            dataForm.ShowDialog();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void showException(Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        private void createInsertProcedure()
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    if (!ProcedureExists("InsertBooks", connection))
                    {
                        string createProcedureQuery = @"
                        CREATE PROCEDURE InsertBooks
                            @BookID int, @BookName varchar(25), @Author varchar(25), @Year int
                        AS
                        BEGIN
                            INSERT INTO DBO.BOOK (book_id, book_name, author, year) values (@BookID, @BookName, @Author, @Year)
                        END;";

                        using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }
        }

        private void createDeleteProcedure()
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    if (!ProcedureExists("DeleteBook", connection))
                    {
                        string createProcedureQuery = @"
                        CREATE PROCEDURE DeleteBook
                            @BookID int
                        AS
                        BEGIN
                            DELETE FROM BOOK where book_id = @BookID
                        END;";

                        using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }
        }

        private void Input()
        {
            using (InputForm inputForm = new InputForm())
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    int bookId = inputForm.BookID;
                    string bookName = inputForm.BookName;
                    string author = inputForm.Author;
                    int year = inputForm.Year;

                    string connectionString =
                        "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

                    using (OdbcConnection connection = new OdbcConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                            if (DeleteForm.BookExists(bookId))
                            {
                                showException(new Exception($"Book with id={bookId} already exists"));
                            }
                            else
                            {
                                if (!ProcedureExists("InsertBooks", connection))
                                {
                                    MessageBox.Show("Stored procedure InsertBooks doesn't exist.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                string query = "EXEC InsertBooks ?, ?, ?, ?";

                                using (OdbcCommand command = new OdbcCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@p1", bookId);
                                    command.Parameters.AddWithValue("@p2", bookName);
                                    command.Parameters.AddWithValue("@p3", author);
                                    command.Parameters.AddWithValue("@p4", year);

                                    command.ExecuteNonQuery();

                                    MessageBox.Show("Data submitted successfully.", "Success", MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        // Метод для проверки существования хранимой процедуры
        public static bool ProcedureExists(string procedureName, OdbcConnection connection)
        {
            string query = "IF EXISTS (SELECT * FROM sys.procedures WHERE name = ?) SELECT 1 ELSE SELECT 0";

            using (OdbcCommand command = new OdbcCommand(query, connection))
            {
                command.Parameters.AddWithValue("@procedureName", procedureName);

                int result = (int)command.ExecuteScalar();
                return result == 1;
            }
        }
    }
}