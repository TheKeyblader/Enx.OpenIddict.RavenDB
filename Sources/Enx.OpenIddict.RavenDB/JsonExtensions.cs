using System.Text.Json;

namespace Enx.OpenIddict.RavenDB
{
    public static partial class JsonExtensions
    {
        public static JsonDocument JsonDocumentFromObject<TValue>(TValue? value, JsonSerializerOptions? options = default)
            => JsonDocumentFromObject(value, options);

        public static JsonDocument JsonDocumentFromObject(object? value, JsonSerializerOptions? options = default)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, options);
            return JsonDocument.Parse(bytes);
        }

        public static JsonElement JsonElementFromObject<TValue>(TValue? value, JsonSerializerOptions? options = default)
            => JsonElementFromObject(value, options);

        public static JsonElement JsonElementFromObject(object? value, JsonSerializerOptions? options = default)
        {
            using var doc = JsonDocumentFromObject(value, options);
            return doc.RootElement.Clone();
        }
    }
}
