using MegaCrit.Sts2.Core.Localization;

namespace DrawPeek;

/// <summary>Uses the game locale, never the operating system language.</summary>
internal static class L10n
{
    internal static string Language => LocManager.Instance?.Language ?? "eng";

    internal static bool IsChinese(string language) => language is "zhs" or "zht";

    internal static string Text(string chinese, string english) =>
        IsChinese(Language) ? chinese : english;
}
