using STS2RitsuLib.Utils;

namespace DrawPeek;

/// <summary>Uses the game locale, never the operating system language.</summary>
internal static class L10n
{
    internal static string Language => I18N.ResolveCurrentLanguageCode();

    internal static bool IsChinese(string language) => language is "zhs" or "zht";

    internal static string Text(string chinese, string english) =>
        IsChinese(Language) ? chinese : english;
}
