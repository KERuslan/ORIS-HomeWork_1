using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace MiniHttpServer.ORM;

internal static class SqlCommands
{
    private static readonly Regex Identifier = new(@"\A[A-Za-z_][A-Za-z0-9_]*\z");

    internal static string Quote(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 128 || !Identifier.IsMatch(name))
            throw new ArgumentException("Ожидается простое имя таблицы или столбца SQL Server.", nameof(name));

        return $"[{name}]";
    }

    internal static SqlCommand Select(string table) => new($"SELECT * FROM {Quote(table)};");

    internal static SqlCommand SelectById(string table, int id) =>
        WithId($"SELECT * FROM {Quote(table)} WHERE [Id] = @id;", id);

    internal static SqlCommand Delete(string table, int id) =>
        WithId($"DELETE FROM {Quote(table)} WHERE [Id] = @id;", id);

    internal static SqlCommand Insert(string table, object entity)
    {
        string target = Quote(table);
        var values = GetValues(entity, insert: true);
        if (values.Length == 0)
            return new SqlCommand($"INSERT INTO {target} DEFAULT VALUES;");

        string columns = string.Join(", ", values.Select(value => value.Column));
        string parameters = string.Join(", ", values.Select(value => value.Parameter.ParameterName));
        return WithValues($"INSERT INTO {target} ({columns}) VALUES ({parameters});", values);
    }

    internal static SqlCommand Update(string table, int id, object entity)
    {
        string target = Quote(table);
        var values = GetValues(entity, insert: false);
        if (values.Length == 0)
            throw new ArgumentException("В модели нет полей для обновления.", nameof(entity));

        string assignments = string.Join(", ", values.Select(value => $"{value.Column} = {value.Parameter.ParameterName}"));
        var command = WithValues($"UPDATE {target} SET {assignments} WHERE [Id] = @id;", values);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        return command;
    }

    private static (string Column, SqlParameter Parameter)[] GetValues(object entity, bool insert)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var properties = entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true
                && property.GetIndexParameters().Length == 0);

        return properties.Where(property => IsWritable(property, insert))
            .Select((property, index) => (Quote(property.Name), CreateParameter(property, entity, index)))
            .ToArray();
    }

    private static bool IsWritable(PropertyInfo property, bool insert)
    {
        var generated = property.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption;
        if (generated is DatabaseGeneratedOption.Computed or DatabaseGeneratedOption.Identity)
            return false;

        bool isId = string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase);
        return !isId || (insert && generated == DatabaseGeneratedOption.None);
    }

    private static SqlParameter CreateParameter(PropertyInfo property, object entity, int index)
    {
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        object? value = property.GetValue(entity);
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
            if (value is not null)
                value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        SqlDbType sqlType = type == typeof(Guid) ? SqlDbType.UniqueIdentifier
            : type == typeof(byte[]) ? SqlDbType.VarBinary
            : type == typeof(TimeSpan) ? SqlDbType.Time
            : type == typeof(DateTimeOffset) ? SqlDbType.DateTimeOffset
            : Type.GetTypeCode(type) switch
            {
                TypeCode.String or TypeCode.Char => SqlDbType.NVarChar,
                TypeCode.Boolean => SqlDbType.Bit,
                TypeCode.Byte => SqlDbType.TinyInt,
                TypeCode.Int16 => SqlDbType.SmallInt,
                TypeCode.Int32 => SqlDbType.Int,
                TypeCode.Int64 => SqlDbType.BigInt,
                TypeCode.Single => SqlDbType.Real,
                TypeCode.Double => SqlDbType.Float,
                TypeCode.Decimal => SqlDbType.Decimal,
                TypeCode.DateTime => SqlDbType.DateTime2,
                _ => throw new ArgumentException($"Свойство {property.Name} имеет неподдерживаемый тип {type.Name}.")
            };

        return new SqlParameter($"@p{index}", sqlType)
        {
            Value = value is char character ? character.ToString() : value ?? DBNull.Value
        };
    }

    private static SqlCommand WithValues(string sql, (string Column, SqlParameter Parameter)[] values)
    {
        var command = new SqlCommand(sql);
        command.Parameters.AddRange(values.Select(value => value.Parameter).ToArray());
        return command;
    }

    private static SqlCommand WithId(string sql, int id)
    {
        var command = new SqlCommand(sql);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        return command;
    }
}
