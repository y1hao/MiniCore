using System.Data;
using System.Reflection;

namespace MiniCore.Framework.Data.Internal;

/// <summary>
/// Maps database rows to objects using reflection.
/// </summary>
internal static class ObjectMapper
{
    /// <summary>
    /// Maps a DataRow to an object of the specified type.
    /// </summary>
    public static object MapToObject(Type type, DataRow row)
    {
        var obj = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create instance of {type}");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanWrite)
                continue;

            var columnName = GetColumnName(property);
            if (!row.Table.Columns.Contains(columnName))
                continue;

            var value = row[columnName];
            if (value == DBNull.Value)
            {
                // Set default value for nullable types
                if (IsNullableType(property.PropertyType))
                {
                    property.SetValue(obj, null);
                }
                continue;
            }

            try
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(obj, convertedValue);
            }
            catch
            {
                // Skip properties that can't be converted
            }
        }

        return obj;
    }

    /// <summary>
    /// Maps a DataRow to an object of type T.
    /// </summary>
    public static T MapToObject<T>(DataRow row) where T : class, new()
    {
        return (T)MapToObject(typeof(T), row);
    }

    /// <summary>
    /// Gets the column name for a property (supports [Column] attribute or uses property name).
    /// </summary>
    private static string GetColumnName(PropertyInfo property)
    {
        // For now, just use property name (can be extended to support [Column] attribute)
        return property.Name;
    }

    /// <summary>
    /// Converts a value to the target type.
    /// </summary>
    private static object? ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return value.ToString();

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        return Convert.ChangeType(value, underlyingType);
    }

    /// <summary>
    /// Checks if a type is nullable.
    /// </summary>
    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    /// <summary>
    /// Gets property values from an object as a dictionary for insert/update operations.
    /// </summary>
    public static Dictionary<string, object?> GetPropertyValues(object obj)
    {
        var values = new Dictionary<string, object?>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead)
                continue;

            // Skip navigation properties (for now, we only handle simple properties)
            if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && !property.PropertyType.IsValueType)
                continue;

            var value = property.GetValue(obj);
            values[property.Name] = value ?? DBNull.Value;
        }

        return values;
    }

    /// <summary>
    /// Gets the primary key property name (assumes "Id" convention).
    /// </summary>
    public static string? GetPrimaryKeyPropertyName(Type entityType)
    {
        var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty != null)
            return idProperty.Name;

        // Try to find any property ending with "Id"
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var keyProperty = properties.FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
        return keyProperty?.Name;
    }
}

