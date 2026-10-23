using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace DrawPeek;

/// <summary>Reads localization only; never initializes events or generates options.</summary>
internal static class EventOptionPreview
{
    internal static IEnumerable<string> GetLines(EventModel model)
    {
        var table = LocManager.Instance.GetTable(model.LocTable);
        var pageKey = model.InitialDescription.LocEntryKey;
        const string descriptionSuffix = ".description";
        if (!pageKey.EndsWith(descriptionSuffix, StringComparison.Ordinal)) return Array.Empty<string>();
        var prefix = pageKey[..^descriptionSuffix.Length] + ".options.";
        return FormatOptions(table.Keys, table.GetRawText, prefix);
    }

    // Separate text processing from game state so it can be verified without a run.
    internal static string[] FormatOptions(IEnumerable<string> keys, Func<string, string> rawText, string prefix)
    {
        var lines = new List<string>();
        foreach (var titleKey in keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)
                     && k.EndsWith(".title", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal))
        {
            var title = PlainText(rawText(titleKey));
            if (string.IsNullOrWhiteSpace(title)) continue;
            var descriptionKey = titleKey[..^".title".Length] + ".description";
            var description = keys.Contains(descriptionKey) ? PlainText(rawText(descriptionKey)) : "";
            lines.Add("• " + title + (string.IsNullOrWhiteSpace(description) ? "" : " — " + description));
        }
        return lines.ToArray();
    }

    internal static string PlainText(string text)
    {
        // SmartFormat values may require initialized event state. Do not present
        // canonical defaults as actual costs or rewards; leave an explicit marker.
        while (Regex.IsMatch(text, @"\{[^{}]*\}"))
            text = Regex.Replace(text, @"\{[^{}]*\}", "?");
        text = Regex.Replace(text, @"\[/?[^\]\r\n]+\]", "");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
