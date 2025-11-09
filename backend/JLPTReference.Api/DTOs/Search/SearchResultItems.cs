using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.ProperNoun;
using JLPTReference.Api.DTOs.Kanji;

namespace JLPTReference.Api.DTOs.Search;

public class SearchResultVocabulary
{
    public List<VocabularySummaryDto> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class SearchResultProperNoun
{
    public List<ProperNounSummaryDto> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class SearchResultKanji
{
    public List<KanjiSummaryDto> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}