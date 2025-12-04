using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.DTOs.ProperNoun;

namespace JLPTReference.Api.Services.Implementations;
public class ProperNounService : IProperNounService {
    private readonly IProperNounRepository _properNounRepository;

    public ProperNounService(IProperNounRepository properNounRepository) {
        _properNounRepository = properNounRepository;
    }

    public async Task<ProperNounDetailDto> GetProperNounDetailByTermAsync(string term) {
        return await _properNounRepository.GetProperNounDetailByTermAsync(term);
    }
}