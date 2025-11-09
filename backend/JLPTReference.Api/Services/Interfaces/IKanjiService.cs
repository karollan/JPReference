using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.Services.Interfaces;

public interface IKanjiService
{
    Task<KanjiDetailDto> GetByIdAsync(Guid id);
    Task<KanjiDetailDto> GetByLiteralAsync(string literal);
    Task<List<KanjiSummaryDto>> SearchAsync(string query, KanjiSearchFilters filters, int page, int pageSize);
    Task<List<VocabularySummaryDto>> GetVocabularyUsingKanjiAsync(Guid kanjiId, int limit);
}