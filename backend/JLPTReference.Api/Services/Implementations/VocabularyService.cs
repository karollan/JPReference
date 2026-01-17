using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Services.Interfaces;
namespace JLPTReference.Api.Services.Implementations;
public class VocabularyService : IVocabularyService {

    private readonly IVocabularyRepository _vocabularyRepository;
    public VocabularyService(IVocabularyRepository vocabularyRepository) {
        _vocabularyRepository = vocabularyRepository;
    }

    public async Task<VocabularyDetailDto?> GetVocabularyDetailByTermAsync(string term) {
        return await _vocabularyRepository.GetVocabularyDetailByTermAsync(term);
    }
}