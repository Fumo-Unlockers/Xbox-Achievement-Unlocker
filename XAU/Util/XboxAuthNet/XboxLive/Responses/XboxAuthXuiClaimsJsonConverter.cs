using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Responses
{
    public class XboxAuthXuiClaimsJsonConverter : JsonConverter<XboxAuthXuiClaims>
    {
        public override XboxAuthXuiClaims? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                return null;

            XboxAuthXuiClaims? claims = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propName = reader.GetString();

                if (propName == "xui")
                {
                    reader.Read();
                    claims = readXui(ref reader, options);
                }
                else
                    reader.Skip();
            }

            return claims;
        }

        private XboxAuthXuiClaims? readXui(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            XboxAuthXuiClaims? claims = null;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    if (claims == null && reader.TokenType == JsonTokenType.StartObject)
                        claims = JsonSerializer.Deserialize<XboxAuthXuiClaims>(ref reader, options);
                    else
                        reader.Skip();
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                claims = JsonSerializer.Deserialize<XboxAuthXuiClaims>(ref reader, options);
            }

            return claims;
        }

        public override void Write(Utf8JsonWriter writer, XboxAuthXuiClaims value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("xui");
            writer.WriteStartArray();
            JsonSerializer.Serialize(writer, value, options);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
