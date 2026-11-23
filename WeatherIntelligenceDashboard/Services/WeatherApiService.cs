using System.Globalization;
using System.Text.Json;
using WeatherIntelligenceDashboard.Models;

namespace WeatherIntelligenceDashboard.Services;

public class WeatherApiService
{
    // Define the Open-Meteo API endpoints for forecasts, location searches, and historical weather data
    private const string ForecastBase =
        "https://api.open-meteo.com/v1/forecast";

    private const string GeocodingBase =
        "https://geocoding-api.open-meteo.com/v1/search";

    private const string HistoricalBase =
        "https://archive-api.open-meteo.com/v1/archive";

    // Store cached API responses to support reuse without unnecessary network requests
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CacheItem> _cache =
        new Dictionary<string, CacheItem>();

    public WeatherApiService(HttpClient httpClient)
    {
        // Use the HTTP client provided by the application for weather API requests
        _httpClient = httpClient;
    }

    public async Task<List<LocationInfo>> SearchLocationsAsync(string query)
    {
        // Return an empty list if the search query is missing or contains only whitespace
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<LocationInfo>();
        }

        // Build the geocoding API request using a URL-encoded search query
        string url =
            $"{GeocodingBase}?name={Uri.EscapeDataString(query.Trim())}" +
            "&count=10&language=en&format=json";

        // Retrieve location data, allowing cached results to be reused for up to 12 hours
        using JsonDocument document =
            await GetJsonAsync(url, TimeSpan.FromHours(12));

        List<LocationInfo> results = new List<LocationInfo>();

        // Return an empty list if the API response contains no valid results array
        if (!document.RootElement.TryGetProperty("results", out JsonElement locations) || locations.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        // Convert each JSON location into a strongly typed LocationInfo object
        foreach (JsonElement location in locations.EnumerateArray())
        {
            results.Add(new LocationInfo
            {
                Id = GetInt(location, "id"),
                Name = GetString(location, "name"),
                Admin1 = GetString(location, "admin1"),
                Country = GetString(location, "country"),
                CountryCode = GetString(location, "country_code"),
                Latitude = GetDouble(location, "latitude"),
                Longitude = GetDouble(location, "longitude"),
                Timezone = GetString(location, "timezone")
            });
        }

        return results;
    }

    public async Task<WeatherSnapshot> GetForecastAsync(LocationInfo location)
    {
        // Format coordinates consistently for the API, regardless of system locale
        string latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);

