using Microsoft.AspNetCore.Routing;
using System.Text.RegularExpressions;

public class KebabCaseTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var stringValue = value.ToString();
        if (stringValue == null)
        {
            return null;
        }

        return Regex.Replace(stringValue, "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
    }
}