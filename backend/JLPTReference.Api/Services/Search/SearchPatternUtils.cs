using System.Text;
using System.Text.RegularExpressions;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Search.Ranking;

namespace JLPTReference.Api.Services.Search;

/// <summary>
/// Shared utilities for search pattern operations.
/// Used by query builders, rankers, and repositories.
/// </summary>
public static class SearchPatternUtils
{
    /// <summary>
    /// Generates SQL LIKE patterns from search tokens.
    /// Combines token variants with a trailing % for prefix matching.
    /// </summary>
    public static List<string> GetPatterns(List<SearchToken>? tokens)
    {
        if (tokens == null || tokens.Count == 0) 
            return new List<string>();

        return tokens
            .Select(t => t.TransliterationBlocked ? new[] { t.RawValue } : t.Variants.ToArray())
            .Aggregate(
                new List<string> { "" },
                (acc, variants) =>
                    acc.SelectMany(prefix => variants.Select(v => prefix + v + '%')).ToList()
            )
            .ToList();
    }

    /// <summary>
    /// Generates SQL LIKE patterns grouped by token for per-token AND matching.
    /// Each inner list contains variant patterns for one token.
    /// This enables "all words must match somewhere" logic rather than
    /// "all words must match in same field" logic.
    /// </summary>
    public static List<List<string>> GetPatternsPerToken(List<SearchToken>? tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return new List<List<string>>();
        
        return tokens.Select(t => 
            (t.TransliterationBlocked ? new[] { t.RawValue } : t.Variants.ToArray())
                .Select(v => v + "%")
                .ToList()
        ).ToList();
    }

    /// <summary>
    /// Gets the count of variants for each token.
    /// Used alongside flattened patterns array to reconstruct token groupings in SQL.
    /// For tokens with multiple variants (e.g., "toku" -> ["とく", "トク", "toku"]),
    /// only ONE variant needs to match (OR within token), but ALL tokens must match (AND across tokens).
    /// </summary>
    public static int[] GetTokenVariantCounts(List<SearchToken>? tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return Array.Empty<int>();
        
        return tokens.Select(t => 
            t.TransliterationBlocked ? 1 : t.Variants.Count
        ).ToArray();
    }

    /// <summary>
    /// Generates combined phrase patterns for searching sequences in a single field.
    /// e.g. ["to", "wake", "up"] -> ["to%wake%up%"]
    /// Handles variants: ["coach", "toku"] -> ["coach%とく%", "coach%トク%", "coach%toku%"]
    /// </summary>
    public static List<string> GetCombinedPatterns(List<SearchToken>? tokens)
    {
        if (tokens == null || tokens.Count < 2) 
            return new List<string>();

        // Cartesian product of variants
        return tokens
            .Select(t => t.TransliterationBlocked ? new[] { t.RawValue } : t.Variants.ToArray())
            .Aggregate(
                new List<string> { "" },
                (acc, variants) =>
                    acc.SelectMany(prefix => variants.Select(v => prefix == "" ? v : prefix + "%" + v)).ToList()
            )
            .Select(p => p + "%") // Add trailing wildcard
            .ToList();
    }

    /// <summary>
    /// Converts a SQL LIKE pattern to a .NET regex pattern.
    /// Handles % (multi-char), _ (single-char), and escaped characters.
    /// </summary>
    public static string LikePatternToRegex(string likePattern)
    {
        var result = new StringBuilder();
        result.Append('^');
        
        for (int i = 0; i < likePattern.Length; i++)
        {
            char c = likePattern[i];
            
            if (c == '\\' && i + 1 < likePattern.Length)
            {
                // Escaped character - treat next char literally
                char next = likePattern[i + 1];
                result.Append(Regex.Escape(next.ToString()));
                i++;
            }
            else if (c == '%')
            {
                result.Append(".*");
            }
            else if (c == '_')
            {
                result.Append('.');
            }
            else
            {
                result.Append(Regex.Escape(c.ToString()));
            }
        }
        
        result.Append('$');
        return result.ToString();
    }

    /// <summary>
    /// Removes escape sequences from a LIKE pattern to get the raw search term.
    /// </summary>
    public static string UnescapeLikePattern(string pattern)
    {
        var result = new StringBuilder();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '\\' && i + 1 < pattern.Length)
            {
                result.Append(pattern[i + 1]);
                i++;
            }
            else if (pattern[i] != '%' && pattern[i] != '_')
            {
                result.Append(pattern[i]);
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Determines the match quality between a SQL LIKE pattern and a text value.
    /// </summary>
    public static MatchQuality DetermineMatchQuality(string pattern, string text, bool hasUserWildcard)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text))
            return MatchQuality.None;

        // Convert SQL LIKE pattern to regex for matching
        var regexPattern = LikePatternToRegex(pattern);
        
        if (!Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase))
            return MatchQuality.None;
        
        // If user used wildcards, we can't determine exact/prefix quality as confidently
        if (hasUserWildcard)
            return MatchQuality.Wildcard;
        
        // Remove the trailing % that we auto-add for prefix search
        var searchTerm = pattern.TrimEnd('%');
        searchTerm = UnescapeLikePattern(searchTerm);
        
        // Check for exact match (case-insensitive)
        if (string.Equals(searchTerm, text, StringComparison.OrdinalIgnoreCase))
            return MatchQuality.Exact;
        
        // Check for prefix match
        if (text.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            return MatchQuality.Prefix;
        
        // Check for contains match
        if (text.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            return MatchQuality.Contains;
        
        return MatchQuality.Wildcard;
    }
}

