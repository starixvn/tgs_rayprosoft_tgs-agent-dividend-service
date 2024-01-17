using System.Collections;

namespace Tgs.Agent.Dividend.Client;

public static class ClientPath
{
    public static string AppendWithProperties(string path, object obj)
    {
        return Append(path, obj);
    }

    public static string PathAppendWithProperties(this string path, object obj)
    {
        return Append(path, obj);
    }

    private static string Append(string path, object obj)
    {
        var last = obj.GetType().GetProperties().Last();

        return obj.GetType()
            .GetProperties()
            .Aggregate(path,
                (current, propertyInfo) =>
                {
                    var value = propertyInfo.GetValue(obj);

                    if (propertyInfo.PropertyType == typeof(DateTimeOffset) ||
                        propertyInfo.PropertyType == typeof(DateTimeOffset?))
                        value = ((DateTimeOffset?)value)?.ToUniversalTime()
                            .ToString("u");

                    if (propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
                        value = ((DateTime?)value)?.ToUniversalTime()
                            .ToString("u");

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        if (value is null) return $"{current}";

                        var query = string.Empty;
                        var enumerable = (IEnumerable)value;

                        query = enumerable.Cast<object>()
                            .Aggregate(query,
                                (current1, itme) => current1 + $"{propertyInfo.Name}={itme}&");

                        return $"{current}{query}";
                    }

                    return last.Equals(propertyInfo)
                        ? $"{current}{propertyInfo.Name}={value}"
                        : $"{current}{propertyInfo.Name}={value}&";
                });
    }
}