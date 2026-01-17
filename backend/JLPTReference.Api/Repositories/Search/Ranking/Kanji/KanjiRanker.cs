namespace JLPTReference.Api.Repositories.Search.Ranking;

public class KanjiRanker : IKanjiRanker
{
    public void ComputeScores(IEnumerable<KanjiMatchInfo> matchInfos, KanjiRankingProfile profile)
    {
        foreach (var info in matchInfos)
        {
            info.RelevanceScore = ComputeScore(info, profile);
        }
    }

    private double ComputeScore(KanjiMatchInfo info, KanjiRankingProfile profile)
    {
        double score = 0;
        
        // Primary score: match quality
        score += (int)info.BestMatchQuality * profile.MatchQualityMultiplier;
        
        // Location bonuses
        if (info.MatchLocations.HasFlag(MatchLocation.Literal))
            score += profile.LiteralMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Reading))
            score += profile.ReadingMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Meaning))
            score += profile.MeaningMatchBonus;
        
        // Frequency bonus: lower frequency = more common = higher score
        if (info.Frequency.HasValue)
        {
            score += profile.HasFrequencyBonus;
            
            if (info.Frequency.Value <= profile.MaxFrequencyForBonus)
            {
                // Linear scale: frequency 1 = full bonus, frequency MaxFrequencyForBonus = no bonus
                var frequencyRatio = 1.0 - ((double)info.Frequency.Value / profile.MaxFrequencyForBonus);
                score += profile.FrequencyBonus * frequencyRatio;
            }
        }
        
        // JLPT bonus
        if (info.JlptLevel.HasValue)
        {
            score += profile.HasJlptBonus;
            // N1 = 5 * (6-1) = 25, N5 = 5 * (6-5) = 5
            score += profile.JlptLevelMultiplier * (6 - info.JlptLevel.Value);
        }
        
        // Grade bonus: lower grade = learned earlier = more common
        if (info.Grade.HasValue)
        {
            score += profile.HasGradeBonus;
            // Grade 1 = 3 * (10-1) = 27, Grade 9 = 3 * (10-9) = 3
            score += profile.GradeLevelMultiplier * (10 - info.Grade.Value);
        }
        
        return score;
    }

    public MatchQuality DetermineMatchQuality(string pattern, string text, bool hasUserWildcard)
    {
        return SearchPatternUtils.DetermineMatchQuality(pattern, text, hasUserWildcard);
    }
}

