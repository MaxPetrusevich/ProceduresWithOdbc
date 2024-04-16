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

        public InsertForm(OdbcConnection conn)
        {
            InitializeComponent();
            connection = conn;
            // Добавим обработчик события для кнопки "Добавить"
            buttonInsert.Click += ButtonInsert_Click;

            // Загрузим список таблиц при загрузке формы
            LoadTableNames();
            comboBoxTables.SelectedItem = Form1.selectedTableName;
            if (!string.IsNullOrEmpty(Form1.selectedTableName))
            {
                LoadTableColumns(Form1.selectedTableName);
            }
        }

        private void ButtonInsert_Click(object sender, EventArgs e)
        {
            // Проверим, все ли поля заполнены
            if (string.IsNullOrEmpty(comboBoxTables.SelectedItem?.ToString()))
            {
                MessageBox.Show("Пожалуйста, выберите таблицу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Вызовем процедуру вставки с введенными данными
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

            // Очистим панель от предыдущих элементов
            panelInput.Controls.Clear();

            // Создадим текстовые поля для каждого столбца таблицы
            foreach (var column in columns)
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

        private void InsertRecord(string tableName)
        {
            string connectionString =
                "Driver={SQL Server};Server=MAKSIM\\MS2012SERVER;Database=Books;Uid=test;Pwd=123456789lab;";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
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

                // Составляем запрос на основе столбцов таблицы
                string parameterNames = string.Join(", ", columns.Select(col => $"@{col.Name}").ToArray());
                string parameterValues = string.Join(", ", columns.Select(col => "?").ToArray());

                // Составляем запрос для вызова процедуры вставки
                string query = $"EXEC Insert{tableName} {parameterValues}";

                using (OdbcCommand command = new OdbcCommand(query, connection))
                {
                    foreach (var column in columns)
                    {
                        // Получаем соответствующий тип данных ODBC
                        OdbcType odbcType = GetOdbcType(column.DataType);

                        // Добавляем параметры к команде
                        command.Parameters.Add("@" + column.Name, odbcType);
                    }

                    // Заполняем значения параметров из контролов формы
                    foreach (Control control in panelInput.Controls)
                    {
                        if (control is TextBox textBox && textBox.Name.StartsWith("textBox"))
                        {
                            string columnName = textBox.Name.Substring("textBox".Length);
                            var parameter = command.Parameters["@" + columnName];
                            parameter.Value = textBox.Text;
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


        private void comboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            // При выборе таблицы загружаем её столбцы и создаем текстовые поля для ввода
            string selectedTableName = comboBoxTables.SelectedItem.ToString();
            LoadTableColumns(selectedTableName);
        }
    }
}