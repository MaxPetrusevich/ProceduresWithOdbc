using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Windows.Forms;

namespace Procedures
{
    public partial class InsertForm : Form
    {
        private OdbcConnection connection;
        private List<ColumnInfo> columns;
        private Dictionary<string, List<object>> secondaryKeyValues; 
        public InsertForm(OdbcConnection conn)
        {
            InitializeComponent();
            connection = conn;
            buttonInsert.Click += ButtonInsert_Click;

            LoadTableNames();
            comboBoxTables.SelectedItem = Form1.selectedTableName;
            if (!string.IsNullOrEmpty(Form1.selectedTableName))
            {
                LoadTableColumns(Form1.selectedTableName);
            }
        }

        private void ButtonInsert_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxTables.SelectedItem?.ToString()))
            {
                MessageBox.Show("Пожалуйста, выберите таблицу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string selectedTableName = comboBoxTables.SelectedItem.ToString();
                InsertRecord(selectedTableName);
                MessageBox.Show("Запись успешно добавлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadTableNames()
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    comboBoxTables.Items.Clear();
                    connection.Open();
                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        if (!tableName.Contains("trace"))
                        {
                            comboBoxTables.Items.Add(tableName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}", "Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private void LoadTableColumns(string tableName)
        {
            columns = new List<ColumnInfo>();
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (OdbcCommand cmd = new OdbcCommand(
                               $@"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
                               connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string columnName = reader.GetString(0);
                                string dataType = reader.GetString(1);
                                columns.Add(new ColumnInfo(columnName, dataType));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки столбцов таблицы: {ex.Message}", "Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }

            panelInput.Controls.Clear();

            foreach (var column in columns)
            {
                if (IsPrimaryKey(tableName, column.Name))
                {
                    continue;
                }

                if (IsForeignKey(tableName, column.Name))
                {
                    ComboBox comboBox = new ComboBox();
                    comboBox.Name = "comboBox" + column.Name;
                    comboBox.Width = 150;
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    LoadForeignKeyValues(tableName, column.Name, comboBox);
                    Label label = new Label();
                    label.Text = column.Name + ":";
                    label.Width = 150;

                    panelInput.Controls.Add(label);
                    panelInput.Controls.Add(comboBox);
                }
                else
                {
                    TextBox textBox = new TextBox();
                    textBox.Name = "textBox" + column.Name;
                    textBox.Text = "";
                    textBox.Width = 150;
                    Label label = new Label();
                    label.Text = column.Name + ":";
                    label.Width = 150;

                    panelInput.Controls.Add(label);
                    panelInput.Controls.Add(textBox);
                }
            }
        }

        private bool IsPrimaryKey(string tableName, string columnName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            string query = $@"
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}' AND CONSTRAINT_NAME = 'PK_{tableName}'";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private bool IsForeignKey(string tableName, string columnName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            string query = $@"
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}' AND CONSTRAINT_NAME <> 'PK_{tableName}'";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

private void LoadForeignKeyValues(string tableName, string columnName, ComboBox comboBox)
{
    string connectionString = "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

    using (OdbcConnection connection = new OdbcConnection(connectionString))
    {
        connection.Open();

        string foreignKeyTable = "";
        string primaryKeyColumn = "";
        string foreignKeyColumn = columnName;

        using (OdbcCommand fkCmd = new OdbcCommand(
                   $@"SELECT 
            OBJECT_NAME(fkc.parent_object_id) AS [Foreign Table],
            COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS [Foreign Column],
            OBJECT_NAME(fkc.referenced_object_id) AS [Primary Table],
            COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS [Primary Column]
       FROM 
            sys.foreign_key_columns AS fkc
       INNER JOIN 
            sys.tables AS tb1
       ON 
            fkc.parent_object_id = tb1.object_id
       INNER JOIN 
            sys.tables AS tb2
       ON 
            fkc.referenced_object_id = tb2.object_id
       WHERE 
            tb1.name = '{tableName}' AND 
            COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = '{columnName}'", connection))
        {
            using (OdbcDataReader fkReader = fkCmd.ExecuteReader())
            {
                if (fkReader.Read())
                {
                    foreignKeyTable = fkReader.GetString(2); 
                    foreignKeyColumn = fkReader.GetString(3);
                    primaryKeyColumn = fkReader.GetString(1);
                }
            }
        }

        Console.WriteLine($"Foreign key table: {foreignKeyTable}, foreign key column: {foreignKeyColumn}, primary key column: {primaryKeyColumn}");

        if (string.IsNullOrEmpty(foreignKeyTable) || string.IsNullOrEmpty(foreignKeyColumn) || string.IsNullOrEmpty(primaryKeyColumn))
        {
            Console.WriteLine("Foreign key not found.");
            return;
        }

        string query = $@"SELECT DISTINCT {foreignKeyColumn} FROM {foreignKeyTable}";
        using (OdbcCommand command = new OdbcCommand(query, connection))
        {
            using (OdbcDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    comboBox.Items.Add(reader.GetString(0));
                }
            }
        }
    }
}




private void ValidateInputFields()
{
    foreach (Control control in panelInput.Controls)
    {
        if (control is TextBox textBox && textBox.Name.StartsWith("textBox"))
        {
            string columnName = textBox.Name.Substring("textBox".Length);
            var column = columns.FirstOrDefault(c => c.Name == columnName);
            if (column != null)
            {
                string dataType = column.DataType.ToLower();
                if (dataType == "int")
                {
                    int value;
                    if (!int.TryParse(textBox.Text, out value))
                    {
                        MessageBox.Show($"Поле '{columnName}' должно быть целым числом.", "Ошибка ввода",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else if (dataType == "datetime")
                {
                    DateTime value;
                    if (!DateTime.TryParse(textBox.Text, out value))
                    {
                        MessageBox.Show($"Поле '{columnName}' должно быть датой.", "Ошибка ввода",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

    else if (control is ComboBox comboBox && comboBox.Name.StartsWith("textBox"))

        {
            string columnName = comboBox.Name.Substring("textBox".Length);
            var column = columns.FirstOrDefault(c => c.Name == columnName);
            if (column != null)
            {
                string dataType = column.DataType.ToLower();
                if (dataType == "int")
                {
                    int value;
                    if (!int.TryParse(comboBox.SelectedItem.ToString(), out value))
                    {
                        MessageBox.Show($"Поле '{columnName}' должно быть целым числом.", "Ошибка ввода",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else if (dataType == "datetime")
                {
                    DateTime value;
                    if (!DateTime.TryParse(comboBox.Text, out value))
                    {
                        MessageBox.Show($"Поле '{columnName}' должно быть датой.", "Ошибка ввода",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

            }
        }
    }
}

private void InsertRecord(string tableName)
{
    ValidateInputFields();
    string connectionString = "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

    using (OdbcConnection connection = new OdbcConnection(connectionString))
    {
        connection.Open();

        List<ColumnInfo> columns = new List<ColumnInfo>();
        using (OdbcCommand cmd = new OdbcCommand(
                   $@"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME NOT IN 
(SELECT cu.COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc ON cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
 WHERE tc.TABLE_NAME = '{tableName}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY')",
                   connection))
        {
            using (OdbcDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string columnName = reader.GetString(0);
                    string dataType = reader.GetString(1);
                    columns.Add(new ColumnInfo(columnName, dataType));
                }
            }
        }

        string parameterPlaceholders = string.Join(", ", Enumerable.Repeat("?", columns.Count).ToArray());

        string query = $"EXEC Insert{tableName} {parameterPlaceholders}";

        using (OdbcCommand command = new OdbcCommand(query, connection))
        {
            foreach (var column in columns)
            {
                OdbcType odbcType = GetOdbcType(column.DataType);

                command.Parameters.Add(column.Name, odbcType);
            }

            foreach (Control control in panelInput.Controls)
            {
                if (control is TextBox textBox && textBox.Name.StartsWith("textBox"))
                {
                    string columnName = textBox.Name.Substring("textBox".Length);
                    var parameter = command.Parameters[columnName];
                    parameter.Value = textBox.Text;
                }
                else if (control is ComboBox comboBox && comboBox.Name.StartsWith("comboBox"))
                {
                    string columnName = comboBox.Name.Substring("comboBox".Length);
                    var parameter = command.Parameters[columnName];
                    parameter.Value = comboBox.SelectedItem;
                }
            }

            command.ExecuteNonQuery();
        }
    }
}



        private void showException(Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        private OdbcType GetOdbcType(string dataType)
        {

            switch (dataType.ToLower())
            {
                case "int":
                    return OdbcType.Int;
                case "varchar":
                case "nchar":
                    return OdbcType.Text;
                case "datetime":
                    return OdbcType.DateTime;
                default:
                    return OdbcType.Text;
            }
        }


        private void comboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedTableName = comboBoxTables.SelectedItem.ToString();
            LoadTableColumns(selectedTableName);
        }
    }
}