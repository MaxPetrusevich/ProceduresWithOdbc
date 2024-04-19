using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Procedures
{
    public partial class Form1 : Form
    {
        private Dictionary<object, Dictionary<string, object>> changedValues =
            new Dictionary<object, Dictionary<string, object>>();

        public Form1()
        {
            InitializeComponent();
            LoadTableNames();
            dataGridView1.AllowUserToAddRows = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            ShowInsertForm();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedTableName))
            {
                showException(new Exception("Выберите таблицу из списка"));
                return;
            }
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            try
            {
                if (dataGridView1.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите запись для удаления.", "Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                object primaryKeyValue = dataGridView1.SelectedRows[0].Cells[primaryKeyColumnName].Value;
                if (DialogResult.Yes == MessageBox.Show("Вы уверены, что хотите удалить выбранную запись?.", "Выбор",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {

                    DeleteRecordFromDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue,
                        connectionString);

                    FillDataGridViewWithData(selectedTableName);

                    MessageBox.Show("Запись удалена успешно.", "Успех", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        

        private void DeleteRecordFromDatabase(string tableName, string primaryKeyColumnName, object primaryKeyValue,
            string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                string query = $"EXEC Delete{tableName} @{primaryKeyColumnName}=?";
                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@p1", primaryKeyValue);

                    command.ExecuteNonQuery();
                }
            }
        }


        private void button3_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedTableName))
            {
                showException(new Exception("Таблица не была выбрана"));
                return;
            }
            FillDataGridViewWithData(selectedTableName);
        }

        private List<string> GetDependentTables(string tableName, OdbcConnection connection)
        {
            List<string> dependentTables = new List<string>();

            try
            {
                using (OdbcCommand command = new OdbcCommand(
                           $@"SELECT OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
                FROM sys.foreign_keys fk
                JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                JOIN sys.tables t ON t.object_id = fk.parent_object_id
                WHERE t.name = '{tableName}'",
                           connection))
                {
                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dependentTable = reader.GetString(0);
                            dependentTables.Add(dependentTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                showException(ex);
            }

            return dependentTables;
        }
        private void AfterDataBinding()
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name.Contains("_"))
                {
                    string[] parts = column.Name.Split('_');
                    column.HeaderText = $"{parts[0]}_{parts[1]}";
                }
            }
        }

