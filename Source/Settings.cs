using System.Text.Json;

namespace DrawPeek;

internal static class Settings
{
    internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlayTheSpire2", ModEntry.Id);
    private static string FilePath => Path.Combine(Folder, "settings.json");
    public static bool Enabled { get; private set; } = true;
    private sealed class Config { public bool Enabled { get; set; } = true; }

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath)) Enabled = JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath))?.Enabled ?? true;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        { ModEntry.Log("Settings load failed; using defaults: " + ex.Message); }
    }

    public static void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Save();
        ModEntry.Log("Enabled=" + enabled);
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(new Config { Enabled = Enabled }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(FilePath + ".tmp", FilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { ModEntry.Log("Settings save failed: " + ex.Message); }
    }
}
