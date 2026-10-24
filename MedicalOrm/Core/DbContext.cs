
// upravlja konekcijama, generira SQL, izvršava upite.


using System.Reflection;
using MedicalOrm.Attributes;
using Npgsql;

namespace MedicalOrm.Core;

public abstract class DbContext : IDisposable
{
    // Konekcija na PostgreSQL
    private readonly NpgsqlConnection _connection;

    // prati promjene nad učitanim objektima
    
    private readonly Dictionary<int, Dictionary<string, object?>> _changeTracker = new();

    
    protected DbContext(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }

    
    // (Code First)
    // Čita atribute refleksijom i gradi CREATE TABLE SQL
    
    public void EnsureCreated<T>() where T : class, new()
    {
        var type = typeof(T);
        var tableName = GetTableName(type); 
        var props = type.GetProperties();

        var columns = new List<string>();
        var primaryKeyCol = "";

        foreach (var prop in props)
        {
            
            if (prop.GetCustomAttribute<IgnoreAttribute>() != null) continue;
            if (prop.GetCustomAttribute<NavigateAttribute>() != null) continue;

            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>();
            var colName = colAttr?.Name ?? ToSnakeCase(prop.Name); 
            var dbType = colAttr?.DbType ?? MapCSharpTypeToSql(prop.PropertyType);

            var colDef = $"\"{colName}\" {dbType}";

            if (pkAttr != null)
            {
                
                colDef = pkAttr.AutoIncrement
                    ? $"\"{colName}\" SERIAL PRIMARY KEY"
                    : $"\"{colName}\" INT PRIMARY KEY";
                primaryKeyCol = colName;
            }
            else
            {
               
                if (colAttr?.IsNullable == false) colDef += " NOT NULL";
                
                if (colAttr?.IsUnique == true) colDef += " UNIQUE";
                
                if (colAttr?.Default != null) colDef += $" DEFAULT {colAttr.Default}";
            }

            columns.Add(colDef);
        }

        var sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", columns)});";
        ExecuteNonQuery(sql);
        Console.WriteLine($"[ORM] Tablica '{tableName}' osigurana.");
    }

    
    // Koristi refleksiju 
    public T Insert<T>(T entity) where T : class, new()
    {
        var type = typeof(T);
        var tableName = GetTableName(type);
        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>()?.AutoIncrement != true
                     && p.GetCustomAttribute<IgnoreAttribute>() == null
                     && p.GetCustomAttribute<NavigateAttribute>() == null)
            .ToList();

        
        var colNames = props.Select(p =>
            $"\"{p.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(p.Name)}\"");
        var paramNames = props.Select((_, i) => $"@p{i}");

        
        var pkProp = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        var pkColName = pkProp?.GetCustomAttribute<ColumnAttribute>()?.Name
                        ?? ToSnakeCase(pkProp?.Name ?? "id");

        var sql = $"INSERT INTO \"{tableName}\" ({string.Join(",", colNames)}) " +
                  $"VALUES ({string.Join(",", paramNames)}) RETURNING \"{pkColName}\"";

        using var cmd = new NpgsqlCommand(sql, _connection);
        for (int i = 0; i < props.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", props[i].GetValue(entity) ?? DBNull.Value);

       
        var newId = cmd.ExecuteScalar();
        if (pkProp != null && newId != null)
            pkProp.SetValue(entity, Convert.ChangeType(newId, pkProp.PropertyType));

        // Dodaj u change tracker
        TrackEntity(entity);
        return entity;
    }

    
    // Dohvati sve redove kao listu objekata
    
    public List<T> GetAll<T>() where T : class, new()
    {
        var sql = $"SELECT * FROM \"{GetTableName(typeof(T))}\"";
        return ExecuteQuery<T>(sql);
    }

    
    // Dohvati jedan objekt po primarnom ključu
    
    public T? GetById<T>(int id) where T : class, new()
    {
        var type = typeof(T);
        var pkProp = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        var pkCol = pkProp?.GetCustomAttribute<ColumnAttribute>()?.Name
                    ?? ToSnakeCase(pkProp?.Name ?? "id");

        var sql = $"SELECT * FROM \"{GetTableName(type)}\" WHERE \"{pkCol}\" = @id";
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@id", id);

        var results = MapReader<T>(cmd.ExecuteReader());
        var entity = results.FirstOrDefault();
        if (entity != null) TrackEntity(entity); 
        return entity;
    }

    
    // Filtriraj po uvjetu (lambda expression)
    
