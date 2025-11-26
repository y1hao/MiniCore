using System.Globalization;
using System.Reflection;
using MiniCore.Framework.Configuration.Abstractions;

namespace MiniCore.Framework.Configuration;

/// <summary>
/// Extension methods for configuration classes.
/// </summary>
public static class ConfigurationExtensions
{
    // Note: GetSection is already defined on IConfiguration interface, so we don't need an extension method for it.

    /// <summary>
    /// Gets the value with the specified key and converts it to type T.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="key">The key.</param>
    /// <returns>The converted value.</returns>
    public static T? GetValue<T>(this IConfiguration configuration, string key)
    {
        return GetValue(configuration, key, default(T));
    }

    /// <summary>
    /// Gets the value with the specified key and converts it to type T.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="configuration">The configuration.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The converted value.</returns>
    public static T GetValue<T>(this IConfiguration configuration, string key, T defaultValue)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var value = configuration[key];
        if (value == null)
        {
            return defaultValue;
        }

        return ConvertValue<T>(value);
    }

    /// <summary>
    /// Binds the configuration instance to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The type of the new instance to bind.</typeparam>
    /// <param name="configuration">The configuration instance to bind.</param>
    /// <returns>The new instance of T if successful, default(T) otherwise.</returns>
    public static T? Bind<T>(this IConfiguration configuration) where T : class, new()
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var instance = new T();
        Bind(configuration, instance);
        return instance;
    }

    /// <summary>
    /// Attempts to bind the configuration instance to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The type of the new instance to bind.</typeparam>
    /// <param name="configuration">The configuration instance to bind.</param>
    /// <param name="instance">The new instance of T.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TryBind<T>(this IConfiguration configuration, out T? instance) where T : class, new()
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        instance = new T();
        Bind(configuration, instance);
        return true;
    }

    /// <summary>
    /// Binds the configuration instance to the instance of type T.
    /// </summary>
    /// <typeparam name="T">The type of the instance to bind.</typeparam>
    /// <param name="configuration">The configuration instance to bind.</param>
    /// <param name="instance">The instance to bind.</param>
    public static void Bind<T>(this IConfiguration configuration, T instance) where T : class
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        BindInstance(instance.GetType(), instance, configuration, string.Empty);
    }

    private static void BindInstance(Type type, object instance, IConfiguration config, string configKey)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid))
        {
            var value = config[configKey];
            if (value != null)
            {
                var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                SetPropertyValue(instance, type, convertedValue);
            }
            return;
        }

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
        {
            // Arrays and lists are not fully supported in this minimal implementation
            return;
        }

        // Bind properties
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            var propertyType = property.PropertyType;
            var propertyName = property.Name;
            var propertyConfigKey = string.IsNullOrEmpty(configKey) ? propertyName : ConfigurationPath.Combine(configKey, propertyName);

            if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType == typeof(decimal) || 
                propertyType == typeof(DateTime) || propertyType == typeof(TimeSpan) || propertyType == typeof(Guid) ||
                (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                var value = config[propertyConfigKey];
                if (value != null)
                {
                    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                    var convertedValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                    property.SetValue(instance, convertedValue);
                }
            }
            else if (propertyType.IsClass)
            {
                var propertyValue = property.GetValue(instance);
                if (propertyValue == null)
                {
                    propertyValue = Activator.CreateInstance(propertyType);
                    if (propertyValue != null)
                    {
                        property.SetValue(instance, propertyValue);
                        BindInstance(propertyType, propertyValue, config, propertyConfigKey);
                    }
                }
                else
                {
                    BindInstance(propertyType, propertyValue, config, propertyConfigKey);
                }
            }
        }
    }

    private static void SetPropertyValue(object instance, Type type, object? value)
    {
        // This is a simplified version - in a real implementation, you'd need to handle
        // properties more carefully. For now, we'll use reflection to set properties.
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.CanWrite && property.PropertyType == type)
            {
                property.SetValue(instance, value);
                break;
            }
        }
    }

    private static T ConvertValue<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default(T)!;
        }

        var type = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
        {
            return (T)(object)value;
        }

        if (underlyingType.IsEnum)
        {
            return (T)Enum.Parse(underlyingType, value, ignoreCase: true);
        }

        return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }
}