private void FillDataGridViewWithData(string selectedTableName)
{
    string connectionString =
        "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

    try
    {
        using (OdbcConnection connection = new OdbcConnection(connectionString))
        {
            connection.Open();
            primaryKeyColumnName = GetPrimaryKeyColumn(selectedTableName, connection);
            List<string> dependentTables = GetDependentTables(selectedTableName, connection);

            using (OdbcCommand command = new OdbcCommand($"Select{selectedTableName}", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                using (OdbcDataAdapter adapter = new OdbcDataAdapter(command))
                {
                    DataSet dataSet = new DataSet();

                    adapter.Fill(dataSet, selectedTableName);

                   

                    dataGridView1.DataSource = dataSet.Tables[selectedTableName];
                   
                }
            }
        }
    }
    catch (Exception ex)
    {
        showException(ex);
    }
}


        public static string selectedTableName = "";

        private void createProceduresForTableIfNotExists(string tableName)
        {
            createDeleteProcedure(tableName);
            CreateSelectProcedure(tableName);
            createInsertProcedure(tableName);
            createUpdateProcedure(tableName);
        }

        private void LoadTableNames()
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    comboBox1.Items.Clear();
                    connection.Open();
                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        if (!tableName.Contains("trace"))
                        {
                            comboBox1.Items.Add(tableName);
                        }
                    }
                }
                catch(Exception ex)
                {
                     MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}");
                }
                finally
                {
                    connection.Close();
                }
            }
        }


        private void showException(Exception exception)
        {
            MessageBox.Show(exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void createUpdateProcedure(string tableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    List<ColumnInfo> columns = new List<ColumnInfo>();
                    using (OdbcCommand cmd = new OdbcCommand(
                               $@"SELECT COLUMN_NAME, DATA_TYPE 
                          FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_NAME = '{tableName}' 
                          AND COLUMN_NAME NOT IN (
                              SELECT cu.COLUMN_NAME 
                              FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu 
                              JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
                              ON cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME 
                              WHERE cu.TABLE_NAME = '{tableName}' 
                              AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                          )",
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

                    if (!ProcedureExists($"Update{tableName}", connection))
                    {
                        if (columns != null && columns.Count > 0)
                        {
                            string parameters = string.Join(", ",
                                columns.Select(col => $"@{col.Name} {GetOdbcType(col.DataType)}").ToArray());

                            string updateStatement =
                                string.Join(", ", columns.Select(col => $"{col.Name} = @{col.Name}").ToArray());

                            string primaryKey = GetPrimaryKeyColumn(tableName, connection);

                            string createProcedureQuery = $@"
    CREATE PROCEDURE Update{tableName}
        @{primaryKey} INT,
        {parameters}
    AS
    BEGIN
        UPDATE {tableName} SET {updateStatement} WHERE {primaryKey} = @{primaryKey}
    END;";

                            using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                            {
                                foreach (var column in columns)
                                {
                                    command.Parameters.Add("@" + column.Name,
                                        GetOdbcType(column.DataType)); 
                                }

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }
        }


        private void createDeleteProcedure(string tableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string primaryKey = "";
                    using (OdbcCommand cmd = new OdbcCommand(
                               $@"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}'",
                               connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                primaryKey = reader.GetString(0);
                            }
                        }
                    }

                    if (!ProcedureExists($@"Delete{tableName}", connection))
                    {
                        string createProcedureQuery = $@"
        CREATE PROCEDURE Delete{tableName}
            @{primaryKey} INT
        AS
        BEGIN
            DELETE FROM {tableName} WHERE {primaryKey} = @{primaryKey}
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


        private void createInsertProcedure(string tableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    if (!ProcedureExists($"Insert{tableName}", connection))
                    {
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

                        if (columns != null && columns.Count > 0)
                        {
                            string parameters = string.Join(", ",
                                columns.Select(col => $"@{col.Name} {GetOdbcType(col.DataType).ToString()}").ToArray());

                            string insertStatement = string.Join(", ", columns.Select(col => col.Name).ToArray());

                            string createProcedureQuery = $@"
                        CREATE PROCEDURE Insert{tableName}
                            {parameters}
                        AS
                        BEGIN
                            INSERT INTO {tableName} ({insertStatement}) VALUES ({string.Join(", ", columns.Select(col => $"@{col.Name}").ToArray())})
                        END;";

                            using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                            {
                                foreach (var column in columns)
                                {
                                    command.Parameters.Add("@" + column.Name, GetOdbcType(column.DataType));
                                }

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }
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
        private List<ColumnInfo> GetTableColumns(string tableName, OdbcConnection connection)
        {
            List<ColumnInfo> columns = new List<ColumnInfo>();

            try
            {
                using (OdbcCommand command = new OdbcCommand(
                           $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'",
                           connection))
                {
                    using (OdbcDataReader reader = command.ExecuteReader())
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
                showException(ex);
            }

            return columns;
        }

 private void CreateSelectProcedure(string tableName)
{
    string connectionString =
        "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

    using (OdbcConnection connection = new OdbcConnection(connectionString))
    {
        try
        {
            connection.Open();
            if (!ProcedureExists($"Select{tableName}", connection))
            {
                List<string> tablesWithSecondaryKeys = GetTablesWithSecondaryKeys(connectionString);

                string createProcedureQuery = $@"
CREATE PROCEDURE Select{tableName}
AS
BEGIN
    SELECT ";

                List<ColumnInfo> mainTableColumns = GetTableColumns(tableName, connection);
                foreach (var column in mainTableColumns)
                {
                    createProcedureQuery += $@"
    MainTable.{column.Name},";
                }

                foreach (var relatedTable in tablesWithSecondaryKeys)
                {
                    List<ColumnInfo> relatedTableColumns = GetTableColumns(relatedTable, connection);
                    foreach (var column in relatedTableColumns)
                    {
                        createProcedureQuery += $@"
    RelatedTable_{relatedTable}.{column.Name} AS {relatedTable}_{column.Name},";
                    }
                }

                createProcedureQuery = createProcedureQuery.TrimEnd(',');

                createProcedureQuery += $@"
    FROM {tableName} AS MainTable";

                foreach (var relatedTable in tablesWithSecondaryKeys)
                {
                    if (relatedTable != tableName)
                    {
                        string primaryKeyRelatedTable = GetPrimaryKeyColumn(relatedTable, connection);
                        createProcedureQuery += $@"
    LEFT JOIN {relatedTable} AS RelatedTable_{relatedTable} ON MainTable.{relatedTable} = RelatedTable_{relatedTable}.{primaryKeyRelatedTable}";
                    }
                }

                createProcedureQuery += @"
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





        private string GetPrimaryKeyColumn(string tableName, OdbcConnection connection)
        {
            string primaryKeyColumn = null;

            try
            {
                string query = $@"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                AND TABLE_NAME = '{tableName}'";

                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            primaryKeyColumn = reader.GetString(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                showException(ex);
            }

            return primaryKeyColumn;
        }

        private List<string> GetTablesWithSecondaryKeys(string connectionString)
        {
            List<string> tablesWithSecondaryKeys = new List<string>();
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command =
                       new OdbcCommand(
                           "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                           connection))
                using (OdbcDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tableName = reader.GetString(0);

                        if (TableHasSecondaryKeys(tableName, connectionString) &&
                            !TableIsSelfReferencing(tableName, connectionString))
                        {
                            if (tableName != selectedTableName)
                            {
                                tablesWithSecondaryKeys.Add(tableName);
                            }
                        }
                    }
                }

                connection.Close();
                return tablesWithSecondaryKeys;
            }
        }

        private bool TableHasSecondaryKeys(string tableName, string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command =
                       new OdbcCommand(
                           $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}' AND CONSTRAINT_NAME <> 'PRIMARY'",
                           connection))
                {
                    int count = (int)command.ExecuteScalar();
                    connection.Close();
                    return count > 0;
                }
            }
        }

        private bool TableIsSelfReferencing(string tableName, string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command =
                       new OdbcCommand(
                           $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}') AND UNIQUE_CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}')",
                           connection))
                {
                    int count = (int)command.ExecuteScalar();
                    connection.Close();
                    return count > 0;
                }
            }
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedTableName = comboBox1.SelectedItem.ToString();
            primaryKeyColumnName = GetPrimaryKeyColumnName(selectedTableName);
            createProceduresForTableIfNotExists(selectedTableName);
        }


        private void ShowInsertForm()
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                using (InsertForm insertForm = new InsertForm(connection))
                {
                    if (insertForm.ShowDialog() == DialogResult.OK)
                    {
                        FillDataGridViewWithData(selectedTableName);
                    }
                }
            }
        }

        private string primaryKeyColumnName = "";


        private string GetPrimaryKeyColumnName(string tableName)
        {
            string primaryKeyColumnName = "";

            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(
                               $@"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}'",
                               connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                primaryKeyColumnName = reader.GetString(0);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }

            return primaryKeyColumnName;
        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            try
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name != primaryKeyColumnName)
                {
                    string newValue = e.FormattedValue.ToString();

                    if (!IsValidValue(newValue))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Введено недопустимое значение.", "Ошибка", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name != primaryKeyColumnName)
                {
                    var primaryKeyValue = dataGridView1.Rows[e.RowIndex].Cells[primaryKeyColumnName].Value;
                    var columnName = dataGridView1.Columns[e.ColumnIndex].Name;
                    var newValue = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    if (!changedValues.ContainsKey(primaryKeyValue))
                        changedValues.Add(primaryKeyValue, new Dictionary<string, object>());

                    changedValues[primaryKeyValue][columnName] = newValue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                dataGridView1.CancelEdit();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedTableName))
            {
                showException(new Exception("Выберите таблицу из списка"));
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(primaryKeyColumnName))
                {
                    MessageBox.Show("Не удалось определить столбец первичного ключа.", "Ошибка", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                string connectionString =
                    "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

                foreach (var primaryKeyValue in changedValues.Keys)
                {
                    Dictionary<string, object> updatedValues = new Dictionary<string, object>();

                    Dictionary<string, object> oldValues =
                        GetRecordValuesFromDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue);

                    foreach (var columnName in oldValues.Keys)
                    {
                        if (dataGridView1.Columns[columnName].ReadOnly != true || columnName.Equals(primaryKeyColumnName))
                        {
                            object newValue = changedValues[primaryKeyValue].ContainsKey(columnName)
                                ? changedValues[primaryKeyValue][columnName]
                                : oldValues[columnName];
                            updatedValues.Add(columnName, newValue);
                        }
                    }

                    UpdateRecordInDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue, updatedValues,
                        connectionString);
                }

                MessageBox.Show("Data updated successfully.", "Success", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                changedValues.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, object> GetRecordValuesFromDatabase(string tableName, string primaryKeyColumnName,
            object primaryKeyValue)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command =
                       new OdbcCommand($"SELECT * FROM {tableName} WHERE {primaryKeyColumnName} = ?", connection))
                {
                    command.Parameters.AddWithValue("@primaryKeyValue", primaryKeyValue);
                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object columnValue = reader.GetValue(i);
                                values.Add(columnName, columnValue);
                            }
                        }
                    }
                }
            }

            return values;
        }

        private void UpdateRecordInDatabase(string tableName, string primaryKeyColumnName, object primaryKeyValue,
            Dictionary<string, object> updatedValues, string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();

                StringBuilder parameters = new StringBuilder();
                foreach (var columnName in updatedValues.Keys)
                {
                    parameters.Append($"@{columnName} = ?, ");
                }

                parameters.Length -= 2;

                string query = $"EXEC Update{tableName} {parameters}";

                using (OdbcCommand command = new OdbcCommand(query, connection))
                {

                    foreach (var columnName in updatedValues.Keys)
                    {
                        object columnValue = updatedValues[columnName];
                        OdbcType odbcType =
                            GetOdbcType(GetColumnInfo(tableName, columnName, connectionString).DataType);
                        command.Parameters.Add("@" + columnName, odbcType).Value = columnValue;
                    }

                    // Выполняем запрос
                    command.ExecuteNonQuery();
                }
            }
        }


        private ColumnInfo GetColumnInfo(string tableName, string columnName, string connectionString)
        {
            ColumnInfo columnInfo = null;
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command =
                       new OdbcCommand(
                           $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'",
                           connection))
                {
                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader.GetString(0);
                            string dataType = reader.GetString(1);
                            columnInfo = new ColumnInfo(name, dataType);
                        }
                    }
                }
            }

            return columnInfo;
        }


        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                DataGridViewColumn currentColumn = dataGridView1.Columns[e.ColumnIndex];

                if (currentColumn.Name == primaryKeyColumnName)
                {
                    currentColumn.ReadOnly = true;
                }
                else
                {
                    currentColumn.ReadOnly = false;
                }

                string connectionString =
                    "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

                try
                {
                    using (OdbcConnection connection = new OdbcConnection(connectionString))
                    {
                        connection.Open();
                        List<string> dependentTables = GetDependentTables(selectedTableName, connection);
                        connection.Close();
                        AfterDataBinding();

                        foreach (var dependentTable in dependentTables)
                        {
                            foreach (DataGridViewColumn column in dataGridView1.Columns)
                            {
                                if (column.Name.Contains(dependentTable) && !column.Name.Equals(dependentTable))
                                {
                                    column.ReadOnly = true;
                                    Console.WriteLine($"Column '{column.Name}' set to ReadOnly");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    showException(ex);
                }
            }
        }


        private bool IsValidValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.Contains("'"))
                return false;

            return true;
        }
    }
}