    public List<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        where T : class, new()
    {
        
        var visitor = new ExpressionToSqlVisitor();
        visitor.Visit(predicate.Body);
        var whereClause = visitor.GetSql();
        var parameters = visitor.GetParameters();

        var sql = $"SELECT * FROM \"{GetTableName(typeof(T))}\" WHERE {whereClause}";
        using var cmd = new NpgsqlCommand(sql, _connection);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        return MapReader<T>(cmd.ExecuteReader());
    }

    
    // Uspoređuje trenutno stanje s originalnim snapshotom
    
    public void SaveChanges<T>(T entity) where T : class, new()
    {
        var key = entity.GetHashCode();
        if (!_changeTracker.TryGetValue(key, out var original))
        {
            
            UpdateFull(entity);
            return;
        }

        var type = typeof(T);
        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() == null
                     && p.GetCustomAttribute<IgnoreAttribute>() == null
                     && p.GetCustomAttribute<NavigateAttribute>() == null)
            .ToList();

        // Pronađi samo promijenjene kolone
        var changedCols = new List<(string col, object? val)>();
        foreach (var prop in props)
        {
            var colName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(prop.Name);
            var current = prop.GetValue(entity);
            if (!original.TryGetValue(colName, out var orig) || !Equals(current, orig))
                changedCols.Add((colName, current));
        }

        if (!changedCols.Any()) return;

        
        var setParts = changedCols.Select((c, i) => $"\"{c.col}\" = @u{i}");
        var pkProp = type.GetProperties()
            .First(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        var pkCol = pkProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(pkProp.Name);

        var sql = $"UPDATE \"{GetTableName(type)}\" SET {string.Join(",", setParts)} WHERE \"{pkCol}\" = @pkval";
        using var cmd = new NpgsqlCommand(sql, _connection);
        for (int i = 0; i < changedCols.Count; i++)
            cmd.Parameters.AddWithValue($"@u{i}", changedCols[i].val ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pkval", pkProp.GetValue(entity) ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        // Osvježi snapshot u trackeru
        TrackEntity(entity);
        Console.WriteLine($"[ORM] Ažurirano {changedCols.Count} kolona za {type.Name}.");
    }

    
    // Briši objekt iz baze
    
    public void Delete<T>(T entity) where T : class, new()
    {
        var type = typeof(T);
        var pkProp = type.GetProperties()
            .First(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        var pkCol = pkProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(pkProp.Name);
        var pkVal = pkProp.GetValue(entity);

        var sql = $"DELETE FROM \"{GetTableName(type)}\" WHERE \"{pkCol}\" = @id";
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@id", pkVal ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        // Ukloni iz trackera
        _changeTracker.Remove(entity.GetHashCode());
    }

    
    // EAGER LOAD
    
    public T? Include<T, TRelated>(int id,
        System.Linq.Expressions.Expression<Func<T, TRelated?>> navigationProperty)
        where T : class, new()
        where TRelated : class, new()
    {
        var entity = GetById<T>(id);
        if (entity == null) return null;

        
        var navPropInfo = (PropertyInfo)((System.Linq.Expressions.MemberExpression)navigationProperty.Body).Member;
        var navAttr = navPropInfo.GetCustomAttribute<NavigateAttribute>();
        if (navAttr == null) return entity;

        
        var fkProp = typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name == navAttr.ForeignKeyColumn
                              || ToSnakeCase(p.Name) == navAttr.ForeignKeyColumn);
        if (fkProp == null) return entity;

        var fkVal = fkProp.GetValue(entity);
        if (fkVal == null) return entity;

        
        var related = GetById<TRelated>(Convert.ToInt32(fkVal));
        navPropInfo.SetValue(entity, related);

        return entity;
    }

    // Generiraj i izvrši migracije
    // Uspoređuje DB shemu s C# klasama
    
    public void RunMigration<T>() where T : class, new()
    {
        var type = typeof(T);
        var tableName = GetTableName(type);

        
        var checkSql = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}'";
        using var checkCmd = new NpgsqlCommand(checkSql, _connection);
        var count = Convert.ToInt32(checkCmd.ExecuteScalar());

        if (count == 0)
        {
            
            Console.WriteLine($"[MIGRATION] Kreiram tablicu '{tableName}'...");
            EnsureCreated<T>();
            LogMigration($"create_table_{tableName}");
            return;
        }

        
        var existingCols = GetExistingColumns(tableName);
        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null
                     && p.GetCustomAttribute<NavigateAttribute>() == null);

