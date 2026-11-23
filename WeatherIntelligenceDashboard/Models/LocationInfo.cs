namespace WeatherIntelligenceDashboard.Models;

public class LocationInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Admin1 { get; set; } = "";
    public string Country { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = "";

    public string DisplayName
    {
        get
        {
            List<string> parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                parts.Add(Name);
            }

            if (!string.IsNullOrWhiteSpace(Admin1) && !Admin1.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(Admin1);
            }

            if (!string.IsNullOrWhiteSpace(Country))
            {
                parts.Add(Country);
            }

            return string.Join(", ", parts);
        }
    }
}
