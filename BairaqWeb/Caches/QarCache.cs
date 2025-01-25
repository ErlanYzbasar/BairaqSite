using System.Data;
using System.Reflection;
using BairaqWeb.Attributes;
using BairaqWeb.Controllers;
using COMMON;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Mvc.Controllers;
using MODEL;
using MODEL.ViewModels;
using static COMMON.WeatherHelper;

namespace BairaqWeb.Caches;

public class QarCache
{
    #region Update Entity List With MultiLanguage +UpdateEntityListWithMultiLanguage<T>(IDbConnection _connection, List<T> entities, string language, List<string> keyList) where T : class

    public static void UpdateEntityListWithMultiLanguage<T>(IDbConnection connection, List<T> entities,
        string language, List<string> keyList) where T : class
    {
        if (entities == null || !entities.Any())
            return;

        var ids = entities.Select(e => (int)e.GetType().GetProperty("Id").GetValue(e)).ToList();
        var multiLanguageList = QarBaseController.GetMultilanguageList(connection, typeof(T).Name, ids, null, language)
            .ToList();

        foreach (var entity in entities)
        foreach (var key in keyList)
        {
            var multiLanguageItem = multiLanguageList.FirstOrDefault(x =>
                x.ColumnId == (int)entity.GetType().GetProperty("Id").GetValue(entity) &&
                x.ColumnName.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (multiLanguageItem != null && !string.IsNullOrEmpty(multiLanguageItem.ColumnValue))
            {
                var propertyInfo = entity.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                    propertyInfo.SetValue(entity, multiLanguageItem.ColumnValue, null);
            }
        }
    }

    #endregion

    #region Clear Cache +ClearCache(IMemoryCache _memoryCache, string cacheName)

    public static void ClearCache(IMemoryCache memoryCache, string cacheName)
    {
        memoryCache.Remove(cacheName);
        memoryCache.Remove($"{cacheName}_1");
        foreach (var language in GetLanguageList(memoryCache))
        {
            memoryCache.Remove($"{cacheName}_{language.LanguageCulture}");
            for (var i = 1; i <= 30; i++)
            {
                memoryCache.Remove($"{cacheName}_{language.LanguageCulture}_{i}");
                foreach (var category in GetCategoryList(memoryCache, language.LanguageCulture))
                    memoryCache.Remove($"{cacheName}_{language.LanguageCulture}_{i}_{category.Id}");
            }
        }
    }

    #endregion

    #region Сайт тілдерін алу +GetLanguageList(IMemoryCache _memoryCache)

    public static List<Language> GetLanguageList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Language> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Language>("where qStatus = 0").Select(x => new Language
            {
                Id = x.Id,
                ShortName = x.ShortName,
                FullName = x.FullName,
                LanguageCulture = x.LanguageCulture,
                IsDefault = x.IsDefault,
                IsSubLanguage = x.IsSubLanguage,
                FrontendDisplay = x.FrontendDisplay,
                BackendDisplay = x.BackendDisplay,
                LanguageFlagImageUrl = x.LanguageFlagImageUrl,
                DisplayOrder = x.DisplayOrder
            }).OrderBy(x => x.DisplayOrder).ToList();
            memoryCache.Set(cacheName, list, TimeSpan.FromDays(7));
        }

