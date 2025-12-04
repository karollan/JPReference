using JLPTReference.Api.DTOs.ProperNoun;

namespace JLPTReference.Api.Services.Interfaces;

public interface IProperNounService
{
    Task<ProperNounDetailDto> GetProperNounDetailByTermAsync(string term);
}