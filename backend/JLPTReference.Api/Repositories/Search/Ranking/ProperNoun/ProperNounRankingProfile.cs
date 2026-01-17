namespace JLPTReference.Api.Repositories.Search.Ranking;

public class ProperNounRankingProfile
{
    public double MatchQualityMultiplier { get; set; } = 1.0;
    
    public int KanjiMatchBonus { get; set; } = 10;
    public int KanaMatchBonus { get; set; } = 15;
    public int TranslationMatchBonus { get; set; } = 5;
    
    /// <summary>
    /// Bonus for shorter matched text (more specific)
    /// </summary>
    public int ShorterTextBonus { get; set; } = 0;
    public int ShorterTextReferenceLength { get; set; } = 10;

    public static ProperNounRankingProfile Default => new();
}

