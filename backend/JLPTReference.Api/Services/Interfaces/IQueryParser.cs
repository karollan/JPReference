using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Interfaces;

public interface IQueryParser {
    SearchSpec Parse(string query);
}