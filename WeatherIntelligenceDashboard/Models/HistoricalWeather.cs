namespace WeatherIntelligenceDashboard.Models;

public class HistoricalWeather
{
    public LocationInfo Location { get; set; } = new LocationInfo();
    public List<HistoricalDay> Days { get; set; } = new List<HistoricalDay>();
}

public class HistoricalDay
{
    public DateTime Date { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Mean { get; set; }
    public double Precipitation { get; set; }
    public double WindSpeedMax { get; set; }
    public int WeatherCode { get; set; }
}
