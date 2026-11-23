namespace WeatherIntelligenceDashboard.Models;

public class AppSettings
{
    // Set Queen Creek, Arizona as the default location for weather forecasts
    public LocationInfo FavoriteLocation { get; set; } = new LocationInfo
    {
        Name = "Queen Creek",
        Admin1 = "Arizona",
        Country = "United States",
        CountryCode = "US",
        Latitude = 33.2487,
        Longitude = -111.6343,
        Timezone = "America/Phoenix"
    };

    public List<LocationInfo> SavedLocations { get; set; } = new List<LocationInfo>();
}
