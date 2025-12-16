namespace JLPTReference.Api.Services.Search.Ranking;

public class VocabularyRanker : IVocabularyRanker
{
    public void ComputeScores(IEnumerable<VocabularyMatchInfo> matchInfos, VocabularyRankingProfile profile)
    {
        foreach (var info in matchInfos)
        {
            info.RelevanceScore = ComputeScore(info, profile);
        }
    }

    private double ComputeScore(VocabularyMatchInfo info, VocabularyRankingProfile profile)
    {
        double score = 0;
        
        score += (int)info.BestMatchQuality * profile.MatchQualityMultiplier;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Kana))
            score += profile.KanaMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Kanji))
            score += profile.KanjiMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Gloss))
            score += profile.GlossMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.FirstSense))
            score += profile.FirstSenseMatchBonus;
        
        if (info.IsCommon)
            score += profile.CommonWordBonus;
        
        if (info.JlptLevel.HasValue)
        {
            score += profile.HasJlptBonus;
            score += profile.JlptLevelMultiplier * (6 - info.JlptLevel.Value);
        }
        
        if (profile.ManySensesPenalty > 0 && info.SenseCount > profile.ManySensesThreshold)
        {
            score -= profile.ManySensesPenalty * (info.SenseCount - profile.ManySensesThreshold);
        }
        
        if (profile.ShorterTextBonus > 0 && info.MatchedTextLength > 0)
        {
            var lengthRatio = Math.Min(1.0, (double)profile.ShorterTextReferenceLength / info.MatchedTextLength);
            score += profile.ShorterTextBonus * lengthRatio;
        }
        
        return score;
    }

    public MatchQuality DetermineMatchQuality(string pattern, string text, bool hasUserWildcard)
    {
        return SearchPatternUtils.DetermineMatchQuality(pattern, text, hasUserWildcard);
    }
}

