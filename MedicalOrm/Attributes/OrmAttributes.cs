
// Refleksija čita ove atribute za vrijeme izvođenja 

namespace MedicalOrm.Attributes;


// [Table("patients")]
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name; 
}


// [Column("first_name")]
[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;         
    public bool IsNullable { get; set; } = true; 
    public bool IsUnique { get; set; } = false;  
    public string? Default { get; set; }         
    public string? DbType { get; set; }         
}


// [PrimaryKey(autoIncrement: true)]
[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyAttribute(bool autoIncrement = true) : Attribute
{
    public bool AutoIncrement { get; } = autoIncrement; 
}


// [ForeignKey("doctor_id")]
[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute(string columnName) : Attribute
{
    public string ColumnName { get; } = columnName;
}


// [Navigate("doctor_id")]
[AttributeUsage(AttributeTargets.Property)]
public class NavigateAttribute(string foreignKeyColumn) : Attribute
{
    public string ForeignKeyColumn { get; } = foreignKeyColumn;
}


// Govori ORM-u da ignoriira ovaj property
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreAttribute : Attribute { }
