namespace JLPTReference.Api.Repositories.Search.Ranking;

public class VocabularyRankingProfile
{
    public double MatchQualityMultiplier { get; set; } = 1.0;
    public int KanaMatchBonus { get; set; } = 15;
    public int KanjiMatchBonus { get; set; } = 10;
    public int GlossMatchBonus { get; set; } = 5;
    public int FirstSenseMatchBonus { get; set; } = 50;
    public int CommonWordBonus { get; set; } = 30;
    public int HasJlptBonus { get; set; } = 20;
    public int JlptLevelMultiplier { get; set; } = 0;
    public int ManySensesPenalty { get; set; } = 0;
    public int ManySensesThreshold { get; set; } = 5;
    public int ShorterTextBonus { get; set; } = 0;
    public int ShorterTextReferenceLength { get; set; } = 10;

    public static VocabularyRankingProfile Default => new();
    
    public static VocabularyRankingProfile Learner => new()
    {
        CommonWordBonus = 50,
        HasJlptBonus = 40,
        JlptLevelMultiplier = 10,
        FirstSenseMatchBonus = 60
    };
}