        return list;
    }

    #endregion

    #region Тіл бумасын алу +GetLanguagePackDictionary(IMemoryCache _memoryCache)

    public static Dictionary<string, Dictionary<string, string>> GetLanguagePackDictionary(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out Dictionary<string, Dictionary<string, string>> allLanguagePackDic))
        {
            allLanguagePackDic = new Dictionary<string, Dictionary<string, string>>();
            var languageList = GetLanguageList(memoryCache);
            string[] canConvertLanguages = { "tote", "latyn" };
            using var connection = Utilities.GetOpenConnection();
            var jsonLangaugePack = LanguagePackHelper.GetLanguagePackJsonString();
            if (!string.IsNullOrEmpty(jsonLangaugePack))
            {
                var languagePackDictionary =
                    JsonHelper.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonLangaugePack);
                foreach (var entry in languagePackDictionary)
                    if (!allLanguagePackDic.ContainsKey(entry.Key))
                    {
                        var currentLanguagePackDic = new Dictionary<string, string>();
                        foreach (var valueEntry in entry.Value)
                        {
                            var key = valueEntry.Key.ToLower().Trim();
                            if (languageList.Exists(x =>
                                    x.LanguageCulture.Equals(key, StringComparison.OrdinalIgnoreCase) ||
                                    (key.Equals("kz", StringComparison.OrdinalIgnoreCase) &&
                                     canConvertLanguages.Contains(x.LanguageCulture))))
                            {
                                if (key.Equals("kz", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (languageList.Exists(x =>
                                            x.LanguageCulture.Equals("kz", StringComparison.OrdinalIgnoreCase)) &&
                                        !currentLanguagePackDic.ContainsKey("kz"))
                                        currentLanguagePackDic.Add("kz", valueEntry.Value);

                                    if (languageList.Exists(x =>
                                            x.LanguageCulture.Equals("latyn",
                                                StringComparison.OrdinalIgnoreCase)) &&
                                        !currentLanguagePackDic.ContainsKey("latyn"))
                                        currentLanguagePackDic.Add("latyn",
                                            Cyrl2LatynHelper.Cyrl2Latyn(valueEntry.Value));

                                    if (languageList.Exists(x =>
                                            x.LanguageCulture.Equals("tote", StringComparison.OrdinalIgnoreCase)) &&
                                        !currentLanguagePackDic.ContainsKey("tote"))
                                        currentLanguagePackDic.Add("tote",
                                            Cyrl2ToteHelper.Cyrl2Tote(valueEntry.Value));
                                }
                                else
                                {
                                    if (!currentLanguagePackDic.ContainsKey(key))
                                        currentLanguagePackDic.Add(key, valueEntry.Value);
                                }
                            }
                        }

                        allLanguagePackDic.Add(entry.Key, currentLanguagePackDic);
                    }

                memoryCache.Set(cacheName, allLanguagePackDic, TimeSpan.FromDays(1));
            }
        }

        return allLanguagePackDic;
    }

    #endregion

    #region Тіл аудармасын алу +GetLanguageValue(IMemoryCache _memoryCache,string localKey, string language)

    public static string GetLanguageValue(IMemoryCache memoryCache, string localKey, string language)
    {
        language = (language ?? string.Empty).ToLower().Trim();
        var languagePackDictionary = GetLanguagePackDictionary(memoryCache);
        if (languagePackDictionary.ContainsKey(localKey) && languagePackDictionary[localKey].ContainsKey(language))
        {
            if (!string.IsNullOrEmpty(languagePackDictionary[localKey][language]))
                return languagePackDictionary[localKey][language];
            return languagePackDictionary[localKey]["en"];
        }

        return localKey;
    }

    #endregion

    #region Барлық мениюлерді алу +GetNavigationList(IMemoryCache _memoryCache, int navigationTypeId = 1)

    public static List<Navigation> GetNavigationList(IMemoryCache memoryCache, int navigationTypeId = 1)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{navigationTypeId}";
        if (!memoryCache.TryGetValue(cacheName, out List<Navigation> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection
                .GetList<Navigation>(
                    "where qStatus = 0 and navigationTypeId =  @navigationTypeId order by displayOrder asc ",
                    new { navigationTypeId }).ToList();
            memoryCache.Set(cacheName, list, DateTimeOffset.MaxValue);
        }

        return list;
    }

    #endregion

    #region Get Current Weather Async +GetCurrentWeatherAsync(IMemoryCache _memoryCache, int locationIP)

    public static async Task<WeatherInfo> GetCurrentWeatherAsync(IMemoryCache memoryCache, string locationIP)
    {
        var currentCity = await WeatherHelper.GetCityByIpAsync(locationIP);

        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{currentCity}";
        if (!memoryCache.TryGetValue(cacheName, out WeatherInfo weatherData))
        {
            weatherData = await GetWeatherByIpAsync(currentCity);
            memoryCache.Set(cacheName, weatherData, TimeSpan.FromHours(3));
        }

        return weatherData;
    }

    #endregion

    #region Get Category List+ GetCategoryList(IMemoryCache _memoryCache, string language = "")

    public static List<Articlecategory> GetCategoryList(IMemoryCache memoryCache, string language = "")
    {
        language = language switch
        {
            "latyn" or "tote" => "kz",
            _ => language
        };

        var cacheName =
            $"{MethodBase.GetCurrentMethod()!.Name}{(string.IsNullOrEmpty(language) ? "" : $"_{language}")}";
        if (memoryCache.TryGetValue(cacheName, out List<Articlecategory> list)) return list;
        using var connection = Utilities.GetOpenConnection();
        var querySql = "where qStatus = 0 ";

        if (!string.IsNullOrEmpty(language)) querySql += " and language = @language ";
        querySql += " order by displayOrder asc";
        list = connection.GetList<Articlecategory>(querySql, new { language }).ToList();
        memoryCache.Set(cacheName, list, TimeSpan.FromHours(10));
        return list;
    }

    #endregion

    #region Get Site Setting +GetSiteSetting(IMemoryCache _memoryCache)

    public static Sitesetting GetSiteSetting(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out Sitesetting siteSetting))
        {
            using var connection = Utilities.GetOpenConnection();
            siteSetting = connection.GetList<Sitesetting>("where qStatus = 0 ").FirstOrDefault();
            if (siteSetting == null)
            {
                siteSetting = new Sitesetting
                {
                    LightLogo = "",
                    DarkLogo = "",
                    MobileLightLogo = "",
                    MobileDarkLogo = "",
                    Title = "",
                    Description = "",
                    Keywords = "",
                    AnalyticsHtml = "",
                    Copyright = "",
                    AnalyticsScript = "",
                    Address = "",
                    Phone = "",
                    Email = "",
                    MapEmbed = "",
                    Facebook = "",
                    Twitter = "",
                    Instagram = "",
                    Vk = "",
                    SEmail = "",
                    Telegram = "",
                    Youtube = "",
                    Whatsapp = "",
                    Tiktok = "",
                    LoginPath = "",
                    MaxErrorCount = 10,
                    Favicon = "",
                    QStatus = 0
                };
                siteSetting.Id = connection.Insert(siteSetting) ?? 0;
            }

            memoryCache.Set(cacheName, siteSetting, DateTimeOffset.MaxValue);
        }

        return siteSetting;
    }

    #endregion

    #region Get Additional Content List +GetAdditionalContentList(IMemoryCache _memoryCache, string language)

    public static List<Additionalcontent> GetAdditionalContentList(IMemoryCache memoryCache, string language)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}";
        if (!memoryCache.TryGetValue(cacheName, out List<Additionalcontent> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Additionalcontent>("where qStatus = 0 ").ToList();
            UpdateEntityListWithMultiLanguage(connection, list, language,
                new List<string>
                {
                    nameof(Additionalcontent.Title), nameof(Additionalcontent.ShortDescription),
                    nameof(Additionalcontent.FullDescription)
                });
            memoryCache.Set(cacheName, list, TimeSpan.FromHours(1));
        }

        return list;
    }

    #endregion

    #region Get Article List +GetArticleList(IMemoryCache _memoryCache,string language)

    public static List<Article> GetArticleList(IMemoryCache memoryCache, string language)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var categoryIdList = GetCategoryList(memoryCache, language).Where(x => x.ParentId == 0).Select(x => x.Id)
                .ToArray();
            if (categoryIdList.Length > 0)
                list = connection
                    .GetList<Article>(
                        $"where qStatus = 0 and categoryId in ({string.Join(",", categoryIdList)}) order by addTime desc ")
                    .Select(x => new Article
                    {
                        LatynUrl = x.LatynUrl,
                        Title = x.Title,
                        AddTime = x.AddTime,
                        ShortDescription = x.ShortDescription,
                        ThumbnailUrl = x.ThumbnailUrl,
                        CategoryId = x.CategoryId
                    }).ToList();
            else
                list = new List<Article>();

            memoryCache.Set(cacheName, list, TimeSpan.FromHours(1));
        }

        return list;
    }

    #endregion

    #region Get Role List +GetRoleList(IMemoryCache _memoryCache, string language)

    public static List<Role> GetRoleList(IMemoryCache memoryCache, string language)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}";
        if (!memoryCache.TryGetValue(cacheName, out List<Role> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Role>("where qStatus = 0 ").ToList();
            UpdateEntityListWithMultiLanguage(connection, list, language,
                new List<string> { nameof(Role.Name), nameof(Role.Description) });
            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(10));
        }

        return list;
    }

    #endregion

    #region Get Role List +GetRoleList(IMemoryCache _memoryCache, string language)

    public static List<Adminrole> GetAdminroleList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Adminrole> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Adminrole>("where qStatus = 0 ").ToList();
            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(10));
        }

        return list;
    }

    #endregion

    #region Get Permission List +GetPermissionList(IMemoryCache _memoryCache)

    public static List<Permission> GetPermissionList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Permission> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Permission>("where qStatus = 0").ToList();
            memoryCache.Set(cacheName, list, DateTimeOffset.MaxValue);
        }

        return list;
    }

    #endregion

    #region Get Role Permission List +GetRolePermissionList(IMemoryCache _memoryCache)

    public static List<Rolepermission> GetRolePermissionList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Rolepermission> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Rolepermission>("where qStatus = 0").ToList();
            memoryCache.Set(cacheName, list, DateTimeOffset.MaxValue);
        }

        return list;
    }

    #endregion

    #region Get Navigation Id List By Role Id +GetNavigationIdListByRoleId(IMemoryCache _memoryCache, int roleId)

    public static List<int> GetNavigationIdListByRoleId(IMemoryCache memoryCache, int roleId)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{roleId}";
        if (!memoryCache.TryGetValue(cacheName, out List<int> list))
        {
            list = new List<int>();
            var rolePermissionList = GetRolePermissionList(memoryCache).Where(x => roleId == x.RoleId).ToList();
            var navigationList = GetNavigationList(memoryCache);
            foreach (var navigation in navigationList.Where(x => x.ParentId == 0).ToList())
            foreach (var childNavigation in navigationList.Where(x => x.ParentId == navigation.Id).ToList())
                if (rolePermissionList.Exists(r =>
                        r.TableName.Equals(nameof(Navigation), StringComparison.OrdinalIgnoreCase)
                        && r.ColumnId == childNavigation.Id))
                {
                    list.Add(childNavigation.Id);
                    if (!list.Contains(navigation.Id)) list.Add(navigation.Id);
                }
        }

        return list;
    }

    #endregion

    #region Admin қатарын алу +GetAllAdminList(IMemoryCache _memoryCache)

    public static List<Admin> GetAllAdminList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Admin> adminList))
        {
            using var connection = Utilities.GetOpenConnection();
            adminList = connection.GetList<Admin>("where 1 = 1 ").ToList();
            memoryCache.Set(cacheName, adminList, TimeSpan.FromDays(10));
        }

        return adminList ?? new List<Admin>();
    }

    #endregion


    #region Check Navigation Permission +CheckNavigationPermission(IMemoryCache _memoryCache, List<int> roleIdList, Controller controller, Action action, string method, out bool canView, out bool canCreate, out bool canEdit, out bool canDelete)

    public static void CheckNavigationPermission(IMemoryCache memoryCache, List<int> roleIdList, Controller controller,
        ControllerActionDescriptor action, string method, out bool canView, out bool canCreate, out bool canEdit,
        out bool canDelete)
    {
        roleIdList ??= new List<int>();

        var actionName = action.ActionName.ToLower();
        var controllerName = action.ControllerName.ToLower();
        var controllerAttributes = controller.GetType().GetCustomAttribute<NoRoleAttribute>(false);
        var actionAttributes = action.MethodInfo.GetCustomAttributes<NoRoleAttribute>(false);
        if (controllerAttributes != null || (actionAttributes != null && actionAttributes.Any()))
        {
            canView = canCreate = canEdit = canDelete = true;
            return;
        }

        if (method.Equals("POST"))
        {
            if (actionName.StartsWith("get") && actionName.EndsWith("list"))
                actionName = actionName[3..^4];
            else if (actionName.StartsWith("set") && actionName.EndsWith("status")) actionName = actionName[3..^6];
        }

        var url = $"/{controllerName}/{actionName}/list";
        var navigationId = GetNavigationList(memoryCache).FirstOrDefault(x => x.NavUrl.Equals(url))?.Id ?? 0;

        canView = canCreate = canEdit = canDelete = false;
        if (navigationId == 0) return;

        var viewPermissionId = GetPermissionList(memoryCache)
            .FirstOrDefault(x => x.ManageType.Equals("view", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
        var createPermissionId = GetPermissionList(memoryCache)
            .FirstOrDefault(x => x.ManageType.Equals("create", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
        var editPermissionId = GetPermissionList(memoryCache)
            .FirstOrDefault(x => x.ManageType.Equals("edit", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
        var deletePermissionId = GetPermissionList(memoryCache)
            .FirstOrDefault(x => x.ManageType.Equals("delete", StringComparison.OrdinalIgnoreCase))?.Id ?? 0;

        canView = GetRolePermissionList(memoryCache).Exists(x =>
            x.TableName.Equals(nameof(Navigation)) && x.ColumnId == navigationId && roleIdList.Contains(x.RoleId) &&
            x.PermissionId == viewPermissionId);
        canCreate = GetRolePermissionList(memoryCache).Exists(x =>
            x.TableName.Equals(nameof(Navigation)) && x.ColumnId == navigationId && roleIdList.Contains(x.RoleId) &&
            x.PermissionId == createPermissionId);
        canEdit = GetRolePermissionList(memoryCache).Exists(x =>
            x.TableName.Equals(nameof(Navigation)) && x.ColumnId == navigationId && roleIdList.Contains(x.RoleId) &&
            x.PermissionId == editPermissionId);
        canDelete = GetRolePermissionList(memoryCache).Exists(x =>
            x.TableName.Equals(nameof(Navigation)) && x.ColumnId == navigationId && roleIdList.Contains(x.RoleId) &&
            x.PermissionId == deletePermissionId);
    }

    #endregion

    #region Check Article Permission +CheckArticlePermission(IMemoryCache _memoryCache, List<int> roleIdList, string manageType)

    public static bool CheckArticlePermission(IMemoryCache memoryCache, List<int> roleIdList, string manageType)
    {
        var manageTypePermissionId = GetPermissionList(memoryCache)
            .Where(x => x.ManageType.Equals(manageType, StringComparison.OrdinalIgnoreCase)).FirstOrDefault()?.Id ?? 0;
        return GetRolePermissionList(memoryCache).Exists(x =>
            x.TableName.Equals(nameof(Article)) && x.ColumnId == 0 && roleIdList.Contains(x.RoleId) &&
            x.PermissionId == manageTypePermissionId);
    }

    #endregion

    #region Мақалалар тызымын алу +GetArticleList(IMemoryCache _memoryCache, string language, int takeCount, int categoryId = 0)

    public static List<Article> GetArticleList(IMemoryCache memoryCache, string language, int takeCount,
        int categoryId = 0)
    {
        language = language switch
        {
            "latyn" or "tote" => "kz",
            _ => language
        };

        var cacheName =
            $"{MethodBase.GetCurrentMethod().Name}_{language}_{takeCount}{(categoryId > 0 ? $"_{categoryId}" : "")}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> articleList))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, categoryId,shortDescription, thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' ";
            if (categoryId > 0)
            {
                querySql += " and categoryId = @categoryId ";
            }
            else
            {
                var categoryIdArr = connection
                    .Query<int>("select id from articlecategory where qStatus = 0 and language = @language",
                        new { language }).ToArray();
                if (categoryIdArr.Length > 0)
                    querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";
            }

            articleList = connection.Query<Article>(querySql + " order by addTime desc limit @takeCount ",
                new { categoryId, takeCount }).ToList();
            if (articleList.Count <= 0 && categoryId > 0)
            {
                articleList = connection.Query<Article>(
                    "select id, title, categoryId,shortDescription, thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' and categoryId in (select id from articlecategory where parentId = @categoryId)" +
                    " order by addTime desc limit @takeCount ",
                    new { categoryId, takeCount }).ToList();
            }

            foreach (var article in articleList)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription.Substring(0, 150) + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, articleList, TimeSpan.FromMinutes(1));
        }

        return articleList;
    }

    #endregion
    
    #region Фокус мақалалар тызымын алу +GetFocusArticleList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Article> GetRegionArticleList(IMemoryCache memoryCache, string language, int takeCount)
    {
        language = language switch
        {
            "latyn" or "tote" => "kz",
            _ => language
        };
        var regionId = language.Equals("ru") ? 179 : language.Equals("en") ? 278 : 76;
        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, shortDescription,categoryId,thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' ";

            var categoryIdArr = connection
                .Query<int>("select id from articlecategory where qStatus = 0 and language = @language and parentId = @regionId",
                    new { language,regionId }).ToArray();
            if (categoryIdArr.Length > 0) querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";

            list = connection
                .Query<Article>(querySql + " order by addTime desc limit @takeCount ",
                    new { takeCount }).ToList();

            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription.Substring(0, 150) + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion

    #region Фокус мақалалар тызымын алу +GetFocusArticleList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Article> GetFocusArticleList(IMemoryCache memoryCache, string language, int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, shortDescription,categoryId,thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' ";

            var categoryIdArr = connection
                .Query<int>("select id from articlecategory where qStatus = 0 and language = @language",
                    new { language }).ToArray();
            if (categoryIdArr.Length > 0) querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";

            list = connection
                .Query<Article>(querySql + " and isFocusNews = 1 order by addTime desc limit @takeCount ",
                    new { takeCount }).ToList();

            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription.Substring(0, 150) + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion

    #region Бекітілген мақалалар тызымын алу +GetPinnedArticleList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Article> GetPinnedArticleList(IMemoryCache memoryCache, string language, int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, shortDescription,categoryId,thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' ";

            var categoryIdArr = GetCategoryList(memoryCache, language).Select(x => x.Id).ToArray();

            if (categoryIdArr.Length > 0) querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";

            list = connection
                .Query<Article>(querySql + " and isPinned = 1 order by updateTime desc limit @takeCount ",
                    new { takeCount }).ToList();

            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription[..150] + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion

    #region Көрлімі жоғары мақалалар тызымын алу +GetTopArticleList(IMemoryCache _memoryCache, string language, int dayCount, int takeCount)

    public static List<Article> GetTopArticleList(IMemoryCache memoryCache, string language, int dayCount,
        int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        dayCount = dayCount <= 0 ? 1 : dayCount;
        var addTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now.AddDays(-1 * dayCount));
        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{language}_{takeCount}";
        var categoryIds = GetCategoryList(memoryCache, language).Select(x => x.Id).ToArray();
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Article>(
                $"where qStatus = 0 and addTime >= {addTime} and categoryId in @categoryIds order by viewCount desc limit @takeCount",
                new { categoryIds, takeCount }).Select(x => new Article
            {
                Id = x.Id,
                Title = x.Title,
                ShortDescription = x.ShortDescription,
                CategoryId = x.CategoryId,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? "/images/no_image.png" : x.ThumbnailUrl,
                LatynUrl = x.LatynUrl,
                AddTime = x.AddTime,
                ViewCount = x.ViewCount
            }).ToList();
            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription.Substring(0, 150) + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion
    
    #region Get Youtube Article List +GetYoutubeArticleList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Article> GetYoutubeArticleList(IMemoryCache memoryCache, string language, int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod()?.Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Article>(
                "where qStatus = 0 and categoryId in (select id from articlecategory where qStatus = 0 and language = @language and displayOrder = 99) order by viewCount desc limit @takeCount",
                new { language, takeCount }).Select(x => new Article
            {
                Id = x.Id,
                Title = x.Title,
                ShortDescription = x.ShortDescription,
                CategoryId = x.CategoryId,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? "/images/no_image.png" : x.ThumbnailUrl,
                FullDescription = x.FullDescription,
                LatynUrl = x.LatynUrl,
                AddTime = x.AddTime,
                ViewCount = x.ViewCount
            }).ToList();
            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription[..150] + "..."
                    : article.ShortDescription;
            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion
    
    #region Валюта тізімін алу +GetCurrencyList(IMemoryCache _memoryCache)

    public static List<Currency> GetCurrencyList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Currency> list))
        {
            using var connection = Utilities.GetOpenConnection();
            list = connection.GetList<Currency>("where qStatus = 0 order by displayOrder").ToList();
            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion

    #region Бекітілген мақалалар тызымын алу +GetFeaturedArticleList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Article> GetFeaturedArticleList(IMemoryCache memoryCache, string language, int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Article> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, shortDescription,categoryId,thumbnailUrl, latynUrl, addTime, viewCount from article where qStatus = 0 and thumbnailUrl <> '' ";

            var categoryIdArr = connection
                .Query<int>("select id from articlecategory where qStatus = 0 and language = @language",
                    new { language }).ToArray();
            if (categoryIdArr.Length > 0) querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";

            list = connection
                .Query<Article>(querySql + " and isFeatured = 1 order by addTime desc limit @takeCount ",
                    new { takeCount }).ToList();

            foreach (var article in list)
                article.ShortDescription = article.ShortDescription.Length > 150
                    ? article.ShortDescription.Substring(0, 150) + "..."
                    : article.ShortDescription;

            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion

    #region +GetPopularTagList(IMemoryCache _memoryCache, string language, int takeCount)

    public static List<Tag> GetPopularTagList(IMemoryCache memoryCache, string language, int takeCount)
    {
        switch (language)
        {
            case "latyn":
            case "tote":
            {
                language = "kz";
            }
                break;
        }

        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}_{takeCount}";
        if (!memoryCache.TryGetValue(cacheName, out List<Tag> list))
        {
            using var connection = Utilities.GetOpenConnection();
            var querySql =
                " select id, title, latynUrl from tag where title <> '' order by taggedCount  desc limit @takeCount ";
//             @"
// SELECT 
//     t.id, 
//     t.title, 
//     t.latynUrl, 
//     tg.tag_count
// FROM 
//     tag t
// JOIN 
//     (SELECT 
//          tagId, 
//          COUNT(tagId) AS tag_count
//      FROM 
//          taglog
//      WHERE 
//          taggedTime >= NOW() - INTERVAL 1 MONTH
//      GROUP BY 
//          tagId
//      ORDER BY 
//          tag_count DESC
//      LIMIT 10) tg
// ON 
//     t.id = tg.tagId
// WHERE 
//     t.title <> ''
// ORDER BY 
//     tg.tag_count DESC
// LIMIT 
//     @takeCount;
// ";

            list = connection
                .Query<Tag>(querySql,
                    new { takeCount }).ToList();
            memoryCache.Set(cacheName, list, TimeSpan.FromMinutes(1));
        }

        return list;
    }

    #endregion


    #region GetQuestionModelList +GetPublishQuestion(IMemoryCache _memoryCache, string language)

    public static List<QuestionModel> GetPublishQuestionList(IMemoryCache memoryCache, string language)
    {
        language = language switch
        {
            "tote" or "latyn" => "kz",
            _ => language
        };

        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}";
        if (!memoryCache.TryGetValue(cacheName, out List<QuestionModel> questionList))
        {
            using var connection = Utilities.GetOpenConnection();
            questionList = connection
                .GetList<Question>(" where qStatus = 0 and language = @language ", new { language }).Select(x =>
                    new QuestionModel
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Type = x.Type,
                        Language = x.Language
                    }).ToList();
            if (questionList.Count > 0)
            {
                foreach (var question in questionList)
                {
                    question.AnswerList = connection.GetList<Answer>(
                            "where qStatus = 0 and  questionId = @questionId order by displayOrder asc ",
                            new { questionId = question.Id })
                        .Select(x => new AnswerModel
                        {
                            Id = x.Id,
                            QuestionId = x.QuestionId,
                            VoteCount = x.VoteCount,
                            Content = x.Content
                        }).ToList();

                    question.TolalVoteCount = question.AnswerList.Sum(x => x.VoteCount);
                }
            }

            memoryCache.Set(cacheName, questionList, TimeSpan.FromMinutes(1));
        }

        return questionList;
    }

    #endregion


    #region Advertise GetAdvertiselist (IMemoryCache memoryCache)

    public static List<Advertise> GetAdvertiseList(IMemoryCache memoryCache, string language)
    {
        language = language switch
        {
            "latyn" or "tote" => "kz",
            _ => language
        };

        var cacheName = $"{MethodBase.GetCurrentMethod().Name}_{language}";
        if (!memoryCache.TryGetValue(cacheName, out List<Advertise> list))
        {
            using (var _connection = Utilities.GetOpenConnection())
            {
                list = _connection.GetList<Advertise>(
                    "where qStatus = 0 and (language = @language OR language = 'all') order by addTime asc",
                    new { language }).ToList();
                memoryCache.Set(cacheName, list, TimeSpan.FromDays(1));
            }
        }

        return list;
    }

    #endregion

    #region Сайт тілдерін алу +GetAdlanguageList(IMemoryCache _memoryCache)

    public static List<Adlanguage> GetAdlanguageList(IMemoryCache memoryCache)
    {
        var cacheName = $"{MethodBase.GetCurrentMethod().Name}";
        if (!memoryCache.TryGetValue(cacheName, out List<Adlanguage> list))
            using (var connection = Utilities.GetOpenConnection())
            {
                list = connection.GetList<Adlanguage>("where qStatus = 0").Select(x => new Adlanguage
                {
                    Id = x.Id,
                    ShortName = x.ShortName,
                    FullName = x.FullName,
                    LanguageCulture = x.LanguageCulture,
                    IsDefault = x.IsDefault,
                    IsSubLanguage = x.IsSubLanguage,
                    FrontendDisplay = x.FrontendDisplay,
                    BackendDisplay = x.BackendDisplay,
                    LanguageFlagImageUrl = x.LanguageFlagImageUrl,
                    DisplayOrder = x.DisplayOrder
                }).OrderBy(x => x.DisplayOrder).ToList();
                memoryCache.Set(cacheName, list, TimeSpan.FromDays(7));
            }

        return list;
    }

    #endregion
}