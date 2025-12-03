using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.Repositories.Interfaces;
public interface IVocabularyRepository {
    Task<VocabularyDetailDto> GetVocabularyDetailByTermAsync(string term);
}