using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.Variants;

public interface IVariantGenerator
{
    void PopulateVariants(SearchSpec spec);
}