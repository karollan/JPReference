using JLPTReference.Api.DTOs.Search;
using Microsoft.OpenApi.Any;

namespace JLPTReference.Api.Services.Search.Parser;

public interface IQueryParser {

    const char BREAK_CHAR = ' ';
    const char MULTI_WILDCARD_CHAR = '*';
    const char SINGLE_WILDCARD_CHAR = '?';
    const char TAG_CHAR = '#';
    const char MULTI_WORD_CHAR = '"';
    const char TAG_SPLIT_CHAR = ':';
    const string POSTGRE_WILDCARD_CHARS = "%_";

    SearchSpec Parse(string query);
}