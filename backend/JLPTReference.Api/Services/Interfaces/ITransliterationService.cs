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
            string hiragana = ToHiragana(text);
            if (IsHiragana(hiragana))
            {
                variants.Add(hiragana);
            }
            string katakana = ToKatakana(text);
            if (IsKatakana(katakana))
            {
                variants.Add(katakana);
            }
        } else if (IsHiragana(text))
        {
            string katakana = ToKatakana(text);
            if (IsKatakana(katakana))
            {
                variants.Add(katakana);
            }
            string romaji = ToRomaji(text);
            if (IsRomaji(romaji))
            {
                variants.Add(romaji);
            }
        }
        else if (IsKatakana(text))
        {
            string hiragana = ToHiragana(text);
            if (IsHiragana(hiragana))
            {
                variants.Add(hiragana);
            }
            string romaji = ToRomaji(text);
            if (IsRomaji(romaji))
            {
                variants.Add(romaji);
            }
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