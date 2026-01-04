using JLPTReference.Api.DTOs.Radical;
namespace JLPTReference.Api.Repositories.Interfaces;
public interface IRadicalRepository
{
    Task<RadicalDetailDto> GetRadicalByLiteralAsync(string literal);
}