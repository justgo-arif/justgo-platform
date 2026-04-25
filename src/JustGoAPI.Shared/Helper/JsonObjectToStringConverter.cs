using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustGoAPI.Shared.Helper
{
    public class JsonObjectToStringConverter : JsonConverter<string>
    {
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                writer.WriteNull();
                return;
            }
            // Always write string values as-is during serialization
            writer.WriteValue(value);           
        }

        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return reader.Value?.ToString();
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // Read the entire JSON object and serialize it back to string
                var jObject = JObject.Load(reader);
                return jObject.ToString(Formatting.None);
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            throw new JsonSerializationException($"Unable to convert token type {reader.TokenType} to string for Config property.");
        }
    }
}
