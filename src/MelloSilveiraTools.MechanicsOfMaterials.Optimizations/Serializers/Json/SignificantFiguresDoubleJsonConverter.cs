using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;

/// <summary>
/// A JSON converter that writes double values using a specified number of significant digits.
/// This prevents floating-point noise from causing hash mismatches for mathematically identical converged parameters.
/// </summary>
public class SignificantFiguresDoubleJsonConverter(int significantDigits = 7) : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() switch
            {
                "NaN" => double.NaN,
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                string raw when double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed) => parsed,
                string unexpected => throw new JsonException($"Invalid double string representation: '{unexpected}'."),
                null => throw new JsonException("Unexpected null string value when reading double.")
            };
        }

        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
            return;
        }

        if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");
            return;
        }

        string formatted = value.ToString($"G{significantDigits}", CultureInfo.InvariantCulture);
        double cleansedValue = double.Parse(formatted, CultureInfo.InvariantCulture);
        writer.WriteNumberValue(cleansedValue);
    }
}
