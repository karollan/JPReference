namespace JLPTReference.Api.Services.Search.Ranking;

public enum MatchQuality
{
    None = 0,
    Wildcard = 100,
    Contains = 200,
    Prefix = 500,
    Exact = 1000
}

[Flags]
public enum MatchLocation
{
    None = 0,
    Kana = 1,
    Kanji = 2,
    Gloss = 4,
    FirstSense = 8,
    Literal = 16,
    Reading = 32,
    Meaning = 64,
    Translation = 128
}

