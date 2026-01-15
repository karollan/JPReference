using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Repositories.Search.Variants;

public interface IVariantGenerator
{
    void PopulateVariants(SearchSpec spec);
}