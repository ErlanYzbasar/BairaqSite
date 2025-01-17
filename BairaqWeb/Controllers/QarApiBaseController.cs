using System.Data;
using System.Security.Claims;
using System.Text;
using COMMON;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MODEL;
using BairaqWeb.Caches;

namespace BairaqWeb.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QarApiBaseController : ControllerBase
{
    protected readonly IConfiguration _configuration;
    protected readonly IMemoryCache _memoryCache;
    protected readonly string[] ImageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg" };
    protected const string CurrencyType = "USD2KGS";

    public QarApiBaseController(IMemoryCache memoryCache, IConfiguration configuration)
    {
        _memoryCache = memoryCache;
        _configuration = configuration;
    }

    protected bool IsApi => HttpContext.Request.Headers.ContainsKey("X-Client-Platform");

    protected string CurrentLanguage => HttpContext.Request.Headers.TryGetValue("language", out var language)
        ? language.ToString()
        : QarCache.GetLanguageList(_memoryCache).FirstOrDefault(x => x.IsDefault == 1)?.LanguageCulture ?? string.Empty;


    #region Тіл аудармасын алу +T(string language, string localKey)

    public string T(string language, string localKey)
    {
        language ??= string.Empty;
        var languageList = QarCache.GetLanguageList(_memoryCache);
        if (!languageList.Exists(x => x.LanguageCulture.Equals(language, StringComparison.OrdinalIgnoreCase)))
        {
            var defaultLanguage = QarCache.GetLanguageList(_memoryCache)
                .FirstOrDefault(x => x.IsDefault == 1 && x.BackendDisplay == 1);
            if (defaultLanguage == null)
                defaultLanguage = QarCache.GetLanguageList(_memoryCache).FirstOrDefault(x => x.BackendDisplay == 1);
            if (defaultLanguage != null) language = defaultLanguage.LanguageCulture;
        }

        return QarCache.GetLanguageValue(_memoryCache, localKey, language);
    }

    public string T(string localKey)
    {
        // var language = CurrentLanguage;
        // var languageList = QarCache.GetLanguageList(_memoryCache);
        // if (!languageList.Exists(x => x.LanguageCulture.Equals(language, StringComparison.OrdinalIgnoreCase)))
        // {
        //     var defaultLanguage = QarCache.GetLanguageList(_memoryCache)
        //         .FirstOrDefault(x => x.IsDefault == 1 && x.BackendDisplay == 1);
        //     if (defaultLanguage == null)
        //         defaultLanguage = QarCache.GetLanguageList(_memoryCache).FirstOrDefault(x => x.BackendDisplay == 1);
        //     if (defaultLanguage != null) language = defaultLanguage.LanguageCulture;
        // }

        return QarCache.GetLanguageValue(_memoryCache, localKey, CurrentLanguage);
    }

    #endregion

    #region Қолданушының IP әдіресін алу +GetIPAddress()

    public string GetIPAddress()
    {
        var locationIP = HttpContext.Connection.RemoteIpAddress.ToString();
        if (HttpContext.Request.Headers["X-Real-IP"].Count() > 0) locationIP = HttpContext.Request.Headers["X-Real-IP"];

        return locationIP;
    }

    #endregion


    #region Save Person Login Info To Cookie + SavePersonLoginInfoToCookie(string email, string realName, int adminId, string roleIdList, string roleNames, bool isSuperAdmin, string avatarUrl, string skinName)

