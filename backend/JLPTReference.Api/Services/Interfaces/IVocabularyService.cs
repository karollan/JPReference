using JLPTReference.Api.DTOs.Vocabulary;

namespace JLPTReference.Api.Services.Interfaces;

public interface IVocabularyService
{
    Task<VocabularyDetailDto?> GetVocabularyDetailByTermAsync(string term);
}