/* Jeff O'Hara
 * Weather Intelligence Dashboard
 * 8/14/2026
 * 
 * This program is a console application that retrieves weather data from an API and displays it in a dashboard format.
 * It uses HttpClient to make HTTP requests and handles automatic decompression of responses.
 * The program also manages settings and configurations through a repository pattern.
 */

/* 8/18/2026
 * No updates, just adding comments for clarity and maintainability.
 * Also cleaned up the code in some areas for better readability and performance.
 */

using System.Net;
using WeatherIntelligenceDashboard;
using WeatherIntelligenceDashboard.Persistence;
using WeatherIntelligenceDashboard.Services;

// Enable UTF-8 encoding to display weather symbols and special characters correctly
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Automatically decompress compressed API responses to efficiently process weather data
using HttpClientHandler handler = new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip |
                             DecompressionMethods.Deflate |
                             DecompressionMethods.Brotli
};

// Configure the HTTP client with a 20-second timeout to prevent requests from hanging indefinitely
using HttpClient httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(20)
};

// Set a User-Agent so requests to the weather API are identifiable and not blocked by the server.
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherIntelligenceDashboard/1.0");

// Initialize the weather API service using the configured HTTP client
WeatherApiService weatherApiService = new WeatherApiService(httpClient);
// Initialize the settings repository to manage application settings
SettingsRepository settingsRepository = new SettingsRepository();

// Initialize the application manager with the weather API service and settings repository
AppManager appManager = new AppManager(weatherApiService, settingsRepository);

// Start the application's main workflow asynchronously
await appManager.RunAsync();
