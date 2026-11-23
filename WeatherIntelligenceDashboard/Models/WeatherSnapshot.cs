namespace WeatherIntelligenceDashboard.Models;

public class WeatherSnapshot
{
    public LocationInfo Location { get; set; } = new LocationInfo();
    public string Timezone { get; set; } = "";

    public CurrentWeather Current { get; set; } = new CurrentWeather();
    public List<HourlyWeather> Hourly { get; set; } = new List<HourlyWeather>();
    public List<DailyWeather> Daily { get; set; } = new List<DailyWeather>();
}

public class CurrentWeather
{
    public DateTime? Time { get; set; }
    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public double RelativeHumidity { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public double CloudCover { get; set; }
    public double Pressure { get; set; }
    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }
    public double WindGusts { get; set; }
}

public class HourlyWeather
{
    public DateTime Time { get; set; }
    public double Temperature { get; set; }
    public double ApparentTemperature { get; set; }
    public int PrecipitationProbability { get; set; }
    public double Precipitation { get; set; }
    public int WeatherCode { get; set; }
    public double WindSpeed { get; set; }
}

public class DailyWeather
{
    public DateTime Date { get; set; }
    public int WeatherCode { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Precipitation { get; set; }
    public int PrecipitationProbability { get; set; }
    public double WindSpeedMax { get; set; }
    public DateTime? Sunrise { get; set; }
    public DateTime? Sunset { get; set; }
}
