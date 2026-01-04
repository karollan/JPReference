using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;

namespace JLPTReference.Api.Services.Implementations;
public class RadicalService : IRadicalService {
    private readonly IRadicalRepository _radicalRepository;

    public RadicalService(IRadicalRepository radicalRepository) {
        _radicalRepository = radicalRepository;
    }

    public Task<RadicalDetailDto> GetRadicalByLiteralAsync(string literal) {
        return _radicalRepository.GetRadicalByLiteralAsync(literal);
    }
}