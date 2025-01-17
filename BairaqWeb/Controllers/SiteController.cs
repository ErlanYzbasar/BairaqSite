using System.Globalization;
using System.Reflection;
using COMMON;
using Dapper;
using DBHelper;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using MODEL;
using BairaqWeb.Caches;
using BairaqWeb.Hangfire;
using Serilog;

namespace BairaqWeb.Controllers;

[Authorize(Roles = "Admin")]
public class SiteController : QarBaseController
{
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _environment;

    public SiteController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Setting +Setting()

    public IActionResult Setting()
    {
        ViewData["title"] = T("ls_Sitesettings");
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Setting +Setting(string name, string value, int pk = 0)

    [HttpPost]
    public IActionResult Setting(string name, string value, int pk = 0)
    {
        name = (name ?? string.Empty).Trim().ToLower();
        value ??= string.Empty;
        using (var connection = Utilities.GetOpenConnection())
        {
            int? result = 0;
            string[] props =
            {
                "title", "description", "keywords", "copyright", "analyticsHtml", "analyticsScript", "aboutUs",
                "aboutProject", "address", "phone", "email", "pressSecretary", "mapEmbed", "facebook", "twitter",
                "instagram", "vk", "telegram", "youtube", "whatsapp", "tiktok", "mStartAndEndTime"
            };

            if (props.Any(x => x.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                var siteSetting = connection.GetList<Sitesetting>("where qStatus = 0 and id = @id", new { id = pk })
                    .FirstOrDefault();
                if (siteSetting == null)
                    return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", null);
                if (name.Equals("mStartAndEndTime", StringComparison.OrdinalIgnoreCase))
                {
                    var mourningDayList = value.Split("~");
                    if (mourningDayList.Length != 2 ||
                        !DateTime.TryParseExact(mourningDayList[0].Trim(), "yyyy-MM-dd HH:mm",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var mStartTime) ||
                        !DateTime.TryParseExact(mourningDayList[1].Trim(), "yyyy-MM-dd HH:mm",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var mEndTime))
                        return MessageHelper.RedirectAjax("Мақала автоматты жолданатын уақытын дұрыс жазыңыз!", "error",
                            "", null);
                    siteSetting.MStartTime = UnixTimeHelper.ConvertToUnixTime(mStartTime);
                    siteSetting.MEndTime = UnixTimeHelper.ConvertToUnixTime(mEndTime);
                }

                if (name.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Title = value;
                }

                if (name.Equals("description", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Description = value;
                }

                if (name.Equals("keywords", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Keywords = value;
                }

                if (name.Equals("copyright", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Copyright = value;
                }

                if (name.Equals("analyticsHtml", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.AnalyticsHtml = value;
                }

                if (name.Equals("analyticsScript", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.AnalyticsScript = value;
                }

                if (name.Equals("address", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Address = value;
                }

                if (name.Equals("phone", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Phone = value;
                }

                if (name.Equals("email", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Email = value;
                }

                if (name.Equals("sEmail", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.SEmail = value;
                }

                if (name.Equals("mapEmbed", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.MapEmbed = value;
                }

                if (name.Equals("facebook", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Facebook = value;
                }

                if (name.Equals("twitter", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Twitter = value;
                }

                if (name.Equals("instagram", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Instagram = value;
                }

                if (name.Equals("vk", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Vk = value;
                }

                if (name.Equals("telegram", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Telegram = value;
                }

                if (name.Equals("youtube", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Youtube = value;
                }

                if (name.Equals("whatsapp", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Whatsapp = value;
                }

                if (name.Equals("tiktok", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.Tiktok = value;
                }

                result = connection.Update(siteSetting);
            }

            if (result > 0)
            {
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetSiteSetting));
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                    $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list", null);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", null);
    }

    #endregion

    #region Сайт logo- сын өзгерту +UploadSiteLogo(IFormFile logoFile, string type)

    [HttpPost]
    public IActionResult UploadSiteLogo(IFormFile logoImage, string type)
    {
        if (logoImage == null) return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", "");

        if (!logoImage.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                logoImage.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
        {
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);
        }

        var absolutePathDirectory = _environment.WebRootPath + "/uploads/images/";
        if (!Directory.Exists(absolutePathDirectory)) Directory.CreateDirectory(absolutePathDirectory);
        var fileFormat = Path.GetExtension(logoImage.FileName).ToLower();
        if (fileFormat.Equals(".jpeg")) fileFormat = ".jpg";
        var absolutePath = absolutePathDirectory + "logo-" + type + fileFormat;
        using (var file = System.IO.File.OpenWrite(absolutePath))
        {
            logoImage.CopyTo(file);
            file.Flush();
        }

        var logoUrl = "/uploads/images/logo-" + type + fileFormat + "?t=" + RandomHelper.GetNumberRandom(5);

        using (var connection = Utilities.GetOpenConnection())
        {
            var siteSetting = connection.GetList<Sitesetting>("where qStatus = 0 order by id").FirstOrDefault();
            if (siteSetting != null)
            {
                if (type.Equals("lightLogo", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.LightLogo = logoUrl;
                }
                else if (type.Equals("darkLogo", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.DarkLogo = logoUrl;
                }
                else if (type.Equals("mobileLightLogo", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.MobileLightLogo = logoUrl;
                }
                else if (type.Equals("mobileDarkLogo", StringComparison.OrdinalIgnoreCase))
                {
                    siteSetting.MobileDarkLogo = logoUrl;
                }

                connection.Update(siteSetting);
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetSiteSetting));
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success", "", null);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", null);
    }

    #endregion

    #region Сайт icon- сын өзгерту +UploadSiteIcon(IFormFile iconFile)

    [HttpPost]
    public IActionResult UploadSiteIcon(IFormFile iconFile)
    {
        if (iconFile == null) return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", "");

        if (!iconFile.ContentType.Contains("image") || !ImageFileExtensions.ToList()
                .Exists(x => Path.GetExtension(iconFile.FileName.ToLower()).EndsWith(x)))
        {
            return MessageHelper.RedirectAjax(T("ls_Tiiions") + "(*.png)", "error", "", null);
        }

        var absolutePathDirectory = _environment.WebRootPath + "/uploads/images/";
        if (!Directory.Exists(absolutePathDirectory)) Directory.CreateDirectory(absolutePathDirectory);
        var fileFormat = Path.GetExtension(iconFile.FileName).ToLower();
        var absolutePath = absolutePathDirectory + "icon" + fileFormat;
        using (var file = System.IO.File.OpenWrite(absolutePath))
        {
            iconFile.CopyTo(file);
            file.Flush();
        }

        var iconUrl = "/uploads/images/icon" + fileFormat + "?t=" + RandomHelper.GetNumberRandom(5);

        using (var connection = Utilities.GetOpenConnection())
        {
            var siteSetting = connection.GetList<Sitesetting>("where qStatus = 0 order by id").FirstOrDefault();
            if (siteSetting != null)
            {
                siteSetting.Favicon = iconUrl;
                connection.Update(siteSetting);
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetSiteSetting));
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success", "", null);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", null);
    }

    #endregion

    #region Тілде +Language()

    public IActionResult Language()
    {
        ViewData["title"] = T("ls_Sitelanguage");
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Тілдерді өзгертіп сақтау +Language(List<Language> languageList)

    [HttpPost]
    public IActionResult Language(List<Language> languageList)
    {
        if (languageList == null || languageList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "jsonData");
        using var connection = Utilities.GetOpenConnection();
        using var tran = connection.BeginTransaction();
        try
        {
            foreach (var item in languageList)
            {
                if (item.Id <= 0)
                    return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong" + $"(id = {item.Id}) "),
                        "error", "", "");
                var itemLanguage = connection.GetList<Language>("where id = @id", new { id = item.Id })
                    .FirstOrDefault();
                if (itemLanguage == null)
                    return MessageHelper.RedirectAjax(T("ls_Isdeletedoridiswrong" + $"(id = {item.Id}) "),
                        "error", "", "");
                itemLanguage.FrontendDisplay = item.FrontendDisplay;
                itemLanguage.BackendDisplay = item.BackendDisplay;
                itemLanguage.DisplayOrder = item.DisplayOrder;
                itemLanguage.IsDefault = item.IsDefault;
                connection.Update(itemLanguage);
            }

            tran.Commit();
            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetLanguageList));
            return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list", null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ActionName);
            tran.Rollback();
            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", null);
        }
    }

    #endregion

    #region Тазалау +Flush()

    public IActionResult Flush()
    {
        ViewData["title"] = T("ls_Flushcache");
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Сайт тіл бумасын жаңалау +FlushCache()

    [HttpPost]
    public IActionResult FlushCache()
    {
        var type = typeof(QarCache);
        var methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var methodNames = methodInfos.Select(method => method.Name).Distinct();
        BackgroundJob.Enqueue<QarJob>(x => x.JobSaveSiteMap());

        foreach (var name in methodNames)
        {
            QarCache.ClearCache(_memoryCache, name);
        }

        return MessageHelper.RedirectAjax(T("ls_Flushsuccessfully"), "success", "", null);
    }

    #endregion
}