using JLPTReference.Api.DTOs.Search;
using Microsoft.OpenApi.Any;

namespace JLPTReference.Api.Services.Search.Parser;

public interface IQueryParser {

    const char BREAK_CHAR = ' ';
    const char MULTI_WILDCARD_CHAR = '*';
    const char SINGLE_WILDCARD_CHAR = '?';
    const char TAG_CHAR = '#';
    const char MULTI_WORD_CHAR = '"';
    const char TAG_SPLIT_CHAR = ':';

    SearchSpec Parse(string query);
    static void FinalizeTokenOrTag(ref SearchFilters filters, List<SearchToken> tokens, string rawValue, bool isTag, bool hasWildcard, bool isTransliterationblocked)
    {
        if (isTag)
        {
            AddTagToTagList(ref filters, rawValue);
        } else 
        {
            tokens.Add(new SearchToken {
                RawValue = rawValue,
                Variants = new (),
                HasWildcard = hasWildcard,
                TransliterationBlocked = isTransliterationblocked
            });
        }
    }
    static void AddTagToTagList(ref SearchFilters filters, string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        string lowerTag = tag.ToLower();
        string[] parts = lowerTag.Split(TAG_SPLIT_CHAR);
        string key = parts[0];
        string value = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        try
        {
            if (FilterParser.TryApplyFilter(filters, key, value))
            {
                return;
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Error applying filter {key}: {ex.Message}");
            return;
        }

        (filters.Tags ??= new List<string>()).Add(tag);
    }
}