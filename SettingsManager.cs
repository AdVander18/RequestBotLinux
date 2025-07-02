using System;
using System.IO;
using System.Text.Json;

namespace RequestBotLinux
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RequestBot",
        "settings.json");

        public static SettingsData LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new SettingsData();

                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return new SettingsData();
            }
        }

        public static void SaveSettings(SettingsData data)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                Directory.CreateDirectory(directory ?? throw new InvalidOperationException());
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
