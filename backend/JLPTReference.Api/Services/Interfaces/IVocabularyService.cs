using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.Services.Interfaces;

public interface IVocabularyService
{
    Task<VocabularyDetailDto> GetByIdAsync(Guid id);
    Task<List<VocabularySummaryDto>> SearchAsync(string query, VocabularySearchFilters filters, int page, int pageSize);
    Task<List<VocabularySummaryDto>> GetByJlptLevelAsync(int level, int page, int pageSize);
}