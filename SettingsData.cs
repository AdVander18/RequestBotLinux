using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Styling;

namespace RequestBotLinux
{
    public class SettingsData
    {
        [JsonConverter(typeof(ThemeVariantConverter))]
        public ThemeVariant Theme { get; set; } = ThemeVariant.Light;
        public string BotToken { get; set; } = string.Empty;
    }
    public class ThemeVariantConverter : JsonConverter<ThemeVariant>
    {
        public override ThemeVariant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                return value == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Key", out var key))
                    {
                        string keyValue = key.GetString();
                        return keyValue == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
                    }
                }
            }

            return ThemeVariant.Light;
        }

        public override void Write(Utf8JsonWriter writer, ThemeVariant value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value == ThemeVariant.Dark ? "Dark" : "Light");
        }
    }

}
