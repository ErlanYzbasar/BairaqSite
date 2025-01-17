using System.Collections.Concurrent;
using MODEL.FormatModels;
using MaxMind.GeoIP2;

// using MaxMind.GeoIP2;

namespace COMMON;

public class QarSingleton
{
    private static QarSingleton _instance;
    private static readonly object Lock = new();
    private readonly ConcurrentDictionary<string, bool> _checkRunDictionary = new();
    private readonly ConcurrentDictionary<string, int> _intDictionary = new();
    private readonly ConcurrentDictionary<int, int> _reLoginAdminDictionary = new();
    private readonly ConcurrentDictionary<string, string> _stringDictionary = new();
    private readonly ConcurrentQueue<int> _queueViewArticleId = new();


    //private ConcurrentDictionary<string, ConcurrentBag<string>> sessionIdDictionary = new ConcurrentDictionary<string, ConcurrentBag<string>>();
    private readonly DatabaseReader geoIPReader;

    private QarSingleton()
    {
        // var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBFiles", "GeoLite2-City.mmdb");
        // geoIPReader = new DatabaseReader(dbPath);
    }

    public static QarSingleton GetInstance()
    {
        if (_instance == null)
            lock (Lock)
            {
                _instance ??= new QarSingleton();
            }

        return _instance;
    }

    #region ConnectionString мәнін алу және сақтау

    public string GetConnectionString()
    {
        var key = "connectionString";
        if (_stringDictionary.ContainsKey(key)) return _stringDictionary[key] ?? string.Empty;

        return null;
    }

    public void SetConnectionString(string connectionString)
    {
        var key = "connectionString";
        _stringDictionary.AddOrUpdate(key, connectionString, (oldKey, oldValue) => connectionString);
    }

    #endregion

    #region Site Theme мәнін алу және сақтау

    public string GetSiteTheme()
    {
        var key = "siteTheme";
        if (_stringDictionary.ContainsKey(key)) return _stringDictionary[key] ?? string.Empty;

        return null;
    }

    public void SetSiteTheme(string connectionString)
    {
        var key = "siteTheme";
        _stringDictionary.AddOrUpdate(key, connectionString, (oldKey, oldValue) => connectionString);
    }

    #endregion

    #region Site Url мәнін алу және сақтау

    public string GetSiteUrl()
    {
        var key = "siteUrl";
        if (_stringDictionary.ContainsKey(key)) return _stringDictionary[key] ?? string.Empty;

        return null;
    }

    public void SetSiteUrl(string siteUrl)
    {
        var key = "siteUrl";
        _stringDictionary.AddOrUpdate(key, siteUrl, (oldKey, oldValue) => siteUrl);
    }

    #endregion

    #region Қайта кіруі керек болған басқарушылар

    public void AddReLoginAdmin(int adminId, int updateTime)
    {
        if (_reLoginAdminDictionary.ContainsKey(adminId))
            _reLoginAdminDictionary[adminId] = updateTime;
        else
            _reLoginAdminDictionary.AddOrUpdate(adminId, updateTime, (oldKey, oldValue) => updateTime);
    }

    public bool IsReLoginAdmin(int adminId, out int updateTime)
    {
        if (_reLoginAdminDictionary.ContainsKey(adminId))
        {
            updateTime = _reLoginAdminDictionary[adminId];
            return true;
        }

        updateTime = 0;
        return false;
    }

    public void RemoveReLoginAdmin(int adminId)
    {
        _reLoginAdminDictionary.TryRemove(adminId, out _);
    }

    #endregion

    #region ConnectionId қосу немесе жоию ConnectionId

    //public void AddConnectionId(string sessionId, string connectionId)
    //{
    //    if (sessionIdDictionary.ContainsKey(sessionId) && !sessionIdDictionary[sessionId].Contains(connectionId))
    //    {
    //        sessionIdDictionary[sessionId].Add(connectionId);
    //    }
    //    else
    //    {
    //        ConcurrentBag<string> connectionList = new ConcurrentBag<string>() { connectionId };
    //        sessionIdDictionary.TryAdd(sessionId, connectionList);
    //    }
    //}

    //public bool RemoveConnectionId(string connectionId)
    //{
    //    foreach(var item in sessionIdDictionary)
    //    {
    //        ConcurrentBag<string> connectionList = item.Value;
    //        if(connectionList.Count == 0)
    //        {
    //            sessionIdDictionary.TryRemove(item.Key, out ConcurrentBag<string> oldValue);
    //        }

    //        if (connectionList.Contains(connectionId))
    //        {
    //            connectionList.TryTake(out connectionId);
    //        }
    //    }
    //    return false;
    //}

    //public ConcurrentDictionary<string, ConcurrentBag<string>> GetAllSessionIdDictionary()
    //{
    //    return sessionIdDictionary;
    //}

    #endregion

    #region Status мәнін алу және сақтау

    public bool GetRunStatus(string key)
    {
        if (_checkRunDictionary.ContainsKey(key)) return _checkRunDictionary[key];

        return false;
    }

    public void SetRunStatus(string key, bool status)
    {
        if (status)
            _checkRunDictionary.AddOrUpdate(key, status, (oldKey, oldValue) => status);
        else
            _checkRunDictionary.TryRemove(key, out var value);
    }

    #endregion

    #region Get City By IP +GetCityByIP(string ipAddress)

    public string GetCityByIP(string ipAddress)
    {
        if (ipAddress.Equals("::1")) return "";
        try
        {
            var city = geoIPReader.City(ipAddress);
            return city.City.Name;
        }
        catch
        {
            return "";
        }
    }

    #endregion


    #region Көрілген мақалалар Id лары

    public void EnqueueViewArticleId(int viewArticleId)
    {
        if (viewArticleId <= 0) return;
        _queueViewArticleId.Enqueue(viewArticleId);
    }

    public int DequeueViewArticleId()
    {
        return _queueViewArticleId.TryDequeue(out var viewArticleId) && viewArticleId > 0 ? viewArticleId : 0;
    }

    public int ViewArticleIdCount()
    {
        return _queueViewArticleId.Count;
    }

    #endregion

    // #region Get City Wither Temp

    // public string GetCityTemp(string city)
    // {
    //     if (witherDictionary.ContainsKey(city))
    //     {
    //         int time = UnixTimeHelper.ConvertToUnixTime(DateTime.Now.AddMinutes(-9));
    //         if (time < witherDictionary[city].Time)
    //         {
    //             Task.Run(async () =>
    //             {
    //                 witherDictionary[city].Temp = await WeatherHelper.GetCurrentWeatherAsync(city);
    //             }).Wait();
    //         }

    //         return witherDictionary[city].Temp;
    //     }
    //     else
    //     {
    //         Task.Run(async () =>
    //         {
    //             witherDictionary.TryAdd(city, new WitherInfoModel()
    //             {
    //                 Time = UnixTimeHelper.ConvertToUnixTime(DateTime.Now),
    //                 Temp = await WeatherHelper.GetCurrentWeatherAsync(city)
    //             });
    //         }).Wait();
    //         return witherDictionary[city].Temp;
    //     }
    // }

    // #endregion

    #region Get and set int value

    public int GetIntValue(string key, int defaultValue = 0)
    {
        if (_intDictionary.TryGetValue(key, out var res)) return res;
        return defaultValue;
    }

    public void SetIntValue(string key, int value)
    {
        _intDictionary.AddOrUpdate(key, value, (oldKey, oldValue) => value);
    }

    #endregion
}