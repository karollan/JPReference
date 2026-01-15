namespace JLPTReference.Api.Repositories.Search.Ranking;

public class KanjiMatchInfo
{
    public Guid KanjiId { get; set; }
    public MatchQuality BestMatchQuality { get; set; } = MatchQuality.None;
    public MatchLocation MatchLocations { get; set; } = MatchLocation.None;
    public int? Frequency { get; set; }
    public int? JlptLevel { get; set; }
    public int? Grade { get; set; }
    public int MatchedTextLength { get; set; }
    public double RelevanceScore { get; set; }
}