    public void SavePersonLoginInfoToCookie(string email, string realName, int personId,
        string avatarUrl, string skinName, string whatsapp, string secondaryPhone)
    {
        var identity = new ClaimsIdentity("AccountLogin");
        identity.AddClaim(new Claim(ClaimTypes.Email, email));
        identity.AddClaim(new Claim("RealName", realName));
        identity.AddClaim(new Claim("PersonId", personId.ToString()));
        identity.AddClaim(new Claim("AvatarUrl", avatarUrl));
        identity.AddClaim(new Claim("SkinName", skinName));
        identity.AddClaim(new Claim("Whatsapp", whatsapp));
        identity.AddClaim(new Claim("SecondaryPhone", secondaryPhone));
        identity.AddClaim(new Claim("LoginTime", UnixTimeHelper.ConvertToUnixTime(DateTime.Now).ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Person"));
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(100),
            IsPersistent = true
        };
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    #endregion

    #region Cookie-ге сақтау + SaveToCookie(string cookieKey, string cookieValue)

    public void SaveToCookie(string cookieKey, string cookieValue)
    {
        Response.Cookies.Delete(cookieKey);
        Response.Cookies.Append(cookieKey, cookieValue, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(100),
            Path = "/",
            HttpOnly = true
        });
    }

    #endregion

    #region Cookie-дегі сақталған мәнді алу +GetCookieValue(string cookieKey)

    public string GetCookieValue(string cookieKey)
    {
        if (Request.Cookies.TryGetValue(cookieKey, out var cookieValue)) return cookieValue;

        return "";
    }

    #endregion

    #region Параметрдің int мәнін алу +GetIntQueryParam(string paramName, int defaultValue)

    public int GetIntQueryParam(string paramName, int defaultValue)
    {
        var strParamName = string.Empty;
        var param = 0;
        try
        {
            strParamName = Request.Form[paramName].ToString();
        }
        catch
        {
            strParamName = string.Empty;
        }

        if (string.IsNullOrEmpty(strParamName) || !int.TryParse(strParamName, out param))
        {
            strParamName = HttpContext.Request.Query[paramName].ToString();
            if (!int.TryParse(strParamName, out param)) param = defaultValue;
        }

        return param;
    }

    #endregion

    #region Параметрдің int мәнін алу +GetStringQueryParam(string paramName, string defaultValue = "")

    public string GetStringQueryParam(string paramName, string defaultValue = "")
    {
        var strParamName = defaultValue;
        try
        {
            strParamName = Request.Form[paramName].ToString();
        }
        catch
        {
            try
            {
                strParamName = HttpContext.Request.Query[paramName].ToString();
            }
            catch
            {
                strParamName = defaultValue;
            }
        }

        return strParamName;
    }

    #endregion

    #region Пәрәметір uint Тізбек мәнін алу +GetIntListQueryParam(string paramName)

    public List<uint> GetIntListQueryParam(string paramName)
    {
        var paramIds = string.Empty;
        try
        {
            paramIds = Request.Form[paramName].ToString();
        }
        catch
        {
            paramIds = string.Empty;
        }

        if (string.IsNullOrEmpty(paramIds)) paramIds = HttpContext.Request.Query[paramName].ToString();
        var paramIdList = new List<uint>();
        foreach (var idStr in paramIds.Split(','))
            if (uint.TryParse(idStr, out var id) && id > 0)
                paramIdList.Add(id);

        return paramIdList;
    }

    #endregion

    #region Көп түрлі тіл мәндерін алу +GetMultilanguageList(IDbConnection _connection, string tableName, List<int> columnIdList, List<string> columnNameList = null, string language = "")

    protected List<Multilanguage> GetMultilanguageList(IDbConnection _connection, string tableName,
        List<int> columnIdList, List<string> columnNameList = null, string language = "")
    {
        if (columnIdList == null || columnIdList.Count() == 0) return new List<Multilanguage>();
        tableName = tableName.Trim().ToLower();
        var columnIdArrIn = "(" + string.Join(",", columnIdList.ToArray()) + ")";
        var querySql = $"where qStatus in (0,7) and columnId in {columnIdArrIn} and tableName = @tableName ";
        object queryObj = new { tableName, language };
        if (!string.IsNullOrEmpty(language)) querySql += " and language = @language ";

        if (columnNameList != null && columnNameList.Count > 0)
        {
            var columnNameArrIn = "(" + string.Join(",", columnNameList.Select(x => "'" + x + "'").ToArray()) + ")";
            querySql += $" and columnName in {columnNameArrIn} ";
        }

        return _connection.GetList<Multilanguage>(querySql, queryObj).ToList();
    }

    #endregion
}