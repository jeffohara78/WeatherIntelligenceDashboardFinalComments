using System.Text.Json;
using WeatherIntelligenceDashboard.Models;

namespace WeatherIntelligenceDashboard.Persistence;

public class SettingsRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;

    public SettingsRepository()
    {
        // Create a Data directory to store application settings locally
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "settings.json");

        // Configure JSON serialization for readability and flexible property matching
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<AppSettings> LoadAsync()
    {
        // Create and save default settings if no settings file exists
        if (!File.Exists(_filePath))
        {
            AppSettings settings = new AppSettings();
            await SaveAsync(settings);
            return settings;
        }

        try
        {
            // Load saved settings from the JSON file
            string json = await File.ReadAllTextAsync(_filePath);

            return JsonSerializer.Deserialize<AppSettings>(json, _options) ?? new AppSettings();
        }
        catch
        {
            // Fall back to default settings if the file cannot be read or parsed
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        // Serialize and persist application settings to the local JSON file
        string json = JsonSerializer.Serialize(settings, _options);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
