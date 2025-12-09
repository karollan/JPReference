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

            if (c == IQueryParser.MULTI_WORD_CHAR)
            {
                isMultiWord = !isMultiWord;
                continue;
            }
            if (c == IQueryParser.SINGLE_WILDCARD_CHAR || c == IQueryParser.MULTI_WILDCARD_CHAR)
            {
                hasWildcard = true;
                isTransliterationblocked = true;
            }
            if (c == IQueryParser.BREAK_CHAR && !isMultiWord)
            {
                if (currentToken.Length > 0)
                {
                    IQueryParser.FinalizeTokenOrTag(ref filters, tokens, currentToken.ToString(), isTag, hasWildcard, isTransliterationblocked);
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
            IQueryParser.FinalizeTokenOrTag(ref filters, tokens, currentToken.ToString(), isTag, hasWildcard, isTransliterationblocked);
        }

        return new SearchSpec
        {
            Tokens = tokens,
            Filters = filters
        };
    }
}