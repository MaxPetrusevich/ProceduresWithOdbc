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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ShowInsertForm();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            try
            {
                // Проверяем, что есть выбранная запись для удаления
                if (dataGridView1.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите запись для удаления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Получаем значение первичного ключа выбранной записи
                object primaryKeyValue = dataGridView1.SelectedRows[0].Cells[primaryKeyColumnName].Value;

                // Вызываем хранимую процедуру Delete для удаления выбранной записи
                DeleteRecordFromDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue, connectionString);

                // Обновляем отображение данных в DataGridView
                FillDataGridViewWithData(selectedTableName);

                MessageBox.Show("Запись удалена успешно.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void DeleteRecordFromDatabase(string tableName, string primaryKeyColumnName, object primaryKeyValue, string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                string query =  $"EXEC Delete{tableName} @{primaryKeyColumnName}=?";
                using (OdbcCommand command = new OdbcCommand(query,connection))
                {

                    // Создаем параметр для первичного ключа
                    command.Parameters.AddWithValue("@p1", primaryKeyValue);

                    // Выполняем запрос
                    command.ExecuteNonQuery();
                }
            }
        }



        private void button3_Click_1(object sender, EventArgs e)
        {
            FillDataGridViewWithData(selectedTableName);
        }

        private void FillDataGridViewWithData(string selectedTableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            primaryKeyColumnName = GetPrimaryKeyColumnName(selectedTableName);

            try
            {
                using (OdbcConnection connection = new OdbcConnection(connectionString))
                {
                    connection.Open();

                    // Создаем команду для вызова хранимой процедуры
                    using (OdbcCommand command = new OdbcCommand($"Select{selectedTableName}", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Создаем адаптер для чтения данных из базы
                        using (OdbcDataAdapter adapter = new OdbcDataAdapter(command))
                        {
                            // Создаем DataSet для хранения данных
                            DataSet dataSet = new DataSet();

                            // Заполняем DataGridView данными из таблицы
                            adapter.Fill(dataSet, selectedTableName);

                            // Привязываем DataGridView к таблице данных
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
            createInsertProcedure(tableName);
            createUpdateProcedure(tableName);
            createSelectProcedure(tableName);
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
                catch
                {
                    // MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}");
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

                    // Получаем информацию о столбцах таблицы
                    List<ColumnInfo> columns = new List<ColumnInfo>();
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

                    if (!ProcedureExists($@"Update{tableName}", connection))
                    {
                        if (columns != null && columns.Count > 0)
                        {
                            // Создаем строку параметров хранимой процедуры на основе столбцов таблицы
                            string parameters = string.Join(", ",
                                columns.Select(col => $"@{col.Name} {GetOdbcType(col.DataType)}").ToArray());

                            // Создаем строку обновления данных в таблице на основе столбцов таблицы
                            string updateStatement =
                                string.Join(", ", columns.Select(col => $"{col.Name} = @{col.Name}").ToArray());

                            // Получаем имя первичного ключа
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

                            // Создаем хранимую процедуру с динамически созданными параметрами
                            string createProcedureQuery = $@"
        CREATE PROCEDURE Update{tableName}
            {parameters},
            @{primaryKey}PK INT
        AS
        BEGIN
            UPDATE {tableName} SET {updateStatement} WHERE {primaryKey} = @{primaryKey}PK
        END;";

                            using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                            {
                                // Добавляем параметры к команде
                                foreach (var column in columns)
                                {
                                    command.Parameters.Add("@" + column.Name,
                                        GetOdbcType(column.DataType)); // Укажите соответствующий тип данных
                                }

                                // Добавляем параметр для первичного ключа
                                command.Parameters.Add("@" + primaryKey, OdbcType.Int);

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

                    // Получаем имя первичного ключа
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
                        // Создаем хранимую процедуру
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

                        // Получаем информацию о столбцах таблицы
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

                        if (columns != null && columns.Count > 0)
                        {
                            // Создаем строку параметров хранимой процедуры на основе столбцов таблицы
                            string parameters = string.Join(", ",
                                columns.Select(col => $"@{col.Name + " " + GetOdbcType(col.DataType).ToString()}")
                                    .ToArray());

                            // Создаем строку вставки данных в таблицу на основе столбцов таблицы
                            string insertStatement = string.Join(", ", columns.Select(col => col.Name).ToArray());

                            // Создаем хранимую процедуру с динамически созданными параметрами
                            string createProcedureQuery = $@"
            CREATE PROCEDURE Insert{tableName}
                {parameters}
            AS
            BEGIN
                INSERT INTO {tableName} ({insertStatement}) VALUES ({string.Join(", ", columns.Select(col => $"@{col.Name}").ToArray())})
            END;";

                            using (OdbcCommand command = new OdbcCommand(createProcedureQuery, connection))
                            {
                                // Добавляем параметры к команде
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

// Метод для преобразования строкового представления типа данных в OdbcType
        private OdbcType GetOdbcType(string dataType)
        {
            // Пример простой логики для преобразования строкового представления типа данных в OdbcType
            // Вам может потребоваться расширить этот метод для поддержки всех возможных типов данных вашей базы данных

            switch (dataType.ToLower())
            {
                case "int":
                    return OdbcType.Int;
                case "varchar":
                case "nchar":
                    return OdbcType.Text;
                case "datetime":
                    return OdbcType.DateTime;
                // Добавьте обработку других типов данных по мере необходимости
                default:
                    // Если тип данных не распознан, возвращаем просто строку
                    return OdbcType.Text;
            }
        }


        private void Input()
        {
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

        private void createSelectProcedure(string tableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    if (!ProcedureExists($@"Select{tableName}", connection))
                    {
                        string createProcedureQuery = $@"
                CREATE PROCEDURE Select{tableName}
                AS
                BEGIN
                    SELECT * FROM {tableName}
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedTableName = comboBox1.SelectedItem.ToString();
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
                        return;
                    }
                }
            }
        }

        // Задайте переменную для хранения имени столбца первичного ключа
        private string primaryKeyColumnName = "";


        private string GetPrimaryKeyColumnName(string tableName)
        {
            // Метод для определения столбца первичного ключа
            string primaryKeyColumnName = "";

            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Выполняем запрос к схеме таблицы, чтобы определить столбец первичного ключа
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
                // Проверяем, является ли измененный столбец первичным ключом
                if (dataGridView1.Columns[e.ColumnIndex].Name != primaryKeyColumnName)
                {
                    // Получаем введенное значение
                    string newValue = e.FormattedValue.ToString();

                    // Добавьте здесь логику валидации значения
                    // Например, проверка на числовой формат или другие ограничения

                    // Если значение недопустимо, отменяем событие
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
                // Проверяем, является ли измененный столбец первичным ключом
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

                // Отменяем изменение значения ячейки, чтобы избежать ошибки
                dataGridView1.CancelEdit();
            }
        }

      private void button4_Click(object sender, EventArgs e)
{
    try
    {
        // Проверяем, что имя столбца первичного ключа было определено
        if (string.IsNullOrEmpty(primaryKeyColumnName))
        {
            MessageBox.Show("Не удалось определить столбец первичного ключа.", "Ошибка", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        string connectionString = "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

        foreach (var primaryKeyValue in changedValues.Keys)
        {
            Dictionary<string, object> updatedValues = new Dictionary<string, object>();

            // Получаем все значения записи по первичному ключу
            Dictionary<string, object> oldValues = GetRecordValuesFromDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue);

            // Добавляем только измененные значения в словарь для передачи в хранимую процедуру
            foreach (var columnName in oldValues.Keys)
            {
                object newValue = changedValues[primaryKeyValue].ContainsKey(columnName) ? changedValues[primaryKeyValue][columnName] : oldValues[columnName];
                updatedValues.Add(columnName, newValue);
            }

            // Вызываем хранимую процедуру Update с переданными значениями
            UpdateRecordInDatabase(selectedTableName, primaryKeyColumnName, primaryKeyValue, updatedValues, connectionString);
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

// Метод для получения значений всех столбцов записи по первичному ключу из базы данных
private Dictionary<string, object> GetRecordValuesFromDatabase(string tableName, string primaryKeyColumnName, object primaryKeyValue)
{
    Dictionary<string, object> values = new Dictionary<string, object>();

    string connectionString = "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

    using (OdbcConnection connection = new OdbcConnection(connectionString))
    {
        connection.Open();
        using (OdbcCommand command = new OdbcCommand($"SELECT * FROM {tableName} WHERE {primaryKeyColumnName} = ?", connection))
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

// Метод для вызова хранимой процедуры Update с переданными значениями
        private void UpdateRecordInDatabase(string tableName, string primaryKeyColumnName, object primaryKeyValue, Dictionary<string, object> updatedValues, string connectionString)
        {
            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
        
                // Создаем параметры для хранимой процедуры
                StringBuilder parameters = new StringBuilder();
                foreach (var columnName in updatedValues.Keys)
                {
                    parameters.Append($"@{columnName} = ?, ");
                }
                // Удаляем лишнюю запятую и пробел в конце строки параметров
                parameters.Length -= 2;
        
                // Составляем запрос для вызова хранимой процедуры
                string query = $"EXEC Update{tableName} @{primaryKeyColumnName}PK = ?, {parameters}";

                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    // Добавляем параметр для первичного ключа
                    OdbcParameter primaryKeyParam = new OdbcParameter("@" + primaryKeyColumnName, primaryKeyValue);
                    command.Parameters.Add(primaryKeyParam);

                    // Добавляем параметры для всех столбцов и их значений
                    foreach (var columnName in updatedValues.Keys)
                    {
                        object columnValue = updatedValues[columnName];
                        OdbcType odbcType = GetOdbcType(GetColumnInfo(tableName, columnName, connectionString).DataType);
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
        using (OdbcCommand command = new OdbcCommand($"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'", connection))
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
            // Проверяем, что индекс строки и столбца допустим
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Получаем объект DataGridViewColumn для текущего столбца
                DataGridViewColumn currentColumn = dataGridView1.Columns[e.ColumnIndex];

                // Проверяем, является ли текущий столбец столбцом с первичным ключом
                if (currentColumn.Name == primaryKeyColumnName)
                {
                    // Устанавливаем свойство ReadOnly для всех ячеек столбца с первичным ключом
                    currentColumn.ReadOnly = true;
                }
                else
                {
                    // Устанавливаем свойство ReadOnly для всех ячеек в остальных столбцах на false
                    currentColumn.ReadOnly = false;
                }
            }
        }


        private bool IsValidValue(string value)
        {
            // Проверяем, что значение не пустое
            if (string.IsNullOrEmpty(value))
                return false;

            // Проверяем, что значение не содержит спецсимволов, которые могут быть использованы для SQL-инъекций
            // В данном примере проверяем, что значение не содержит символы одинарной кавычки (')
            // Вы можете дополнить этот метод для обнаружения других спецсимволов
            if (value.Contains("'"))
                return false;

            // Если значение прошло все проверки, считаем его допустимым
            return true;
        }
    }
}