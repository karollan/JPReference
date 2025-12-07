using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.DTOs.Search;
using System.Text;
using System.Text.RegularExpressions;

namespace JLPTReference.Api.Services.Implementations;
public class QueryParser : IQueryParser {
    public SearchSpec Parse(string query) {
        var tokens = new List<SearchToken>();
        var hashtags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Regex pattern = new Regex("\"([^\"]*)\"|(#\\w+)|([^\\ ]+)", RegexOptions.Compiled);
        MatchCollection matches = pattern.Matches(query);
        foreach (Match match in matches) {
            if (match.Groups[1].Success) {
                tokens.Add(new SearchToken {
                    RawValue = match.Groups[1].Value,
                    HasWildcard = match.Groups[1].Value.Contains('*') || match.Groups[1].Value.Contains('?'),
                    TransliterationBlocked = true,
                    Variants = new List<string>(),
                });
            }
            else if (match.Groups[2].Success) {
                string tag = match.Groups[2].Value.Substring(1);
                hashtags[tag] = tag;
            }
            else if (match.Groups[3].Success) {
                string raw = match.Groups[3].Value;
                if (raw.StartsWith('#')) {
                    string tag = raw.Substring(1);
                    hashtags[tag] = tag;
                }
                else {
                    tokens.Add(new SearchToken {
                        RawValue = raw,
                        HasWildcard = raw.Contains('*') || raw.Contains('?'),
                        TransliterationBlocked = false,
                        Variants = new List<string>(),
                    });
                }
            }
        }

        var filters = new SearchFilters{
            JlptLevels = hashtags.ContainsKey("jlpt") ? hashtags["jlpt"].Split(',').Select(int.Parse).ToList() : null,
            PartOfSpeech = hashtags.ContainsKey("pos") ? hashtags["pos"].Split(',').ToList() : null,
            CommonOnly = hashtags.ContainsKey("common") ? true : null,
            Tags = hashtags.Keys.ToList(),
            StrokeCount = hashtags.ContainsKey("stroke") ? new IntRange {
                Min = int.Parse(hashtags["stroke"].Split(':')[1].Split('-')[0]),
                Max = int.Parse(hashtags["stroke"].Split(':')[1].Split('-')[1])
            } : null,
            Grades = hashtags.ContainsKey("grade") ? hashtags["grade"].Split(',').Select(int.Parse).ToList() : null,
        };

        return new SearchSpec{
            Tokens = tokens,
            Filters = filters,
        };
    }
}