using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Search.QueryBuilder;

namespace JLPTReference.Api.Services.Implementations;

public class KanjiService : IKanjiService {
    private readonly IKanjiRepository _kanjiRepository;
    private readonly IKanjiSearchService _kanjiSearchService;

    public KanjiService(IKanjiRepository kanjiRepository, IKanjiSearchService kanjiSearchService) {
        _kanjiRepository = kanjiRepository;
        _kanjiSearchService = kanjiSearchService;
    }

    public Task<KanjiDetailDto> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<KanjiDetailDto> GetByLiteralAsync(string literal)
    {
        return await _kanjiRepository.GetKanjiDetailByLiteralAsync(literal);
    }

    public Task<List<VocabularySummaryDto>> GetVocabularyUsingKanjiAsync(Guid kanjiId, int limit)
    {
        throw new NotImplementedException();
    }

    public async Task<List<KanjiSummaryDto>> SearchAsync(string query, KanjiSearchFilters filters, int page, int pageSize)
    {
        // For classic search, we might still want to use the repository or search service
        // But the user didn't ask to fix this now.
        throw new NotImplementedException();
    }

    public async Task<SearchResultKanji> SearchByRadicalsAsync(List<string> radicals, int page, int pageSize)
    {
        return await _kanjiSearchService.SearchByRadicalsAsync(radicals, pageSize, page);
    }
}