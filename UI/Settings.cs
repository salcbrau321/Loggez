using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Loggez.UI
{
    public static class Settings
    {
        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Loggez", "settings.json");

        public static string ExternalOpener { get; set; } = "notepad.exe";
        public static List<string> SupportedExtensions { get; set; } = new List<string> { ".log", ".txt" };

        static Settings()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var cfg  = JsonSerializer.Deserialize<SettingsDto>(json);
                    ExternalOpener       = cfg.ExternalOpener;
                    SupportedExtensions  = cfg.SupportedExtensions;
                }
            }
            catch
            {
  
            }
        }

        public static void Save()
        {
            try
            {
                var dto = new SettingsDto
                {
                    ExternalOpener      = ExternalOpener,
                    SupportedExtensions = SupportedExtensions
                };
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {

            }
        }

        private class SettingsDto
        {
            public string ExternalOpener { get; set; } = "notepad.exe";
            public List<string> SupportedExtensions { get; set; } = new List<string> { ".log", ".txt" };
        }
    }
}
