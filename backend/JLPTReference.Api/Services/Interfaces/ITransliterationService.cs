using WanaKanaNet;

namespace JLPTReference.Api.Services.Interfaces;

public interface ITransliterationService
{
    static List<string> GetAllSearchVariants(string text) {
        HashSet<string> variants = new HashSet<string>(StringComparer.Ordinal)
        {
            text
        };

        if (IsRomaji(text))
        {
            variants.Add(ToHiragana(text));
            variants.Add(ToKatakana(text));
        } else if (IsHiragana(text))
        {
            variants.Add(ToKatakana(text));
            variants.Add(ToRomaji(text));
        }
        else if (IsKatakana(text))
        {
            variants.Add(ToHiragana(text));
            variants.Add(ToRomaji(text));
        }
        else if (IsKanji(text))
        {
            variants.Add(text);
        }

        return variants.ToList();
    }
    static string ToHiragana(string text) {
        return WanaKana.ToHiragana(text);
    }
    static string ToKatakana(string text) {
        return WanaKana.ToKatakana(text);
    }
    static string ToRomaji(string text) {
        return WanaKana.ToRomaji(text);
    }

    static bool IsRomaji(string text) {
        return WanaKana.IsRomaji(text);
    }
    static bool IsHiragana(string text) {
        return WanaKana.IsHiragana(text);
    }
    static  bool IsKatakana(string text) {
        return WanaKana.IsKatakana(text);
    }
    static bool IsKanji(string text) {
        return WanaKana.IsKanji(text);
    }
}