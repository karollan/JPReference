using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Kanji;
namespace JLPTReference.Api.Repositories.Interfaces;
public interface IRadicalRepository
{
    Task<RadicalDetailDto?> GetRadicalByLiteralAsync(string literal);
    Task<List<RadicalSummaryDto>> GetRadicalsListAsync();
    Task<RadicalSearchResultDto> SearchKanjiByRadicalsAsync(List<Guid> radicalIds);
}