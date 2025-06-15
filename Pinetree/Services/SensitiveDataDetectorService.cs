using System.Reflection;
using System.Text.Json;

namespace Pinetree.Services;

public interface ISensitiveDataDetectorService
{
    bool IsSensitiveProperty(string propertyName);
    string MaskSensitiveValue(string value);
    object? MaskSensitiveObject(object? obj);
    string MaskQueryString(string queryString);
}

public class SensitiveDataDetectorService : ISensitiveDataDetectorService
{
    private readonly HashSet<string> _sensitiveKeywords;
    private readonly char _maskChar = '*';
    private readonly int _maskLength = 8;

    public SensitiveDataDetectorService()
    {
        // Define sensitive data keywords - case insensitive
        _sensitiveKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Authentication related
            "password", "pwd", "pass", "secret", "token", "key", "auth", "credential",
            
            // Personal identifiable information
            "email", "mail", "phone", "telephone", "mobile", "ssn", "socialsecurity",
            "passport", "license", "drivinglicense", "nationalid",
            
            // Payment related
            "stripe", "payment", "card", "credit", "debit", "bank", "account",
            "cvv", "cvc", "expiry", "expire", "billing",
            
            // Session and security
            "session", "cookie", "csrf", "xsrf", "api", "bearer",
            
            // Business sensitive
            "salary", "wage", "income", "tax", "revenue", "profit"
        };
    }

    public bool IsSensitiveProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return false;

        return _sensitiveKeywords.Any(keyword => 
            propertyName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // For very short values, mask completely
        if (value.Length <= 3)
            return new string(_maskChar, _maskLength);

        // For longer values, show first and last character
        if (value.Length <= 10)
            return $"{value[0]}{new string(_maskChar, Math.Min(value.Length - 2, _maskLength))}{value[^1]}";

        // For very long values, show first 2 and last 2 characters
        return $"{value[..2]}{new string(_maskChar, _maskLength)}{value[^2..]}";
    }

    public object? MaskSensitiveObject(object? obj)
    {
        if (obj == null)
            return null;

        var type = obj.GetType();

        // Handle primitive types and strings
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal))
        {
            return obj;
        }        // Handle collections
        if (obj is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(MaskSensitiveObject(item));
            }
            return list;
        }        // Handle complex objects - create a masked copy
        var maskedObject = new Dictionary<string, object?>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                {
                    maskedObject[property.Name] = null;
                    continue;
                }

                if (IsSensitiveProperty(property.Name))
                {
                    // Mask sensitive property
                    maskedObject[property.Name] = MaskSensitiveValue(value.ToString() ?? "");
                }
                else
                {
                    // Recursively process non-sensitive properties
                    maskedObject[property.Name] = MaskSensitiveObject(value);
                }
            }
            catch (Exception)
            {
                // In case of any reflection errors, mask the property value
                maskedObject[property.Name] = "[ERROR_READING_PROPERTY]";
            }
        }

        return maskedObject;
    }

    public string MaskQueryString(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return queryString;

        // Remove leading '?' if present
        var cleanQuery = queryString.TrimStart('?');
        var parameters = cleanQuery.Split('&', StringSplitOptions.RemoveEmptyEntries);
        var maskedParameters = new List<string>();

        foreach (var parameter in parameters)
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0];
                var value = parts[1];

                if (IsSensitiveProperty(key))
                {
                    maskedParameters.Add($"{key}={MaskSensitiveValue(value)}");
                }
                else
                {
                    maskedParameters.Add(parameter);
                }
            }
            else
            {
                maskedParameters.Add(parameter);
            }
        }

        return string.Join("&", maskedParameters);
    }
}
