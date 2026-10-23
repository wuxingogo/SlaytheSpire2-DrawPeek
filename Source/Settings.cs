using System.Text.Json;

namespace DrawPeek;

internal static class Settings
{
    internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlayTheSpire2", ModEntry.Id);
    private static string FilePath => Path.Combine(Folder, "settings.json");
    public static bool Enabled { get; private set; } = true;
    public static bool MapPredictionEnabled { get; private set; } = true;
    public static bool EventPredictionEnabled { get; private set; } = true;
    private sealed class Config
    {
        public bool Enabled { get; set; } = true;
        // Missing fields in pre-1.5.1 settings inherit the former master toggle.
        public bool? MapPredictionEnabled { get; set; }
        public bool? EventPredictionEnabled { get; set; }
    }

    public static void Load()
    {
        try
        {
            var config = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath)) ?? new Config()
                : new Config();
            Enabled = config.Enabled;
            MapPredictionEnabled = config.MapPredictionEnabled ?? config.Enabled;
            EventPredictionEnabled = config.EventPredictionEnabled ?? config.Enabled;
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
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(new Config {
                Enabled = Enabled,
                MapPredictionEnabled = MapPredictionEnabled,
                EventPredictionEnabled = EventPredictionEnabled
            }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(FilePath + ".tmp", FilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { ModEntry.Log("Settings save failed: " + ex.Message); }
    }

    public static void SetMapPredictionEnabled(bool enabled)
    {
        MapPredictionEnabled = enabled;
        Save();
        ModEntry.Log("MapPredictionEnabled=" + enabled);
    }

    public static void SetEventPredictionEnabled(bool enabled)
    {
        EventPredictionEnabled = enabled;
        Save();
        ModEntry.Log("EventPredictionEnabled=" + enabled);
    }
}
