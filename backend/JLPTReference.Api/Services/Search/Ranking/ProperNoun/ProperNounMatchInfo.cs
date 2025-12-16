namespace JLPTReference.Api.Services.Search.Ranking;

public class ProperNounMatchInfo
{
    public Guid ProperNounId { get; set; }
    public MatchQuality BestMatchQuality { get; set; } = MatchQuality.None;
    public MatchLocation MatchLocations { get; set; } = MatchLocation.None;
    public int MatchedTextLength { get; set; }
    public double RelevanceScore { get; set; }
}

