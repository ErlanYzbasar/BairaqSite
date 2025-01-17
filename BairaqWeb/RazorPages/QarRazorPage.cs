using BairaqWeb.Caches;
using COMMON;
using Microsoft.AspNetCore.Mvc.Razor;
using MODEL;

namespace BairaqWeb.RazorPages;

public abstract class QarRazorPage<TModel> : RazorPage<TModel>
{
    private IWebHostEnvironment _environment;

    protected string PagingHref(string keyword, int categoryId)
    {
        var keywordParam = !string.IsNullOrWhiteSpace(keyword) ? $"keyword={keyword}&" : "";

        return '/' + CurrentLanguage +
               (ActionName.Equals("category", StringComparison.OrdinalIgnoreCase)
                   ? $"/category/{CategoryList.FirstOrDefault(x => x.Id == categoryId)?.LatynUrl ?? ""}?"
                   : ActionName.Equals("tag", StringComparison.OrdinalIgnoreCase)
                       ? $"/tag/{ViewData["tagUrl"]}?"
                       :ActionName.Equals("author", StringComparison.OrdinalIgnoreCase) ?$"/author/{(ViewData["author"] as Admin)?.LatynUrl}?":"/article/list?" + keywordParam);
    }

    protected string T(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey)) return localKey;
        var memoryCache = ViewContext.HttpContext.RequestServices.GetService<IMemoryCache>();
        return QarCache.GetLanguageValue(memoryCache, localKey, CurrentLanguage);
    }

    protected List<T> QarList<T>(string vdName) where T : new()
    {
        if (ViewData[vdName] is List<T> value)
        {
            return value;
        }

        return new List<T>();
    }

    protected T QarModel<T>(string vdName)
    {
        if (ViewData[vdName] is T value)
        {
            return value;
        }

        return default;
    }

    protected string GetFullUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        _environment ??= ViewContext.HttpContext.RequestServices.GetService<IWebHostEnvironment>();

        if (_environment == null || _environment.IsDevelopment())
        {
            return url.StartsWith("http") ? url : PathHelper.Combine(SiteUrl, url);
        }

        return url;
    }

    protected Additionalcontent GetAc(string additionalType)
    {
        var additionalContentList = QarList<Additionalcontent>("additionalContentList");
        return additionalContentList.FirstOrDefault(x =>
            x.AdditionalType.Equals(additionalType, StringComparison.OrdinalIgnoreCase));
    }

    protected string CurrentLanguage => (ViewData["language"] ?? string.Empty) as string;

    protected string CurrentTheme => QarSingleton.GetInstance().GetSiteTheme();

    protected string SiteUrl => QarSingleton.GetInstance().GetSiteUrl();
    
    
    public List<Adlanguage> AdlanguageList =>
        (ViewData["adlanguageList"] ?? new List<Adlanguage>()) as List<Adlanguage>;

    // protected string SiteUrl => (ViewData["siteUrl"] ?? string.Empty) as string;
    protected string Query => (ViewData["query"] ?? string.Empty) as string;
    protected string ControllerName => (ViewData["controllerName"] ?? string.Empty) as string;
    protected string ActionName => (ViewData["actionName"] ?? string.Empty) as string;
    protected string SkinName => (ViewData["skinName"] ?? string.Empty) as string;
    protected string Title => (ViewData["title"] ?? string.Empty) as string;
    public List<Admin> UserList => (ViewData["userList"] ?? new  List<Admin>()) as  List<Admin>;
    public List<Advertise> AdvertiseList => QarList<Advertise>("advertiseList");

    protected List<Articlecategory> CategoryList => QarList<Articlecategory>("categoryList");
    protected List<Language> LanguageList => (ViewData["languageList"] ?? new List<Language>()) as List<Language>;

    protected List<Multilanguage> MultiLanguageList =>
        (ViewData["multiLanguageList"] ?? new List<Multilanguage>()) as List<Multilanguage>;

    protected Sitesetting SiteSetting =>
        ViewData["siteSetting"] != null ? (ViewData["siteSetting"] as Sitesetting) : null;

    protected bool CanView => Convert.ToBoolean(ViewData["canView"] ?? false);
    protected bool CanCreate => Convert.ToBoolean(ViewData["canCreate"] ?? false);
    protected bool CanEdit => Convert.ToBoolean(ViewData["canEdit"] ?? false);
    protected bool CanDelete => Convert.ToBoolean(ViewData["canDelete"] ?? false);

 
 
}