using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.DTOs.Vocabulary;
using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Interfaces;

public interface IKanjiService
{
    Task<KanjiDetailDto?> GetByLiteralAsync(string literal);
}