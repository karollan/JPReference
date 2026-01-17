namespace JLPTReference.Api.Repositories.Search.Ranking;

public class ProperNounRanker : IProperNounRanker
{
    public void ComputeScores(IEnumerable<ProperNounMatchInfo> matchInfos, ProperNounRankingProfile profile)
    {
        foreach (var info in matchInfos)
        {
            info.RelevanceScore = ComputeScore(info, profile);
        }
    }

    private double ComputeScore(ProperNounMatchInfo info, ProperNounRankingProfile profile)
    {
        double score = 0;
        
        // Primary score: match quality
        score += (int)info.BestMatchQuality * profile.MatchQualityMultiplier;
        
        // Location bonuses
        if (info.MatchLocations.HasFlag(MatchLocation.Kanji))
            score += profile.KanjiMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Kana))
            score += profile.KanaMatchBonus;
        
        if (info.MatchLocations.HasFlag(MatchLocation.Translation))
            score += profile.TranslationMatchBonus;
        
        // Shorter text bonus (more specific matches)
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

