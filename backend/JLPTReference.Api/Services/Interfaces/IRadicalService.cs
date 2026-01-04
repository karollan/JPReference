using JLPTReference.Api.DTOs.Radical;

namespace JLPTReference.Api.Services.Interfaces;
public interface IRadicalService
{
    Task<RadicalDetailDto> GetRadicalByLiteralAsync(string literal);
}