using JLPTReference.Api.DTOs.ProperNoun;

namespace JLPTReference.Api.Repositories.Interfaces;

public interface IProperNounRepository {
    public Task<ProperNounDetailDto> GetProperNounDetailByTermAsync(string term);
}