using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Services.Interfaces;
public class KanjiService : IKanjiService {
    private readonly IKanjiRepository _kanjiRepository;

    public KanjiService(IKanjiRepository kanjiRepository) {
        _kanjiRepository = kanjiRepository;
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

    public Task<List<KanjiSummaryDto>> SearchAsync(string query, KanjiSearchFilters filters, int page, int pageSize)
    {
        throw new NotImplementedException();
    }
}