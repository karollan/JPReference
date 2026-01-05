using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.DTOs.Kanji;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Services.Implementations;
public class RadicalService : IRadicalService {
    private readonly IRadicalRepository _radicalRepository;

    public RadicalService(IRadicalRepository radicalRepository) {
        _radicalRepository = radicalRepository;
    }

    public Task<List<RadicalSummaryDto>> GetRadicalsListAsync() {
        return _radicalRepository.GetRadicalsListAsync();
    }

    public Task<RadicalDetailDto> GetRadicalByLiteralAsync(string literal) {
        return _radicalRepository.GetRadicalByLiteralAsync(literal);
    }

    public Task<RadicalSearchResultDto> SearchKanjiByRadicalsAsync(List<Guid> radicalIds) {
        return _radicalRepository.SearchKanjiByRadicalsAsync(radicalIds);
    }
}