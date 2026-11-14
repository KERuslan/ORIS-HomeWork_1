using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace MiniHttpServer.ORM;

internal sealed class RowMapper<T> where T : class, new()
{
    private readonly List<(PropertyInfo Property, int Ordinal)> _bindings = new();

    internal RowMapper(DbDataReader reader)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            columns.Add(reader.GetName(ordinal), ordinal);

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.SetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0
                && columns.TryGetValue(property.Name, out int ordinal))
                _bindings.Add((property, ordinal));
        }
    }

    internal T Map(DbDataReader reader)
    {
        var entity = new T();
        foreach (var (property, ordinal) in _bindings)
        {
            Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);
            if (reader.IsDBNull(ordinal))
            {
                if (!property.PropertyType.IsValueType || nullableType is not null)
                    property.SetValue(entity, null);

                continue;
            }

            property.SetValue(entity, ConvertValue(reader.GetValue(ordinal), nullableType ?? property.PropertyType));
        }

        return entity;
    }

    private static object ConvertValue(object value, Type target)
    {
        if (target.IsInstanceOfType(value))
            return value;

        if (target == typeof(Guid) && value is string guid)
            return Guid.Parse(guid);

        if (target.IsEnum)
        {
            if (value is string name)
                return Enum.Parse(target, name, ignoreCase: true);

            var number = Convert.ChangeType(value, Enum.GetUnderlyingType(target), CultureInfo.InvariantCulture);
            return Enum.ToObject(target, number);
        }

        return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }
}
