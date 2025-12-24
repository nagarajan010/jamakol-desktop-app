using System;
using System.Text.Json.Serialization;

namespace JamakolAstrology.Models;

public class GeoLocation
{
    public string Name { get; set; } = "";
    public string CountryName { get; set; } = "";
    public string AdminName1 { get; set; } = ""; // State/Region
    public string Lat { get; set; } = "0";
    public string Lng { get; set; } = "0";
    public GeoTimezone? Timezone { get; set; }
    
    // Helper for display
    public string DisplayName => $"{Name}, {AdminName1}, {CountryName}";
}

public class GeoTimezone
{
    public double GmtOffset { get; set; }
    public string TimeZoneId { get; set; } = "";
}
