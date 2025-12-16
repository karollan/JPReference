using JLPTReference.Api.DTOs.Search;
using System.Text;

namespace JLPTReference.Api.Services.Search.Parser;
public class QueryParser : IQueryParser {
    public SearchSpec Parse(string query) {
        query = query.Trim();

        List<SearchToken> tokens = new List<SearchToken>();
        SearchFilters filters = new SearchFilters();

        StringBuilder currentToken = new StringBuilder();
        bool isTag = false;
        bool hasWildcard = false;
        bool isTransliterationblocked = false;
        bool isMultiWord = false;

        for (int i = 0; i < query.Length; i++)
        {
            char c = query[i];

            if (IQueryParser.POSTGRE_WILDCARD_CHARS.Contains(c))
            {
                currentToken.Append('\\');
                currentToken.Append(c);
                continue;
            }

            if (c == IQueryParser.MULTI_WORD_CHAR)
            {
                if (!isMultiWord)
                {
                    isTransliterationblocked = true;
                }
                isMultiWord = !isMultiWord;
                continue;
            }
            if (c == IQueryParser.SINGLE_WILDCARD_CHAR || c == IQueryParser.MULTI_WILDCARD_CHAR)
            {
                hasWildcard = true;
                isTransliterationblocked = true;
                if (c == IQueryParser.MULTI_WILDCARD_CHAR)
                {
                    currentToken.Append('%');
                } else {
                    currentToken.Append('_');
                }
                continue;
            }
            if (c == IQueryParser.BREAK_CHAR && !isMultiWord)
            {
                if (currentToken.Length > 0)
                {
                    FinalizeTokenOrTag(ref filters, tokens, currentToken.ToString(), isTag, hasWildcard, isTransliterationblocked);
                }
                currentToken.Clear();
                hasWildcard = false;
                isTransliterationblocked = false;
                isTag = false;
                continue;
            }
            if (c == IQueryParser.TAG_CHAR && currentToken.Length == 0 && !isMultiWord)
            {
                isTag = true;
                continue;
            }

            currentToken.Append(c);
        }

        if (currentToken.Length > 0)
        {
            FinalizeTokenOrTag(ref filters, tokens, currentToken.ToString(), isTag, hasWildcard, isTransliterationblocked);
        }

        if (tokens.Count == 0)
        {
            // if no tokens were found, we search for all terms
            tokens.Add(new SearchToken {
                RawValue = "%",
                Variants = new (),
                HasWildcard = true,
                TransliterationBlocked = true
            });
        }

        if (filters.Languages is null || filters.Languages.Count == 0)
        {
            filters.Languages = new List<string> { "eng" };
        }

        return new SearchSpec
        {
            Tokens = tokens,
            Filters = filters
        };
    }

    // helper methods
    private void FinalizeTokenOrTag(ref SearchFilters filters, List<SearchToken> tokens, string rawValue, bool isTag, bool hasWildcard, bool isTransliterationblocked)
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

    private void AddTagToTagList(ref SearchFilters filters, string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        string lowerTag = tag.ToLower();
        string[] parts = lowerTag.Split(IQueryParser.TAG_SPLIT_CHAR);
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