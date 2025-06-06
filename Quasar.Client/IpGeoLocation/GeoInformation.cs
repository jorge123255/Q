using System;

namespace Quasar.Client.IpGeoLocation
{
    /// <summary>
    /// Provides geolocation information for IP addresses
    /// </summary>
    public class GeoInformation
    {
        public string IpAddress { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZone { get; set; }

        public GeoInformation()
        {
            IpAddress = "Unknown";
            Country = "Unknown";
            CountryCode = "N/A";
            Region = "Unknown";
            City = "Unknown";
            ZipCode = "Unknown";
            Latitude = 0;
            Longitude = 0;
            TimeZone = "Unknown";
        }
    }
}
