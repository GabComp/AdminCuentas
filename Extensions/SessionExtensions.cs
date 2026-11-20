using System.Text.Json;

namespace PrototipoComapa.Extensions
{
    public static class SessionExtensions
    {
        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
