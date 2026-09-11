using System;
using System.IO;
using System.Text.Json;
using MenubarDock.Diagnostics;
using MenubarDock.Models;

namespace MenubarDock.Core
{
    public class ConfigurationManager
    {
        private static readonly string _configPath;
        private MenuBarSettings _settings = new();

        public MenuBarSettings Settings => _settings;

        static ConfigurationManager()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "MenubarDock");
            Directory.CreateDirectory(dir);
            _configPath = Path.Combine(dir, "settings.json");
        }

        public static string ConfigDirectory => Path.GetDirectoryName(_configPath)!;

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var loaded = JsonSerializer.Deserialize<MenuBarSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loaded != null)
                    {
                        _settings = loaded;
                        Logger.Info("Menubar settings loaded.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to load settings, using defaults.", ex);
            }

            _settings = new MenuBarSettings();
            SaveSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings.", ex);
            }
        }
    }
}
