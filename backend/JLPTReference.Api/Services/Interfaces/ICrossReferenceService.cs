using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Radical;

namespace JLPTReference.Api.Services.Interfaces;

public interface ICrossReferenceService
{
    Task<List<VocabularySummaryDto>> GetVocabularyForKanjiAsync(Guid kanjiId);
    Task<List<KanjiSummaryDto>> GetKanjiInVocabularyAsync(Guid vocabularyId);
    Task<List<RadicalSummaryDto>> GetRadicalsForKanjiAsync(Guid kanjiId);
}