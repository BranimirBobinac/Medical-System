
// Parsira C# lambda expression u SQL WHERE klauzulu.


using System.Linq.Expressions;
using System.Reflection;
using MedicalOrm.Attributes;

namespace MedicalOrm.Core;

public class ExpressionToSqlVisitor : ExpressionVisitor
{
    // Gradi SQL string
    private readonly System.Text.StringBuilder _sql = new();

  
    private readonly Dictionary<string, object?> _params = new();
    private int _paramCount = 0;

    // Vrati generirani SQL
    public string GetSql() => _sql.ToString();

    // Vrati parametre za NpgsqlCommand
    public Dictionary<string, object?> GetParameters() => _params;

    
    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sql.Append('(');
        Visit(node.Left);              

        // Prevedi C# u SQL operator
        var op = node.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",
            _ => throw new NotSupportedException($"Operator {node.NodeType} nije podržan.")
        };

        _sql.Append(op);
        Visit(node.Right);              
        _sql.Append(')');
        return node;
    }

    
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression?.NodeType == ExpressionType.Parameter)
        {
            
            var prop = (PropertyInfo)node.Member;
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var colName = colAttr?.Name ?? DbContext.ToSnakeCase(prop.Name);
            _sql.Append($"\"{colName}\"");
        }
        else
        {
           
            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            AddParam(value);
        }
        return node;
    }

        protected override Expression VisitConstant(ConstantExpression node)
    {
        AddParam(node.Value);
        return node;
    }

    
    private void AddParam(object? value)
    {
        var paramName = $"@p{_paramCount++}";
        _params[paramName] = value;
        _sql.Append(paramName);
    }
}