        foreach (var prop in props)
        {
            var colName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(prop.Name);
            if (!existingCols.Contains(colName))
            {
               
                var dbType = prop.GetCustomAttribute<ColumnAttribute>()?.DbType
                             ?? MapCSharpTypeToSql(prop.PropertyType);
                var alterSql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{colName}\" {dbType}";
                ExecuteNonQuery(alterSql);
                Console.WriteLine($"[MIGRATION] Dodana kolona '{colName}' u '{tableName}'.");
                LogMigration($"add_column_{tableName}_{colName}");
            }
        }
    }

    // Spremi zapis o migraciji
    private void LogMigration(string name)
    {
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ""__migrations"" 
                          (id SERIAL PRIMARY KEY, name TEXT, applied_at TIMESTAMP DEFAULT NOW())");
        using var cmd = new NpgsqlCommand(
            "INSERT INTO \"__migrations\" (name) VALUES (@n) ON CONFLICT DO NOTHING", _connection);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.ExecuteNonQuery();
    }

   

   
    private HashSet<string> GetExistingColumns(string tableName)
    {
        var sql = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'";
        var cols = new HashSet<string>();
        using var cmd = new NpgsqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) cols.Add(reader.GetString(0));
        return cols;
    }

    
    internal static string GetTableName(Type type)
    {
        var attr = type.GetCustomAttribute<TableAttribute>();
        return attr?.Name ?? ToSnakeCase(type.Name) + "s";
    }

    
    internal static string MapCSharpTypeToSql(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.Name switch
        {
            "Int32" => "INT",
            "Int64" => "BIGINT",
            "String" => "VARCHAR(255)",
            "Boolean" => "BOOLEAN",
            "DateTime" => "TIMESTAMP WITH TIME ZONE",
            "DateOnly" => "DATE",
            "Decimal" => "DECIMAL(18,2)",
            "Double" => "FLOAT",
            "Single" => "REAL",
            _ => "TEXT"
        };
    }

    
    internal static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }

    
    internal void ExecuteNonQuery(string sql)
    {
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.ExecuteNonQuery();
    }

   
    private void UpdateFull<T>(T entity) where T : class, new()
    {
        var type = typeof(T);
        var props = type.GetProperties()
            .Where(p => p.GetCustomAttribute<PrimaryKeyAttribute>() == null
                     && p.GetCustomAttribute<IgnoreAttribute>() == null
                     && p.GetCustomAttribute<NavigateAttribute>() == null)
            .ToList();

        var pkProp = type.GetProperties()
            .First(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);
        var pkCol = pkProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(pkProp.Name);

        var setParts = props.Select((p, i) =>
            $"\"{p.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(p.Name)}\" = @u{i}");

        var sql = $"UPDATE \"{GetTableName(type)}\" SET {string.Join(",", setParts)} WHERE \"{pkCol}\" = @pkval";
        using var cmd = new NpgsqlCommand(sql, _connection);
        for (int i = 0; i < props.Count; i++)
            cmd.Parameters.AddWithValue($"@u{i}", props[i].GetValue(entity) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pkval", pkProp.GetValue(entity) ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    
    private List<T> ExecuteQuery<T>(string sql) where T : class, new()
    {
        using var cmd = new NpgsqlCommand(sql, _connection);
        return MapReader<T>(cmd.ExecuteReader());
    }

    
    private List<T> MapReader<T>(NpgsqlDataReader reader) where T : class, new()
    {
        var type = typeof(T);
        var result = new List<T>();
        var props = type.GetProperties();

        
        var colMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in props)
        {
            if (prop.GetCustomAttribute<NavigateAttribute>() != null) continue;
            var colName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(prop.Name);
            colMap[colName] = prop;
        }

        while (reader.Read())
        {
            var obj = new T();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var colName = reader.GetName(i);
                if (!colMap.TryGetValue(colName, out var prop)) continue;
                var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                if (val != null)
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    
                    if (targetType == typeof(DateOnly))
                    {
                        DateOnly dateVal;
                        if (val is DateTime dt)
                            dateVal = DateOnly.FromDateTime(dt);
                        else
                            dateVal = DateOnly.Parse(val.ToString()!);
                        prop.SetValue(obj, dateVal);
                    }
                    else
                    {
                        prop.SetValue(obj, Convert.ChangeType(val, targetType));
                    }
                }
            }
            result.Add(obj);
        }
        reader.Close();
        return result;
    }

    
    private void TrackEntity<T>(T entity) where T : class, new()
    {
        var type = typeof(T);
        var snapshot = new Dictionary<string, object?>();
        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute<NavigateAttribute>() != null) continue;
            var colName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? ToSnakeCase(prop.Name);
            snapshot[colName] = prop.GetValue(entity);
        }
        _changeTracker[entity.GetHashCode()] = snapshot;
    }

    
    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
