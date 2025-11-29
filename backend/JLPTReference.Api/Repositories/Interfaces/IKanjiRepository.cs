using JLPTReference.Api.DTOs.Kanji;

public interface IKanjiRepository {
    Task<KanjiDetailDto> GetKanjiDetailByLiteralAsync(string literal);
}