namespace Procedures
{
    public class ForeignKeyInfo
    {
        public string ColumnName { get; set; }
        public string ReferenceTableName { get; set; }
        public string ReferenceColumnName { get; set; }

        public ForeignKeyInfo(string columnName, string referenceTableName, string referenceColumnName)
        {
            ColumnName = columnName;
            ReferenceTableName = referenceTableName;
            ReferenceColumnName = referenceColumnName;
        }
    }
}