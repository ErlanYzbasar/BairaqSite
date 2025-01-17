using Microsoft.AspNetCore.Http;

namespace COMMON;

public static class SessionExtension
{
    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonHelper.SerializeObject(value));
    }

    public static T Get<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default(T) : JsonHelper.DeserializeObject<T>(value);
    }
}