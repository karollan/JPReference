namespace JLPTReference.Api.Services.Search.Ranking;

public class VocabularyMatchInfo
{
    public Guid VocabularyId { get; set; }
    public MatchQuality BestMatchQuality { get; set; } = MatchQuality.None;
    public MatchLocation MatchLocations { get; set; } = MatchLocation.None;
    public bool IsCommon { get; set; }
    public int? JlptLevel { get; set; }
    public int SenseCount { get; set; }
    public int MatchedTextLength { get; set; }
    public double RelevanceScore { get; set; }
}

