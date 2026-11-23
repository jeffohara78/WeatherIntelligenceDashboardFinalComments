using WeatherIntelligenceDashboard.Models;
using WeatherIntelligenceDashboard.Persistence;
using WeatherIntelligenceDashboard.Services;
using WeatherIntelligenceDashboard.Utilities;

namespace WeatherIntelligenceDashboard;

public class AppManager
{
    private readonly WeatherApiService _weatherApiService;
    private readonly SettingsRepository _settingsRepository;
    private AppSettings _settings = new AppSettings();

    public AppManager(
        WeatherApiService weatherApiService,
        SettingsRepository settingsRepository)
    {
        _weatherApiService = weatherApiService;
        _settingsRepository = settingsRepository;
    }

    public async Task RunAsync()
    {
        _settings = await _settingsRepository.LoadAsync();

        bool running = true;

        while (running)
        {
            ShowMainMenu();

            Console.Write("Choose an option: ");
            string choice = Console.ReadLine()?.Trim() ?? "";

            try
            {
                switch (choice)
                {
                    case "1":
                        await ShowDashboardAsync();
                        break;
                    case "2":
                        await ShowSevenDayForecastAsync();
                        break;
                    case "3":
                        await ShowHourlyForecastAsync();
                        break;
                    case "4":
                        await SearchLocationAsync();
                        break;
                    case "5":
                        await CompareLocationsAsync();
                        break;
                    case "6":
                        await ShowHistoricalWeatherAsync();
                        break;
                    case "7":
                        await ManageSavedLocationsAsync();
                        break;
                    case "8":
                        await ChangeFavoriteLocationAsync();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        ConsoleTheme.Error(
                            "Please choose one of the menu options.");
                        ConsoleTheme.Pause();
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                ConsoleTheme.Error(
                    $"Unable to reach the weather service: {ex.Message}");
                ConsoleTheme.Pause();
            }
            catch (TaskCanceledException)
            {
                ConsoleTheme.Error(
                    "The weather request timed out.");
                ConsoleTheme.Pause();
            }
            catch (Exception ex)
            {
                ConsoleTheme.Error(
                    $"Unable to complete the request: {ex.Message}");
                ConsoleTheme.Pause();
            }
        }
    }

    private void ShowMainMenu()
    {
        Console.Clear();
        ConsoleTheme.Title("WEATHER INTELLIGENCE DASHBOARD");

        Console.Write("Favorite location: ");
        ConsoleTheme.Highlight(
            _settings.FavoriteLocation.DisplayName);

        ConsoleTheme.Muted(
            "Color accessibility: labels and symbols always accompany color.");

        Console.WriteLine();
        ConsoleTheme.MenuOption(
            "1",
            "Favorite Location Dashboard");
        ConsoleTheme.MenuOption(
            "2",
            "7-Day Forecast");
        ConsoleTheme.MenuOption(
            "3",
            "Hourly Forecast");
        ConsoleTheme.MenuOption(
            "4",
            "Search Another Location");
        ConsoleTheme.MenuOption(
            "5",
            "Compare Two Locations");
        ConsoleTheme.MenuOption(
            "6",
            "Historical Weather");
        ConsoleTheme.MenuOption(
            "7",
            "Saved Locations");
        ConsoleTheme.MenuOption(
            "8",
            "Change Favorite Location");
        ConsoleTheme.MenuOption(
            "0",
            "Exit");

        Console.WriteLine();
    }

    private async Task ShowDashboardAsync()
    {
        Console.Clear();

        WeatherSnapshot weather =
            await _weatherApiService.GetForecastAsync(
                _settings.FavoriteLocation);

        ConsoleTheme.Title(
            $"{weather.Location.DisplayName.ToUpper()} WEATHER");

        CurrentWeather current = weather.Current;

        ConsoleTheme.Section("Current Conditions");

        Console.WriteLine(
            $"Conditions:   {WeatherCodeMapper.Describe(current.WeatherCode)}");
        Console.WriteLine(
            $"Temperature:  {current.Temperature:0.#} °F");
        Console.WriteLine(
            $"Feels Like:   {current.ApparentTemperature:0.#} °F");
        Console.WriteLine(
            $"Humidity:     {current.RelativeHumidity:0}%");
        Console.WriteLine(
            $"Wind:         {current.WindSpeed:0.#} mph {Direction(current.WindDirection)}");
        Console.WriteLine(
            $"Wind Gusts:   {current.WindGusts:0.#} mph");
        Console.WriteLine(
            $"Cloud Cover:  {current.CloudCover:0}%");
        Console.WriteLine(
            $"Pressure:     {current.Pressure:0.#} hPa");
        Console.WriteLine(
            $"Precipitation:{current.Precipitation,7:0.00} in");

        DailyWeather? today =
            weather.Daily.FirstOrDefault();

        if (today != null)
        {
            ConsoleTheme.Section("Today");

            Console.WriteLine(
                $"High / Low:  {today.High:0.#} °F / {today.Low:0.#} °F");
            Console.WriteLine(
                $"Rain Chance: {today.PrecipitationProbability}%");
            Console.WriteLine(
                $"Precip Total:{today.Precipitation,7:0.00} in");
            Console.WriteLine(
                $"Max Wind:    {today.WindSpeedMax:0.#} mph");

            if (today.Sunrise.HasValue)
            {
                Console.WriteLine(
                    $"Sunrise:     {today.Sunrise.Value:h:mm tt}");
            }

            if (today.Sunset.HasValue)
            {
                Console.WriteLine(
                    $"Sunset:      {today.Sunset.Value:h:mm tt}");
            }
        }

        ConsoleTheme.Section("Next 6 Hours");

        DateTime now = DateTime.Now;

        List<HourlyWeather> upcoming =
            weather.Hourly
                .Where(hour => hour.Time >= now.AddHours(-1))
                .Take(6)
                .ToList();

        if (upcoming.Count == 0)
        {
            ConsoleTheme.Muted(
                "No hourly forecast was returned.");
        }
        else
        {
            ConsoleTheme.TableHeader(
                "TIME       TEMP  FEELS  RAIN  CONDITIONS");

            foreach (HourlyWeather hour in upcoming)
            {
                Console.WriteLine(
                    $"{hour.Time:h tt,-10} " +
                    $"{hour.Temperature,5:0}° " +
                    $"{hour.ApparentTemperature,5:0}° " +
                    $"{hour.PrecipitationProbability,4}%  " +
                    $"{WeatherCodeMapper.Describe(hour.WeatherCode)}");
            }
        }

        ConsoleTheme.Pause();
    }

    private async Task ShowSevenDayForecastAsync()
    {
        Console.Clear();

        WeatherSnapshot weather =
            await _weatherApiService.GetForecastAsync(
                _settings.FavoriteLocation);

        ConsoleTheme.Title(
            $"7-DAY FORECAST — {weather.Location.DisplayName}");

        ConsoleTheme.TableHeader(
            "DAY          CONDITIONS                 HIGH   LOW   RAIN   WIND");

        foreach (DailyWeather day in weather.Daily)
        {
            string conditions =
                Trim(
                    WeatherCodeMapper.Describe(day.WeatherCode),
                    26);

            Console.WriteLine(
                $"{day.Date:ddd MMM d,-12} " +
                $"{conditions,-26} " +
                $"{day.High,5:0}° " +
                $"{day.Low,5:0}° " +
                $"{day.PrecipitationProbability,5}% " +
                $"{day.WindSpeedMax,5:0} mph");
        }

        ConsoleTheme.Pause();
    }

    private async Task ShowHourlyForecastAsync()
    {
        Console.Clear();

        WeatherSnapshot weather =
            await _weatherApiService.GetForecastAsync(
                _settings.FavoriteLocation);

        ConsoleTheme.Title(
            $"HOURLY FORECAST — {weather.Location.DisplayName}");

        Console.WriteLine(
            "1. Next 12 hours");
        Console.WriteLine(
            "2. Next 24 hours");
        Console.WriteLine(
            "3. Tomorrow");
        Console.WriteLine(
            "0. Back");
        Console.WriteLine();

        Console.Write("Choose: ");
        string choice =
            Console.ReadLine()?.Trim() ?? "";

        if (choice == "0")
        {
            return;
        }

        IEnumerable<HourlyWeather> selected;
        DateTime now = DateTime.Now;

        if (choice == "1")
        {
            selected =
                weather.Hourly
                    .Where(hour => hour.Time >= now.AddHours(-1))
                    .Take(12);
        }
        else if (choice == "2")
        {
            selected =
                weather.Hourly
                    .Where(hour => hour.Time >= now.AddHours(-1))
                    .Take(24);
        }
        else if (choice == "3")
        {
            DateTime tomorrow =
                DateTime.Today.AddDays(1);

            selected =
                weather.Hourly.Where(
                    hour =>
                        hour.Time.Date == tomorrow.Date);
        }
        else
        {
            ConsoleTheme.Error(
                "That was not a valid option.");
            ConsoleTheme.Pause();
            return;
        }

        ConsoleTheme.Section("Hourly Details");
        ConsoleTheme.TableHeader(
            "TIME             TEMP  FEELS  RAIN  WIND   CONDITIONS");

        foreach (HourlyWeather hour in selected)
        {
            Console.WriteLine(
                $"{hour.Time:ddd h tt,-16} " +
                $"{hour.Temperature,5:0}° " +
                $"{hour.ApparentTemperature,5:0}° " +
                $"{hour.PrecipitationProbability,4}% " +
                $"{hour.WindSpeed,5:0}  " +
                $"{WeatherCodeMapper.Describe(hour.WeatherCode)}");
        }

        ConsoleTheme.Pause();
    }

    private async Task SearchLocationAsync()
    {
        Console.Clear();
        ConsoleTheme.Title("SEARCH LOCATION");

        LocationInfo? location =
            await PromptForLocationAsync();

        if (location == null)
        {
            return;
        }

        WeatherSnapshot weather =
            await _weatherApiService.GetForecastAsync(
                location);

        ShowLocationSummary(weather);

        Console.WriteLine();
        Console.Write(
            "Save this location? (y/n): ");

        string answer =
            Console.ReadLine()?.Trim() ?? "";

        if (answer.Equals(
                "y",
                StringComparison.OrdinalIgnoreCase))
        {
            await AddSavedLocationAsync(location);
        }

        ConsoleTheme.Pause();
    }

    private async Task CompareLocationsAsync()
    {
        Console.Clear();
        ConsoleTheme.Title("COMPARE TWO LOCATIONS");

        ConsoleTheme.Section("First Location");
        LocationInfo? first =
            await PromptForLocationAsync();

        if (first == null)
        {
            return;
        }

        ConsoleTheme.Section("Second Location");
        LocationInfo? second =
            await PromptForLocationAsync();

        if (second == null)
        {
            return;
        }

        WeatherSnapshot firstWeather =
            await _weatherApiService.GetForecastAsync(first);

        WeatherSnapshot secondWeather =
            await _weatherApiService.GetForecastAsync(second);

        Console.Clear();
        ConsoleTheme.Title("WEATHER COMPARISON");

        string firstName =
            Trim(first.DisplayName, 28);

        string secondName =
            Trim(second.DisplayName, 28);

        ConsoleTheme.TableHeader(
            $"{Pad("METRIC", 20)}" +
            $"{Pad(firstName, 28)}" +
            $"{Pad(secondName, 28)}");

        PrintComparisonRow(
            "Conditions",
            WeatherCodeMapper.Describe(
                firstWeather.Current.WeatherCode),
            WeatherCodeMapper.Describe(
                secondWeather.Current.WeatherCode));

        PrintComparisonRow(
            "Temperature",
            $"{firstWeather.Current.Temperature:0.#} °F",
            $"{secondWeather.Current.Temperature:0.#} °F");

        PrintComparisonRow(
            "Feels Like",
            $"{firstWeather.Current.ApparentTemperature:0.#} °F",
            $"{secondWeather.Current.ApparentTemperature:0.#} °F");

        PrintComparisonRow(
            "Humidity",
            $"{firstWeather.Current.RelativeHumidity:0}%",
            $"{secondWeather.Current.RelativeHumidity:0}%");

        PrintComparisonRow(
            "Wind",
            $"{firstWeather.Current.WindSpeed:0.#} mph",
            $"{secondWeather.Current.WindSpeed:0.#} mph");

        DailyWeather? firstToday =
            firstWeather.Daily.FirstOrDefault();

        DailyWeather? secondToday =
            secondWeather.Daily.FirstOrDefault();

        if (firstToday != null &&
            secondToday != null)
        {
            PrintComparisonRow(
                "Today's High",
                $"{firstToday.High:0.#} °F",
                $"{secondToday.High:0.#} °F");

            PrintComparisonRow(
                "Today's Low",
                $"{firstToday.Low:0.#} °F",
                $"{secondToday.Low:0.#} °F");

            PrintComparisonRow(
                "Rain Chance",
                $"{firstToday.PrecipitationProbability}%",
                $"{secondToday.PrecipitationProbability}%");
        }

        ConsoleTheme.Pause();
    }

    private async Task ShowHistoricalWeatherAsync()
    {
        Console.Clear();
        ConsoleTheme.Title("HISTORICAL WEATHER");

        Console.WriteLine(
            "1. Favorite location");
        Console.WriteLine(
            "2. Search another location");
        Console.WriteLine(
            "0. Back");
        Console.WriteLine();

        Console.Write("Choose: ");
        string choice =
            Console.ReadLine()?.Trim() ?? "";

        LocationInfo? location;

        if (choice == "0")
        {
            return;
        }

        if (choice == "1")
        {
            location = _settings.FavoriteLocation;
        }
        else if (choice == "2")
        {
            location =
                await PromptForLocationAsync();
        }
        else
        {
            ConsoleTheme.Error(
                "That was not a valid option.");
            ConsoleTheme.Pause();
            return;
        }

        if (location == null)
        {
            return;
        }

        Console.Write(
            "Start date (YYYY-MM-DD): ");

        if (!DateTime.TryParse(
                Console.ReadLine(),
                out DateTime startDate))
        {
            ConsoleTheme.Error(
                "The start date was not valid.");
            ConsoleTheme.Pause();
            return;
        }

        Console.Write(
            "End date (YYYY-MM-DD): ");

        if (!DateTime.TryParse(
                Console.ReadLine(),
                out DateTime endDate))
        {
            ConsoleTheme.Error(
                "The end date was not valid.");
            ConsoleTheme.Pause();
            return;
        }

        if (endDate < startDate)
        {
            ConsoleTheme.Error(
                "The end date cannot be before the start date.");
            ConsoleTheme.Pause();
            return;
        }

        if ((endDate - startDate).TotalDays > 31)
        {
            ConsoleTheme.Error(
                "For this portfolio app, historical searches are limited to 31 days at a time.");
            ConsoleTheme.Pause();
            return;
        }

        if (endDate.Date >= DateTime.Today)
        {
            ConsoleTheme.Error(
                "Historical searches must end before today.");
            ConsoleTheme.Pause();
            return;
        }

        HistoricalWeather history =
            await _weatherApiService.GetHistoricalAsync(
                location,
                startDate,
                endDate);

        Console.Clear();
        ConsoleTheme.Title(
            $"HISTORICAL WEATHER — {location.DisplayName}");

        if (history.Days.Count == 0)
        {
            ConsoleTheme.Muted(
                "No historical weather was returned.");
            ConsoleTheme.Pause();
            return;
        }

        ConsoleTheme.TableHeader(
            "DATE         HIGH   LOW   MEAN   PRECIP   WIND   CONDITIONS");

        foreach (HistoricalDay day in history.Days)
        {
            Console.WriteLine(
                $"{day.Date:yyyy-MM-dd} " +
                $"{day.High,6:0.0} " +
                $"{day.Low,6:0.0} " +
                $"{day.Mean,6:0.0} " +
                $"{day.Precipitation,7:0.00} " +
                $"{day.WindSpeedMax,6:0.0}  " +
                $"{WeatherCodeMapper.Describe(day.WeatherCode)}");
        }

        ConsoleTheme.Section("Summary");

        Console.WriteLine(
            $"Average High: {history.Days.Average(day => day.High):0.0} °F");

        Console.WriteLine(
            $"Average Low:  {history.Days.Average(day => day.Low):0.0} °F");

        Console.WriteLine(
            $"Warmest Day:  {history.Days.Max(day => day.High):0.0} °F");

        Console.WriteLine(
            $"Coolest Day:  {history.Days.Min(day => day.Low):0.0} °F");

        Console.WriteLine(
            $"Total Precip: {history.Days.Sum(day => day.Precipitation):0.00} in");

        ConsoleTheme.Pause();
    }

    private async Task ManageSavedLocationsAsync()
    {
        Console.Clear();
        ConsoleTheme.Title("SAVED LOCATIONS");

        if (_settings.SavedLocations.Count == 0)
        {
            ConsoleTheme.Muted(
                "You have not saved any locations yet.");
            ConsoleTheme.Pause();
            return;
        }

        for (int i = 0;
             i < _settings.SavedLocations.Count;
             i++)
        {
            LocationInfo location =
                _settings.SavedLocations[i];

            Console.WriteLine(
                $"{i + 1,2}. {location.DisplayName}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "Enter a location number to view it.");
        Console.WriteLine(
            "Enter D to delete a saved location.");
        Console.WriteLine(
            "Enter 0 to go back.");
        Console.WriteLine();

        Console.Write("Choice: ");
        string choice =
            Console.ReadLine()?.Trim() ?? "";

        if (choice == "0")
        {
            return;
        }

        if (choice.Equals(
                "d",
                StringComparison.OrdinalIgnoreCase))
        {
            await DeleteSavedLocationAsync();
            return;
        }

        if (!int.TryParse(
                choice,
                out int selection) ||
            selection < 1 ||
            selection > _settings.SavedLocations.Count)
        {
            ConsoleTheme.Error(
                "That saved location number is not valid.");
            ConsoleTheme.Pause();
            return;
        }

        LocationInfo selected =
            _settings.SavedLocations[selection - 1];

        WeatherSnapshot weather =
            await _weatherApiService.GetForecastAsync(
                selected);

        ShowLocationSummary(weather);
        ConsoleTheme.Pause();
    }

    private async Task ChangeFavoriteLocationAsync()
    {
        Console.Clear();
        ConsoleTheme.Title("CHANGE FAVORITE LOCATION");

        LocationInfo? location =
            await PromptForLocationAsync();

        if (location == null)
        {
            return;
        }

        _settings.FavoriteLocation = location;
        await _settingsRepository.SaveAsync(_settings);

        ConsoleTheme.Success(
            $"Favorite location changed to {location.DisplayName}.");

        ConsoleTheme.Pause();
    }

    private async Task<LocationInfo?> PromptForLocationAsync()
    {
        Console.Write(
            "Enter a city or location name (0 to cancel): ");

        string query =
            Console.ReadLine()?.Trim() ?? "";

        if (query == "0" ||
            string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        List<LocationInfo> results =
            await _weatherApiService.SearchLocationsAsync(
                query);

        if (results.Count == 0)
        {
            ConsoleTheme.Error(
                "No matching locations were found.");
            ConsoleTheme.Pause();
            return null;
        }

        Console.WriteLine();
        ConsoleTheme.TableHeader(
            " #  LOCATION");

        for (int i = 0; i < results.Count; i++)
        {
            Console.WriteLine(
                $"{i + 1,2}. {results[i].DisplayName}");
        }

        Console.WriteLine();
        Console.Write(
            "Choose a location number (0 to cancel): ");

        if (!int.TryParse(
                Console.ReadLine(),
                out int selection) ||
            selection < 0 ||
            selection > results.Count)
        {
            ConsoleTheme.Error(
                "That location number is not valid.");
            ConsoleTheme.Pause();
            return null;
        }

        if (selection == 0)
        {
            return null;
        }

        return results[selection - 1];
    }

    private void ShowLocationSummary(
        WeatherSnapshot weather)
    {
        Console.Clear();
        ConsoleTheme.Title(
            weather.Location.DisplayName.ToUpper());

        Console.WriteLine(
            $"Conditions:  {WeatherCodeMapper.Describe(weather.Current.WeatherCode)}");

        Console.WriteLine(
            $"Temperature: {weather.Current.Temperature:0.#} °F");

        Console.WriteLine(
            $"Feels Like:  {weather.Current.ApparentTemperature:0.#} °F");

        Console.WriteLine(
            $"Humidity:    {weather.Current.RelativeHumidity:0}%");

        Console.WriteLine(
            $"Wind:        {weather.Current.WindSpeed:0.#} mph");

        DailyWeather? today =
            weather.Daily.FirstOrDefault();

        if (today != null)
        {
            Console.WriteLine(
                $"High / Low: {today.High:0.#} °F / {today.Low:0.#} °F");

            Console.WriteLine(
                $"Rain Chance:{today.PrecipitationProbability,4}%");
        }
    }

    private async Task AddSavedLocationAsync(
        LocationInfo location)
    {
        bool exists =
            _settings.SavedLocations.Any(
                saved =>
                    Math.Abs(
                        saved.Latitude -
                        location.Latitude) < 0.0001 &&
                    Math.Abs(
                        saved.Longitude -
                        location.Longitude) < 0.0001);

        if (exists)
        {
            ConsoleTheme.Muted(
                "That location is already saved.");
            return;
        }

        _settings.SavedLocations.Add(location);
        await _settingsRepository.SaveAsync(_settings);

        ConsoleTheme.Success(
            $"{location.DisplayName} was saved.");
    }

    private async Task DeleteSavedLocationAsync()
    {
        Console.Write(
            "Enter the saved location number to delete: ");

        if (!int.TryParse(
                Console.ReadLine(),
                out int selection) ||
            selection < 1 ||
            selection > _settings.SavedLocations.Count)
        {
            ConsoleTheme.Error(
                "That location number is not valid.");
            ConsoleTheme.Pause();
            return;
        }

        LocationInfo removed =
            _settings.SavedLocations[selection - 1];

        _settings.SavedLocations.RemoveAt(selection - 1);
        await _settingsRepository.SaveAsync(_settings);

        ConsoleTheme.Success(
            $"{removed.DisplayName} was removed.");
        ConsoleTheme.Pause();
    }

    private static void PrintComparisonRow(
        string metric,
        string first,
        string second)
    {
        Console.WriteLine(
            $"{Pad(metric, 20)}" +
            $"{Pad(Trim(first, 26), 28)}" +
            $"{Pad(Trim(second, 26), 28)}");
    }

    private static string Pad(
        string value,
        int width)
    {
        if (value.Length >= width)
        {
            return value.Substring(0, width);
        }

        return value.PadRight(width);
    }

    private static string Trim(
        string value,
        int length)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        if (value.Length <= length)
        {
            return value;
        }

        return value.Substring(
                   0,
                   Math.Max(0, length - 3)) +
               "...";
    }

    private static string Direction(
        double degrees)
    {
        string[] directions =
        {
            "N",
            "NE",
            "E",
            "SE",
            "S",
            "SW",
            "W",
            "NW"
        };

        int index =
            (int)Math.Round(
                degrees / 45.0,
                MidpointRounding.AwayFromZero) % 8;

        return directions[index];
    }
}
