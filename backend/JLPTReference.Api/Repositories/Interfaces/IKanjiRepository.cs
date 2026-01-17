using JLPTReference.Api.DTOs.Kanji;

namespace JLPTReference.Api.Repositories.Interfaces;
public interface IKanjiRepository {
    Task<KanjiDetailDto?> GetKanjiDetailByLiteralAsync(string literal);
}