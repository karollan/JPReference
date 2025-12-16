namespace JLPTReference.Api.Services.Search.Ranking;

public class KanjiRankingProfile
{
    public double MatchQualityMultiplier { get; set; } = 1.0;
    
    public int LiteralMatchBonus { get; set; } = 20;
    public int ReadingMatchBonus { get; set; } = 10;
    public int MeaningMatchBonus { get; set; } = 5;
    
    /// <summary>
    /// Bonus for having a frequency rank (common kanji)
    /// </summary>
    public int HasFrequencyBonus { get; set; } = 20;
    
    /// <summary>
    /// Max bonus for frequency ranking. Lower frequency = more common = higher score.
    /// Score = FrequencyBonus * (1 - frequency/MaxFrequencyForBonus)
    /// </summary>
    public int FrequencyBonus { get; set; } = 50;
    
    /// <summary>
    /// Frequency values above this get no bonus (rare kanji)
    /// </summary>
    public int MaxFrequencyForBonus { get; set; } = 2500;
    
    /// <summary>
    /// Bonus for having a JLPT level assigned
    /// </summary>
    public int HasJlptBonus { get; set; } = 30;
    
    /// <summary>
    /// Multiplier for JLPT level (N1 gets more points than N5)
    /// </summary>
    public int JlptLevelMultiplier { get; set; } = 0;
    
    /// <summary>
    /// Bonus for having a grade assigned (Jouyou kanji)
    /// </summary>
    public int HasGradeBonus { get; set; } = 25;
    
    /// <summary>
    /// Multiplier for grade level (lower grade = learned earlier = more common)
    /// Score = GradeLevelMultiplier * (10 - grade)
    /// </summary>
    public int GradeLevelMultiplier { get; set; } = 3;

    public static KanjiRankingProfile Default => new();
    
    public static KanjiRankingProfile Learner => new()
    {
        HasJlptBonus = 50,
        JlptLevelMultiplier = 10,
        HasGradeBonus = 40,
        GradeLevelMultiplier = 5
    };
}

