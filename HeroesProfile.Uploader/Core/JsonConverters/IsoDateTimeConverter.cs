using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeroesProfile.Uploader.Core.JsonConverters;

public class IsoDateTimeConverter : JsonConverter<DateTime>
{
    private readonly DateTimeStyles _dateTimeStyles;

    public IsoDateTimeConverter(DateTimeStyles dateTimeStyles)
    {
        _dateTimeStyles = dateTimeStyles;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) {
            throw new JsonException("Expected a string token.");
        }

        var dateString = reader.GetString();

        if (DateTime.TryParse(dateString, null, _dateTimeStyles, out var dateTime)) {
            return dateTime;
        }

        throw new JsonException($"Unable to convert '{dateString}' to DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("o"));
    }
}
