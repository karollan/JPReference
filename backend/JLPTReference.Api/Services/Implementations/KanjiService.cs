using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.Search;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Services.Implementations;

public class KanjiService : IKanjiService {
    private readonly IKanjiRepository _kanjiRepository;

    public KanjiService(IKanjiRepository kanjiRepository) {
        _kanjiRepository = kanjiRepository;
    }

    public async Task<KanjiDetailDto?> GetByLiteralAsync(string literal)
    {
        return await _kanjiRepository.GetKanjiDetailByLiteralAsync(literal);
    }
}