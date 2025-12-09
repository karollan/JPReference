using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.Parser;
public static class FilterParser
{
    private static readonly Dictionary<string, Action<SearchFilters, string>> _filterActions = new ()
    {
        ["jlpt"] = (filters, value) =>
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int level))
            {
                (filters.JlptLevels ??= new List<int>()).Add(level);
            }
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
            if (!string.IsNullOrEmpty(value))
            {
                ParseStrokeCount(filters, value);
            }
        },
        ["grade"] = (filters, value) =>
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int grade))
            {
                (filters.Grades ??= new List<int>()).Add(grade);
            }
        }
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

    private static void ParseStrokeCount(SearchFilters filters, string value)
    {
        string[] strokeVals = value.Split('-');
        if (strokeVals.Length == 1 && int.TryParse(strokeVals[0], out int singleStroke))
        {
            filters.StrokeCount = new IntRange { Min = singleStroke, Max = singleStroke };
        }
        else if (strokeVals.Length == 2 && int.TryParse(strokeVals[0], out int minStroke) && int.TryParse(strokeVals[1], out int maxStroke))
        {
            filters.StrokeCount = new IntRange { Min = minStroke, Max = maxStroke };
        }
    }
}