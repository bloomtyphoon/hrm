using System.Text.Json;
using System.Text.Json.Serialization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// JSON converter for <see cref="DataScopeLevel"/>.
/// Serializes as the integer ID and deserializes from an integer ID
/// using <see cref="DataScopeLevel.FromId"/>.
/// </summary>
public sealed class DataScopeLevelJsonConverter : JsonConverter<DataScopeLevel>
{
    public override DataScopeLevel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number)
        {
            var id = reader.GetInt32();
            return DataScopeLevel.FromId(id);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value is not null && int.TryParse(value, out var id))
                return DataScopeLevel.FromId(id);

            if (value is not null)
                return DataScopeLevel.FromName(value);
        }

        throw new JsonException($"Cannot convert token of type {reader.TokenType} to DataScopeLevel.");
    }

    public override void Write(Utf8JsonWriter writer, DataScopeLevel value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue(value.Id);
    }
}
