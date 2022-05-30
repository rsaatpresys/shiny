using System;
using System.Text.Json.Serialization;


namespace Shiny.Locations.Web
{
    public class GeoPosition : IGpsReading
    {
        [JsonIgnore]
        public double PositionAccuracy => this.Accuracy;

        [JsonIgnore]
        public double Accuracy => this.RawAccuracy ?? -1;

        [JsonIgnore]
        public double Speed { get; set; }
        
        [JsonIgnore]
        public double Altitude => this.RawAltitude ?? -1;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonIgnore]
        public double Heading => this.RawHeading ?? -1;


        [JsonPropertyName("altitudeAccuracy")]
        public long? AltitudeAccuracy { get; set; }

        [JsonIgnore]
        public double HeadingAccuracy => -1;

        [JsonIgnore]
        public double SpeedAccuracy => -1;

        [JsonPropertyName("timestamp")]
        public long Epoch { get; set; }

        [JsonPropertyName("accuracy")]
        public double? RawAccuracy { get; set; }

        [JsonPropertyName("heading")]
        public double? RawHeading { get; set; }

        [JsonPropertyName("speed")]
        public double? RawSpeed { get; set; }

        [JsonPropertyName("altitude")]
        public double? RawAltitude { get; set; }

        Position? position;
        public Position Position => this.position ??= new Position(this.Latitude, this.Longitude);

        [JsonIgnore]
        public DateTime Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(this.Epoch).DateTime;
    }
}
