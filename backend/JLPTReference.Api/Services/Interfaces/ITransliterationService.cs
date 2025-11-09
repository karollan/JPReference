using WanaKanaNet;

namespace JLPTReference.Api.Services.Interfaces;

public interface ITransliterationService
{
    List<string> GetAllSearchVariants(string text) {
        return new List<string> {
            text,
            ToHiragana(text),
            ToKatakana(text),
            ToRomaji(text)
        };
    }
    string ToHiragana(string text) {
        return WanaKana.ToHiragana(text);
    }
    string ToKatakana(string text) {
        return WanaKana.ToKatakana(text);
    }
    string ToRomaji(string text) {
        return WanaKana.ToRomaji(text);
    }
    bool IsHiragana(string text) {
        return WanaKana.IsHiragana(text);
    }
    bool IsKatakana(string text) {
        return WanaKana.IsKatakana(text);
    }
    bool IsKanji(string text) {
        return WanaKana.IsKanji(text);
    }
}