using JLPTReference.Api.DTOs.ProperNoun;

namespace JLPTReference.Api.Services.Interfaces;

public interface IProperNounService
{
    Task<ProperNounDetailDto> GetByIdAsync(Guid id);
    Task<List<ProperNounSummaryDto>> SearchAsync(string query, ProperNounSearchFilters filters, int page, int pageSize);
}