using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;

namespace RequestBotLinux
{
    public static class SettingsManager
    {
        private const string ThemeKey = "SelectedTheme";
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RequestBot",
            "settings.json");

        public static ThemeVariant LoadTheme()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null && settings.TryGetValue(ThemeKey, out var themeValue))
                    {
                        return themeValue == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading theme: {ex.Message}");
            }

            return ThemeVariant.Light;
        }

        public static void SaveTheme(ThemeVariant theme)
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    [ThemeKey] = theme == ThemeVariant.Dark ? "Dark" : "Light"
                };

                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving theme: {ex.Message}");
            }
        }
        public static SettingsData LoadSettings()
        {
            if (!File.Exists(SettingsPath))
                return new SettingsData();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
        }

        public static void SaveSettings(SettingsData data)
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            Directory.CreateDirectory(directory ?? throw new InvalidOperationException());
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(SettingsPath, json);
        }

    }
    public class SettingsData
    {
        public ThemeVariant Theme { get; set; } = ThemeVariant.Dark; // ИЗМЕНИТЬ ТЕМУ А ТО ОНО ПО ДЕФОЛТУ ДРУГОЕ НУУУ
        public string BotToken { get; set; } = string.Empty;
    }
}