        string longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);

        // Specify the current, hourly, and daily weather measurements to retrieve
        string current =
            "temperature_2m,relative_humidity_2m,apparent_temperature," +
            "precipitation,weather_code,cloud_cover,pressure_msl," +
            "wind_speed_10m,wind_direction_10m,wind_gusts_10m";

        string hourly =
            "temperature_2m,apparent_temperature," +
            "precipitation_probability,precipitation,weather_code," +
            "wind_speed_10m";

        string daily =
            "weather_code,temperature_2m_max,temperature_2m_min," +
            "precipitation_sum,precipitation_probability_max," +
            "wind_speed_10m_max,sunrise,sunset";

        // Build the forecast request using imperial units and the location's time zone
        string url =
            $"{ForecastBase}?latitude={latitude}&longitude={longitude}" +
            $"&current={current}" +
            $"&hourly={hourly}" +
            $"&daily={daily}" +
            "&temperature_unit=fahrenheit" +
            "&wind_speed_unit=mph" +
            "&precipitation_unit=inch" +
            "&timezone=auto" +
            "&forecast_days=7";

        // Retrieve forecast data, reusing cached responses for up to 10 minutes
        using JsonDocument document = await GetJsonAsync(url, TimeSpan.FromMinutes(10));

        // Convert the API response into a structured weather snapshot
        return ParseForecast(document.RootElement, location);
    }

    public async Task<HistoricalWeather> GetHistoricalAsync(LocationInfo location, DateTime startDate, DateTime endDate)
    {
        // Format geographic coordinates consistently for the API
        string latitude =
            location.Latitude.ToString(
                CultureInfo.InvariantCulture);

        string longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);

        // Specify the daily weather measurements needed for historical analysis
        string daily =
            "weather_code,temperature_2m_max,temperature_2m_min," +
            "temperature_2m_mean,precipitation_sum,wind_speed_10m_max";

        // Build the historical weather request using the selected date range and imperial units
        string url =
            $"{HistoricalBase}?latitude={latitude}&longitude={longitude}" +
            $"&start_date={startDate:yyyy-MM-dd}" +
            $"&end_date={endDate:yyyy-MM-dd}" +
            $"&daily={daily}" +
            "&temperature_unit=fahrenheit" +
            "&wind_speed_unit=mph" +
            "&precipitation_unit=inch" +
            "&timezone=auto";

        // Retrieve historical weather data, caching responses for up to six hours
        using JsonDocument document = await GetJsonAsync(url, TimeSpan.FromHours(6));

        return ParseHistorical(document.RootElement, location);
    }

    private WeatherSnapshot ParseForecast(JsonElement root, LocationInfo location)
    {
        // Initialize the weather snapshot with location and time zone information
        WeatherSnapshot snapshot = new WeatherSnapshot
        {
            Location = location,
            Timezone = GetString(root, "timezone")
        };

        // Extract current weather conditions when the API response includes them
        if (root.TryGetProperty("current", out JsonElement current))
        {
            snapshot.Current = new CurrentWeather
            {
                Time = GetDateTime(current, "time"),
                Temperature =
                    GetDouble(current, "temperature_2m"),
                ApparentTemperature =
                    GetDouble(current, "apparent_temperature"),
                RelativeHumidity =
                    GetDouble(current, "relative_humidity_2m"),
                Precipitation =
                    GetDouble(current, "precipitation"),
                WeatherCode =
                    GetInt(current, "weather_code"),
                CloudCover =
                    GetDouble(current, "cloud_cover"),
                Pressure =
                    GetDouble(current, "pressure_msl"),
                WindSpeed =
                    GetDouble(current, "wind_speed_10m"),
                WindDirection =
                    GetDouble(current, "wind_direction_10m"),
                WindGusts =
                    GetDouble(current, "wind_gusts_10m")
            };
        }

        // Populate hourly and daily forecasts using their dedicated parsing methods
        ParseHourly(root, snapshot.Hourly);
        ParseDaily(root, snapshot.Daily);

        return snapshot;
    }

    private void ParseHourly(JsonElement root, List<HourlyWeather> results)
    {
        // Exit if the API response does not contain hourly forecast data
        if (!root.TryGetProperty("hourly", out JsonElement hourly))
        {
            return;
        }

        // Extract hourly weather measurements into separate lists
        List<string> times = GetStringArray(hourly, "time");

        List<double> temperatures = GetDoubleArray(hourly, "temperature_2m");

        List<double> apparent = GetDoubleArray(hourly, "apparent_temperature");

        List<int> precipitationProbability = GetIntArray(hourly, "precipitation_probability");

        List<double> precipitation = GetDoubleArray(hourly, "precipitation");

        List<int> codes = GetIntArray(hourly, "weather_code");

        List<double> wind = GetDoubleArray(hourly, "wind_speed_10m");

        // Use the shortest list to prevent accessing missing weather measurements
        int count = new[]
        {
            times.Count,
            temperatures.Count,
            apparent.Count,
            precipitationProbability.Count,
            precipitation.Count,
            codes.Count,
            wind.Count
        }.Min();

        for (int i = 0; i < count; i++)
        {
            // Skip hourly records containing invalid or unrecognized timestamps
            if (!DateTime.TryParse(
                    times[i],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime time))
            {
                continue;
            }

            // Combine matching measurements into individual hourly forecast records.
            results.Add(new HourlyWeather
            {
                Time = time,
                Temperature = temperatures[i],
                ApparentTemperature = apparent[i],
                PrecipitationProbability =
                    precipitationProbability[i],
                Precipitation = precipitation[i],
                WeatherCode = codes[i],
                WindSpeed = wind[i]
            });
        }
    }

    private void ParseDaily(JsonElement root, List<DailyWeather> results)
    {
        // Exit if the API response does not contain daily forecast data
        if (!root.TryGetProperty("daily", out JsonElement daily))
        {
            return;
        }

        // Extract daily weather measurements and sunrise/sunset times into separate lists
        List<string> times = GetStringArray(daily, "time");

        List<int> codes = GetIntArray(daily, "weather_code");

        List<double> highs = GetDoubleArray(daily, "temperature_2m_max");

        List<double> lows = GetDoubleArray(daily, "temperature_2m_min");

        List<double> precipitation = GetDoubleArray(daily, "precipitation_sum");

        List<int> precipitationProbability = GetIntArray(daily, "precipitation_probability_max");

        List<double> wind = GetDoubleArray(daily, "wind_speed_10m_max");

        List<string> sunrise = GetStringArray(daily, "sunrise");

        List<string> sunset = GetStringArray(daily, "sunset");

        // Limit processing to the shortest required list to prevent indexing errors
        int count = new[]
        {
            times.Count,
            codes.Count,
            highs.Count,
            lows.Count,
            precipitation.Count,
            precipitationProbability.Count,
            wind.Count
        }.Min();

        for (int i = 0; i < count; i++)
        {
            // Skip daily records containing invalid or unrecognized dates
            if (!DateTime.TryParse(
                    times[i],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime date))
            {
                continue;
            }

            // Combine daily measurements while safely handling missing sunrise or sunset values
            results.Add(new DailyWeather
            {
                Date = date,
                WeatherCode = codes[i],
                High = highs[i],
                Low = lows[i],
                Precipitation = precipitation[i],
                PrecipitationProbability =
                    precipitationProbability[i],
                WindSpeedMax = wind[i],
                Sunrise = i < sunrise.Count
                    ? ParseDateTime(sunrise[i])
                    : null,
                Sunset = i < sunset.Count
                    ? ParseDateTime(sunset[i])
                    : null
            });
        }
    }

    private HistoricalWeather ParseHistorical(JsonElement root, LocationInfo location)
    {
        // Initialize the historical weather result for the requested location
        HistoricalWeather result = new HistoricalWeather
        {
            Location = location
        };

        // Return an empty historical result if the API response contains no daily data
        if (!root.TryGetProperty("daily", out JsonElement daily))
        {
            return result;
        }

        // Extract historical weather measurements into separate lists
        List<string> times = GetStringArray(daily, "time");

        List<int> codes = GetIntArray(daily, "weather_code");

        List<double> highs = GetDoubleArray(daily, "temperature_2m_max");

        List<double> lows = GetDoubleArray(daily, "temperature_2m_min");

        List<double> means = GetDoubleArray(daily, "temperature_2m_mean");

        List<double> precipitation = GetDoubleArray(daily, "precipitation_sum");

        List<double> wind = GetDoubleArray(daily, "wind_speed_10m_max");

        // Process only complete records to prevent out-of-range array access
        int count = new[]
        {
            times.Count,
            codes.Count,
            highs.Count,
            lows.Count,
            means.Count,
            precipitation.Count,
            wind.Count
        }.Min();

        for (int i = 0; i < count; i++)
        {
            // Skip historical records with invalid or unrecognized dates
            if (!DateTime.TryParse(
                    times[i],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime date))
            {
                continue;
            }

            // Combine matching measurements into individual historical weather records
            result.Days.Add(new HistoricalDay
            {
                Date = date,
                WeatherCode = codes[i],
                High = highs[i],
                Low = lows[i],
                Mean = means[i],
                Precipitation = precipitation[i],
                WindSpeedMax = wind[i]
            });
        }

        return result;
    }

    private async Task<JsonDocument> GetJsonAsync(string url, TimeSpan cacheDuration)
    {
        // Reuse cached responses that have not exceeded their expiration period
        if (_cache.TryGetValue(url, out CacheItem? cached) && DateTimeOffset.UtcNow - cached.StoredAt < cacheDuration)
        {
            return JsonDocument.Parse(cached.Json);
        }

        // Send the API request and ensure the server returned a successful HTTP status
        using HttpResponseMessage response = await _httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        // Read the response and reject empty or whitespace-only data
        string json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "The weather service returned an empty response.");
        }

        // Cache the response with a UTC timestamp for future expiration checks
        _cache[url] = new CacheItem
        {
            Json = json,
            StoredAt = DateTimeOffset.UtcNow
        };

        return JsonDocument.Parse(json);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        // Verify the property exists and contains a string before retrieving its value
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        // Return an empty string when the property is missing or is not a string
        return "";
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        // Verify the JSON property exists before attempting to read its value
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(
                propertyName,
                out JsonElement value))
        {
            // Retrieve the integer directly when the JSON value is numeric
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int number))
            {
                return number;
            }

            // Attempt to convert string values into integers when necessary
            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(
                    value.GetString(),
                    out number))
            {
                return number;
            }
        }

        // Return zero if the property is missing or cannot be converted to an integer
        return 0;
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        // Verify the JSON property exists before attempting to retrieve its value
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement value))
        {
            // Retrieve the decimal value directly when the JSON property is numeric
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDouble(out double number))
            {
                return number;
            }

            // Convert numeric strings using consistent decimal formatting across system locales
            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(
                    value.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }
        }

        // Return zero if the property is missing or cannot be converted to a double
        return 0;
    }

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        // Retrieve the date/time string from the specified JSON property
        string value = GetString(
            element,
            propertyName);

        return ParseDateTime(value);
    }

    private static DateTime? ParseDateTime(string value)
    {
        // Attempt to convert the string into a DateTime using culture-independent formatting
        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
        {
            return date;
        }

        // Return null when the date/time value is missing or cannot be parsed
        return null;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        List<string> results = new List<string>();

        // Return an empty list if the requested property is missing or is not a JSON array
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                propertyName,
                out JsonElement array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        // Extract string values while preserving array positions for consistent data alignment
        foreach (JsonElement item in array.EnumerateArray())
        {
            results.Add(
                item.ValueKind == JsonValueKind.String
                    ? item.GetString() ?? ""
                    : "");
        }

        // Return the extracted values for further weather data processing
        return results;
    }

    private static List<double> GetDoubleArray(JsonElement element, string propertyName)
    {
        List<double> results = new List<double>();

        // Return an empty list if the property is missing or is not a JSON array
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                propertyName,
                out JsonElement array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        // Extract numeric values while preserving their original array positions
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number &&
                item.TryGetDouble(out double value))
            {
                results.Add(value);
            }
            else
            {
                // Substitute zero for invalid or nonnumeric values to maintain array alignment
                results.Add(0);
            }
        }

        return results;
    }

    private static List<int> GetIntArray(JsonElement element, string propertyName)
    {
        List<int> results = new List<int>();

        // Return an empty list if the property is missing or is not a JSON array
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(
                propertyName,
                out JsonElement array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        // Extract integer values while preserving their original array positions
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number &&
                item.TryGetInt32(out int value))
            {
                results.Add(value);
            }
            else
            {
                // Substitute zero for invalid values to maintain array alignment
                results.Add(0);
            }
        }

        return results;
    }

    // Store cached JSON responses and their timestamps to track cache expiration
    private class CacheItem
    {
        public string Json { get; set; } = "";
        public DateTimeOffset StoredAt { get; set; }
    }
}
