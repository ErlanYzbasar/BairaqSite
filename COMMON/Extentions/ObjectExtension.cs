namespace COMMON.Extensions;

public static class ObjectExtension
{
    public static T Clone<T>(this T source)
    {
        var json = JsonHelper.SerializeObject(source);
        return JsonHelper.DeserializeObject<T>(json);
    }
}