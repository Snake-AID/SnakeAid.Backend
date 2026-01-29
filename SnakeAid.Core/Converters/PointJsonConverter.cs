using NetTopologySuite.Geometries;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnakeAid.Core.Converters
{
    /// <summary>
    /// JSON Converter for NetTopologySuite Point (PostGIS geometry)
    /// Converts between Point and JSON object with latitude/longitude
    /// </summary>
    public class PointJsonConverter : JsonConverter<Point>
    {
        public override Point? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            double? latitude = null;
            double? longitude = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "latitude":
                        case "lat":
                            latitude = reader.GetDouble();
                            break;
                        case "longitude":
                        case "lon":
                        case "lng":
                            longitude = reader.GetDouble();
                            break;
                    }
                }
            }

            if (!latitude.HasValue || !longitude.HasValue)
                throw new JsonException("Point must have both latitude and longitude");

            // PostGIS uses SRID 4326 (WGS84) - lon/lat order
            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            return geometryFactory.CreatePoint(new Coordinate(longitude.Value, latitude.Value));
        }

        public override void Write(Utf8JsonWriter writer, Point? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteNumber("latitude", value.Y);   // Y = latitude
            writer.WriteNumber("longitude", value.X);  // X = longitude
            writer.WriteEndObject();
        }
    }
}
