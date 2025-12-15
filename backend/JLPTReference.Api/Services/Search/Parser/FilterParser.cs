using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.Parser;
public static class FilterParser
{
    private static readonly Dictionary<string, Action<SearchFilters, string>> _filterActions = new ()
    {
        ["jlpt"] = (filters, value) =>
        {
            ParseIntRange(value, (intRange) => filters.JlptLevels = intRange);
        },
        ["pos"] = (filters, value) =>
        {
            if (!string.IsNullOrEmpty(value))
            {
                (filters.PartOfSpeech ??= new List<string>()).Add(value);
            }
        },
        ["common"] = (filters, value) =>
        {
            filters.CommonOnly = true;
        },
        ["stroke"] = (filters, value) =>
        {
            ParseIntRange(value, (intRange) => filters.StrokeCount = intRange);
        },
        ["grade"] = (filters, value) =>
        {
            ParseIntRange(value, (intRange) => filters.Grades = intRange);
        },
        ["freq"] = (filters, value) =>
        {
            ParseIntRange(value, (intRange) => filters.Frequency = intRange);
        },
    };

    public static bool TryApplyFilter(SearchFilters filters, string key, string value)
    {
        if (_filterActions.TryGetValue(key, out var action))
        {
            action(filters, value);
            return true;
        }
        return false;
    }

    private static void ParseIntRange(string value, Action<IntRange?> setRange)
    {
        if (string.IsNullOrEmpty(value)) return;

        string[] intVals = value.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (intVals.Length == 1 && int.TryParse(intVals[0], out int singleInt))
        {
            setRange(new IntRange { Min = singleInt, Max = singleInt });
        }
        else if (intVals.Length == 2 && int.TryParse(intVals[0], out int minInt) && int.TryParse(intVals[1], out int maxInt))
        {
            setRange(new IntRange { Min = minInt, Max = maxInt });
        }
    }
}