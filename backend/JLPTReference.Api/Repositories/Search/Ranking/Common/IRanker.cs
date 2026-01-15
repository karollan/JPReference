namespace JLPTReference.Api.Repositories.Search.Ranking;

public interface IRanker<TMatchInfo, TProfile>
    where TMatchInfo : class
    where TProfile : class
{
    void ComputeScores(IEnumerable<TMatchInfo> matchInfos, TProfile profile);
    
    MatchQuality DetermineMatchQuality(string pattern, string text, bool hasUserWildcard);
}

