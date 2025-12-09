using JLPTReference.Api.DTOs.Search;

namespace JLPTReference.Api.Services.Search.Variants;
public class VariantGenerator : IVariantGenerator, ITransliterationService
{
    public void PopulateVariants(SearchSpec spec)
    {
        foreach (var token in spec.Tokens)
        {
            token.Variants = ITransliterationService.GetAllSearchVariants(token.RawValue);
        }
    }
}