using Newtonsoft.Json;

namespace COMMON;

public static class JsonHelper
{
    #region Өбиектіні Json ге озгерту  +SerializeObject(object obj)

    public static string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    #endregion

    #region Json ды өбиектіге озгерту +DeserializeObject<T>(string str)

    public static T DeserializeObject<T>(string str)
    {
        return JsonConvert.DeserializeObject<T>(str);
    }

    #endregion
}