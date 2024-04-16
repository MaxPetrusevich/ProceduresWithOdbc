namespace Procedures
{
    public class ColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }

        public ColumnInfo(string name, string dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }
}