using System;
using System.IO;
using System.Text.Json;

namespace Conda.Core.Settings
{
    public class FolderSettings
    {
        private const string SettingsFileName = "conda-settings.json";
        private const string CondaFolderName = ".conda";

        public SettingsModel Settings { get; private set; }
        private readonly string _folderPath;

        public FolderSettings(string folderPath)
        {
            _folderPath = folderPath;
            Settings = Load();
        }

        private string GetSettingsPath()
        {
            return Path.Combine(_folderPath, CondaFolderName, SettingsFileName);
        }

        public SettingsModel Load()
        {
            string path = GetSettingsPath();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<SettingsModel>(json);
                    return settings ?? new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading folder settings: {ex.Message}");
            }

            // Return a default model (could also return global settings as base)
            return new SettingsModel();
        }

        public void Save()
        {
            string path = GetSettingsPath();
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    // Hide the .conda folder on Windows if possible
                    try { File.SetAttributes(directory, File.GetAttributes(directory) | FileAttributes.Hidden); } catch { }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Settings, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving folder settings: {ex.Message}");
            }
        }
    }
}
