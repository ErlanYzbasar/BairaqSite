global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Security.Claims;
using BairaqWeb.Attributes;
using BairaqWeb.Caches;
using COMMON;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MODEL;
using MODEL.Enums;
using MODEL.ViewModels;
using Serilog;

namespace BairaqWeb.Controllers;

public class QarBaseController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;
    public readonly string[] ImageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg", ".webp", ".avif", ".svg" };
    public QarBaseController(IMemoryCache memoryCache, IWebHostEnvironment environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }
    public int ExpireDayCount { get; } = 7;

    public string CurrentLanguage => (ViewData["language"] ?? string.Empty) as string;

    public string ControllerName => (ViewData["controllerName"] ?? string.Empty) as string;

    public string ActionName => (ViewData["actionName"] ?? string.Empty) as string;
    public List<Admin> UserList => (ViewData["userList"] ?? new List<Admin>()) as List<Admin>;

    public string CurrentTheme => QarSingleton.GetInstance().GetSiteTheme();

    public string NoImage => $"/{CurrentLanguage}/QarBase/GenerateRatioImage?w=160&h=90";

    #region Language Value +T(string localKey)

    public string T(string localKey)
    {
        if (string.IsNullOrWhiteSpace(localKey)) return localKey;
        return QarCache.GetLanguageValue(_memoryCache, localKey, CurrentLanguage);
    }

    #endregion

    #region Сайтқа тіркеліп кіруші ақпаратын Cookie-ге сақтау + SaveLoginInfoToCookie(string email, string realName, int adminId, string roleIdList, string roleNames, bool isSuperAdmin, string avatarUrl, string skinName)

    public void SaveLoginInfoToCookie(string email, string realName, int adminId, List<int> roleIdList,
        string roleNames, bool isSuperAdmin, string avatarUrl, string skinName)
    {
        var identity = new ClaimsIdentity("AccountLogin");
        identity.AddClaim(new Claim(ClaimTypes.Email, email));
        identity.AddClaim(new Claim("RealName", realName));
        identity.AddClaim(new Claim("AdminId", adminId.ToString()));
        identity.AddClaim(new Claim("RoleIds", string.Join(",", roleIdList)));
        identity.AddClaim(new Claim("RoleNames", roleNames));
        identity.AddClaim(new Claim("IsSuperAdmin", isSuperAdmin ? "1" : "0"));
        identity.AddClaim(new Claim("AvatarUrl", avatarUrl));
        identity.AddClaim(new Claim("SkinName", skinName));
        identity.AddClaim(new Claim("LoginTime", UnixTimeHelper.ConvertToUnixTime(DateTime.Now).ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(ExpireDayCount)
        };
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    #endregion

    #region Get Admin Id +GetAdminId()

    public int GetAdminId()
    {
        return Convert.ToInt32(ViewData["adminId"] ?? 0);
    }

    #endregion

    #region IS Super Admin +IsSuperAdmin()

    public bool IsSuperAdmin()
    {
        return HttpContext.User.Identity.IsSuperAdmin();
    }

    #endregion

    #region Көп түрлі тіл мәндерін алу +GetMultilanguageList(IDbConnection _connection, string tableName, List<int> columnIdList, List<string> columnNameList = null, string language = "")

    public static List<Multilanguage> GetMultilanguageList(IDbConnection connection, string tableName,
        List<int> columnIdList, List<string> columnNameList = null, string language = "")
    {
        if (columnIdList == null || columnIdList.Count == 0) return new List<Multilanguage>();
        tableName = tableName.Trim().ToLower();
        var columnIdArrIn = "(" + string.Join(",", columnIdList.ToArray()) + ")";
        var querySql = $"where qStatus = 0 and columnId in {columnIdArrIn} and tableName = @tableName ";
        object queryObj = new { tableName, language };
        if (!string.IsNullOrEmpty(language)) querySql += " and language = @language ";

        if (columnNameList != null && columnNameList.Count > 0)
        {
            var columnNameArrIn = "(" + string.Join(",", columnNameList.Select(x => "'" + x + "'").ToArray()) + ")";
            querySql += $" and columnName in {columnNameArrIn} ";
        }

        return connection.GetList<Multilanguage>(querySql, queryObj).ToList();
    }

    #endregion

    #region Көп түрлі тіл мәндерін сақтау +SaveMultilanguageList(IDbConnection _connection, List<Multilanguage> multiLanguageList, string tableName, int columnId)

    public bool SaveMultilanguageList(IDbConnection connection, List<Multilanguage> multiLanguageList,
        string tableName, int columnId)
    {
        tableName = tableName.Trim().ToLower();
        foreach (var item in multiLanguageList)
        {
            item.Language = (item.Language ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(item.ColumnName) ||
                string.IsNullOrEmpty(item.ColumnName = item.ColumnName.Trim().ToLower()) ||
                string.IsNullOrEmpty(item.Language)) continue;
            var multiLanguage = connection.GetList<Multilanguage>(
                "where qStatus = 0 and language = @language and columnId = @columnId and tableName = @tableName and columnName = @columnName",
                new { language = item.Language, columnId, tableName, columnName = item.ColumnName }).FirstOrDefault();
            item.ColumnValue = (item.ColumnValue ?? string.Empty).Trim();
            if (multiLanguage != null)
            {
                if (string.IsNullOrEmpty(item.ColumnValue))
                    multiLanguage.QStatus = 1;
                else
                    multiLanguage.ColumnValue = item.ColumnValue;

                connection.Update(multiLanguage);
            }
            else
            {
                if (!string.IsNullOrEmpty(item.ColumnValue))
                    connection.Insert(new Multilanguage
                    {
                        TableName = tableName,
                        Language = item.Language,
                        ColumnId = columnId,
                        ColumnName = item.ColumnName,
                        ColumnValue = item.ColumnValue,
                        QStatus = 0
                    });
            }
        }

        return false;
    }

    #endregion

    #region Get Additional Content +AdditionalContent(IDbConnection _connection, string additionalType)

    public Additionalcontent AdditionalContent(IDbConnection connection, string additionalType)
    {
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var additionalContent = connection
            .GetList<Additionalcontent>("where qStatus = 0 and additionalType = @additionalType ",
                new { additionalType }).FirstOrDefault();
        if (additionalContent == null)
        {
            additionalContent = new Additionalcontent
            {
                AdditionalType = additionalType,
                Title = "",
                ShortDescription = "",
                FullDescription = "",
                BackgroundImageUrl = "",
                BackgroundColor = "",
                Color = "",
                IconUrl = "",
                VideoEmbed = "",
                AddtionalInfo1 = "",
                AddtionalInfo2 = "",
                AddtionalInfo3 = "",
                DisplayOrder = 0,
                AddTime = currentTime,
                UpdateTime = currentTime,
                QStatus = 0
            };
            additionalContent.Id = connection.Insert(additionalContent) ?? 0;
        }

        ViewData["multiLanguageList"] = GetMultilanguageList(connection, nameof(Additionalcontent),
            new List<int> { additionalContent.Id });
        return additionalContent;
    }

    #endregion

    #region Update Additional Content +AdditionalContent(Additionalcontent item,IFormFile backgroundImage, string showFields, string multiLanguageJson)

    [NoRole]
    [HttpPost]
    public IActionResult AdditionalContent(Additionalcontent item, IFormFile backgroundImage, string showFields,
        string multiLanguageJson)
    {
        var showFieldList = (showFields ?? string.Empty).Split(",").ToList();

        foreach (var field in showFieldList)
            switch (field)
            {
                case nameof(Additionalcontent.BackgroundImageUrl):
                    {
                        if (string.IsNullOrEmpty(item.BackgroundImageUrl) && backgroundImage == null)
                            return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", null);
                    }
                    break;
                case nameof(Additionalcontent.Title):
                    {
                        if (string.IsNullOrWhiteSpace(item.Title))
                            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "title");
                    }
                    break;
                case nameof(Additionalcontent.ShortDescription):
                    {
                        if (string.IsNullOrWhiteSpace(item.ShortDescription))
                            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "shortDescription");
                    }
                    break;
                case nameof(Additionalcontent.AddtionalInfo1):
                    {
                        if (string.IsNullOrWhiteSpace(item.AddtionalInfo1))
                            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "addtionalInfo1");
                    }
                    break;
                case nameof(Additionalcontent.AddtionalInfo2):
                    {
                        if (string.IsNullOrWhiteSpace(item.AddtionalInfo2))
                            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "addtionalInfo2");
                    }
                    break;
                case nameof(Additionalcontent.AddtionalInfo3):
                    {
                        if (string.IsNullOrWhiteSpace(item.AddtionalInfo3))
                            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "addtionalInfo3");
                    }
                    break;
            }

        List<Multilanguage> multiLanguageList = null;
        try
        {
            multiLanguageList = JsonHelper.DeserializeObject<List<Multilanguage>>(multiLanguageJson);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AdditionalContent");
            return MessageHelper.RedirectAjax(T("ls_Edjd"), "error", "", "multiLanguageJson");
        }

        if (backgroundImage != null)
        {
            if (!backgroundImage.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                    backgroundImage.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
                return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);

            var tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var fileFormat = Path.GetExtension(backgroundImage.FileName).ToLower();
            item.BackgroundImageUrl = "/uploads/images/" + tempKey + fileFormat;
            var absolutePath = _environment.WebRootPath + item.BackgroundImageUrl;
            if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            using var stream = System.IO.File.OpenWrite(absolutePath);
            backgroundImage.CopyTo(stream);
        }
        else
        {
            item.BackgroundImageUrl = string.Empty;
        }

        using (var connection = Utilities.GetOpenConnection())
        {
            var additionalContent = connection
                .GetList<Additionalcontent>("where qStatus = 0 and id  = @id", new { id = item.Id }).FirstOrDefault();
            if (additionalContent == null)
                return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", null);

            if (!string.IsNullOrWhiteSpace(item.Title)) additionalContent.Title = item.Title;

            if (!string.IsNullOrWhiteSpace(item.ShortDescription))
                additionalContent.ShortDescription = item.ShortDescription;

            if (!string.IsNullOrWhiteSpace(item.FullDescription))
                additionalContent.FullDescription = HtmlAgilityPackHelper.GetHtmlBoyInnerHtml(item.FullDescription);

            if (!string.IsNullOrEmpty(item.BackgroundImageUrl))
            {
                var oldImagePath = _environment.WebRootPath + additionalContent.BackgroundImageUrl;
                if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);

                additionalContent.BackgroundImageUrl = item.BackgroundImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(item.BackgroundColor))
                additionalContent.BackgroundColor = item.BackgroundColor;

            if (!string.IsNullOrWhiteSpace(item.Color)) additionalContent.Color = item.Color;

            if (!string.IsNullOrWhiteSpace(item.IconUrl)) additionalContent.IconUrl = item.IconUrl;

            if (!string.IsNullOrWhiteSpace(item.VideoEmbed)) additionalContent.VideoEmbed = item.VideoEmbed;

            if (!string.IsNullOrWhiteSpace(item.AddtionalInfo1)) additionalContent.AddtionalInfo1 = item.AddtionalInfo1;

            if (!string.IsNullOrWhiteSpace(item.AddtionalInfo2)) additionalContent.AddtionalInfo2 = item.AddtionalInfo2;

            if (!string.IsNullOrWhiteSpace(item.AddtionalInfo3)) additionalContent.AddtionalInfo3 = item.AddtionalInfo3;

            additionalContent.DisplayOrder = item.DisplayOrder;
            additionalContent.UpdateTime = UnixTimeHelper.GetCurrentUnixTime();
            if (connection.Update(additionalContent) > 0)
            {
                SaveMultilanguageList(connection, multiLanguageList, nameof(Additionalcontent), additionalContent.Id);
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetAdditionalContentList));
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success", "", null);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", null);
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

    #region Параметрдің string мәнін алу +GetStringQueryParam(string paramName, string defaultValue = "")

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

    #region Қолданушының IP әдіресін алу +GetIPAddress()

    public string GetIPAddress()
    {
        string locationIP = HttpContext.Connection.RemoteIpAddress.ToString();
        if (HttpContext.Request.Headers["X-Real-IP"].Count() > 0)
        {
            locationIP = HttpContext.Request.Headers["X-Real-IP"];
        }

        return locationIP;
    }

    #endregion


    #region Пәрәметір Int Тізбек мәнін алу +GetIntListQueryParam(string paramName)

    public List<int> GetIntListQueryParam(string paramName)
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
        var paramIdList = new List<int>();
        foreach (var idStr in paramIds.Split(','))
            if (int.TryParse(idStr, out var id) && id > 0)
                paramIdList.Add(id);

        return paramIdList;
    }

    #endregion

    #region Update Media Info +UpdateMediaInfo(IDbConnection _connection, string insertMediaPath, string removeMediaPath)

    public void UpdateMediaInfo(IDbConnection connection, string insertMediaPath, string removeMediaPath)
    {
        if (!string.IsNullOrEmpty(removeMediaPath))
            connection.Execute(
                "update mediainfo set useCount = useCount -1 where qStatus = 0 and path = @removeMediaPath",
                new { removeMediaPath });

        if (!string.IsNullOrEmpty(insertMediaPath))
            connection.Execute(
                "update mediainfo set useCount = useCount +1 where qStatus = 0 and path = @insertMediaPath",
                new { insertMediaPath });
    }

    #endregion

    #region Generate Ratio Image GenerateRatioImage(float w, float h)

    public IActionResult GenerateRatioImage(float w, float h)
    {
        var directory = _environment.WebRootPath + "/images";
        var abosolutePath = directory + $"/noimage{w}-{h}.png";
        if (System.IO.File.Exists(abosolutePath)) return Redirect($"/images/noimage{w}-{h}.png");
        var picturePath = directory + "/no-picture.png";
        return File(ImgHandler.GenerateRatioImage(w, h, picturePath, abosolutePath), "image/png");
    }

    #endregion

    #region Get Distinct Latyn Url +GetDistinctLatynUrl(IDbConnection _connection, string tableName, string latynUrl, string title, int itemId, string language)

    public static string GetDistinctLatynUrl(IDbConnection connection, string tableName, string latynUrl, string title,
        int itemId, string language)
    {
        tableName = (tableName ?? string.Empty).ToLower();

        if (tableName.Equals(nameof(Article), StringComparison.OrdinalIgnoreCase))
        {
            //if contains article id, remove!
            latynUrl = StringHelper.GetRealLatynUrl(latynUrl);
        }

        var orginalLatynUrl = latynUrl =
            (string.IsNullOrEmpty(latynUrl) ? StringHelper.Kaz2LatForUrl(title) : latynUrl).ToLower();
        var hasCount = 0;
        var index = 1;
        object queryObj = null;

        do
        {
            queryObj = new { tableName, itemId, latynUrl, language };
            var querySql = $" select count(1) from {tableName} where latynUrl = @latynUrl ";
            if (tableName.Equals(nameof(Articlecategory), StringComparison.OrdinalIgnoreCase))
                querySql += " and language = @language ";

            if (itemId > 0) querySql += " and id <> @itemId ";

            hasCount = connection.Query<int>(querySql, queryObj).FirstOrDefault();
            if (hasCount == 0)
            {
                querySql = " where qStatus = 0 and tableName = @tableName and latynUrl = @latynUrl ";
                if (tableName.Equals(nameof(Articlecategory), StringComparison.OrdinalIgnoreCase))
                    querySql += " and language = @language ";

                var lItem = connection.GetList<Latynurl>(querySql, queryObj).FirstOrDefault();
                if (lItem != null)
                {
                    if (lItem.ItemId == itemId)
                    {
                        lItem.QStatus = 1;
                        connection.Update(lItem);
                    }
                    else
                    {
                        hasCount = 1;
                    }
                }
            }

            if (hasCount > 0) latynUrl = $"{orginalLatynUrl}-{index++}";
        } while (hasCount > 0);

        if (itemId > 0)
        {
            var oldLatynUrl = connection.Query<string>($"select latynUrl from {tableName} where id = @itemId", queryObj)
                .FirstOrDefault();
            if (!oldLatynUrl.Equals(latynUrl))
            {
                var lItem = connection
                    .GetList<Latynurl>(
                        " where qStatus = 1 and tableName = @tableName and latynUrl = @oldLatynUrl and itemId = @itemId and language = @language ",
                        new { itemId, oldLatynUrl, tableName, language }).FirstOrDefault();
                if (lItem != null)
                {
                    lItem.QStatus = 0;
                    connection.Update(lItem);
                }
                else
                {
                    connection.Insert(new Latynurl
                    {
                        ItemId = itemId,
                        LatynUrl = oldLatynUrl,
                        TableName = tableName,
                        Language = language,
                        QStatus = 0
                    });
                }
            }
        }

        return $"{itemId}-{latynUrl}";
    }

    #endregion
    #region Кілт сөз қатарын алу +GetTagList(string keyWord)

    [NoRole]
    [HttpGet]
    public IActionResult GetTagList(string keyWord)
    {
        keyWord ??= string.Empty;
        using var connection = Utilities.GetOpenConnection();
        var tagList = connection
            .Query<string>(
                "SELECT title FROM tag WHERE taggedCount > 0 AND MATCH(title) AGAINST(@keyWord IN NATURAL LANGUAGE MODE) limit 10",
                new { keyWord }).Select(x => new TagModel
            {
                Value = x // Regex.Unescape(x)
            }).ToList();

        return Json(new { status = "success", data = tagList });
    }

    #endregion

    #region Кілт сөздерді қосу +InsertOrEditTagList(IDbConnection _connection, List<string> tagList, string tableName, int itemId)

    public void InsertOrEditTagList(IDbConnection connection, List<string> tagList, string tableName, int itemId)
    {
        if (tagList == null || tagList.Count == 0) return;

        var oldTagList = connection.GetList<Tag>(
            "where id in (select tagId from tagmap where qStatus = 0 and tableName = @tableName and itemId = @itemId)",
            new { tableName, itemId }).ToList();
        if (oldTagList.Count > 0)
        {
            var isRemoveTagIds = oldTagList.Where(x => !tagList.Contains(x.Title)).Select(x => x.Id).ToArray();
            if (isRemoveTagIds.Length > 0)
                connection.Execute(
                    $"delete from tagmap where tableName = '{tableName}' and itemId = {itemId} and tagId in ({string.Join(",", isRemoveTagIds)}) ;  update tag set taggedCount = taggedCount - 1  where id in ({string.Join(",", isRemoveTagIds)})");

            tagList = tagList.Where(x => !oldTagList.Exists(t => t.Title.Equals(x))).ToList();
        }

        var sql = string.Empty;
        foreach (var item in tagList)
        {
            var tagTitle = item.Trim();
            tagTitle = StringHelper.SymbolReplace(tagTitle);
            if (string.IsNullOrEmpty(tagTitle)) continue;

            var tagId = connection.Query<int?>("select id from tag where title = @tagTitle", new { tagTitle })
                .FirstOrDefault();
            if (tagId == null || tagId <= 0)
                tagId = connection.Insert(new Tag
                {
                    TaggedCount = 0,
                    LatynUrl = GetDistinctLatynUrl(connection, nameof(Tag), "", tagTitle, 0, CurrentLanguage),
                    OldLatynUrl = string.Empty,
                    Title = tagTitle
                });

            sql +=
                $" insert into tagmap (tagId, tableName, itemId) values ({tagId},'{tableName}',{itemId}); update tag set taggedCount = (select count(1) from tagmap where qStatus = 0 and tagId = {tagId} ) where id = {tagId}; ";
        }

        if (!string.IsNullOrEmpty(sql)) connection.Execute(sql);
    }

    #endregion

    #region Save Role Permission List +SaveRolePermissionList(IDbConnection _connection, int roleId , List<Rolepermission> rolePermissionList)

    public void SaveRolePermissionList(IDbConnection connection, int roleId, List<Rolepermission> rolePermissionList)
    {
        if (roleId <= 0) return;

        var currentRolePermissionList =
            connection.GetList<Rolepermission>("where roleId = @roleId", new { roleId }).ToList();
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        //Update Old
        foreach (var current in currentRolePermissionList)
            if (rolePermissionList.Exists(x =>
                    x.TableName.Equals(current.TableName) && x.PermissionId == current.PermissionId &&
                    x.ColumnId == current.ColumnId))
            {
                if (current.QStatus != 0)
                {
                    current.QStatus = 0;
                    current.UpdateTime = currentTime;
                    connection.Update(current);
                }
            }
            else
            {
                if (current.QStatus != 1)
                {
                    current.QStatus = 1;
                    current.UpdateTime = currentTime;
                    connection.Update(current);
                }
            }

        //Add new
        foreach (var rolePermission in rolePermissionList)
            if (!currentRolePermissionList.Exists(x =>
                    x.TableName.Equals(rolePermission.TableName) && x.PermissionId == rolePermission.PermissionId &&
                    x.ColumnId == rolePermission.ColumnId))
            {
                rolePermission.RoleId = roleId;
                rolePermission.AddTime = currentTime;
                rolePermission.UpdateTime = currentTime;
                rolePermission.QStatus = 0;
                connection.Insert(rolePermission);
            }
    }

    #endregion


    protected IActionResult Return404()
    {
        return Redirect("/404.html");
    }

    protected static string GetArticleCacheName(string latynUrl, string language)
    {
        return $"{nameof(HomeController)}_GetArticle_{latynUrl}_{language}";
    }

    protected bool UpdateDiff(IDbConnection connection, Article oldArticle, Article newArticle, List<string> tagList,
        int currentTime)
    {
        var adminId = GetAdminId();
        var itemId = oldArticle?.Id ?? 0;
        if (oldArticle == null || newArticle == null || adminId <= 0 || itemId <= 0) return false;

        const string tableName = nameof(Article);

        var titleDiffList = DiffHelper.GetDiffList(oldArticle.Title, newArticle.Title);

        var fullDescriptionDiffList = DiffHelper.GetDiffList(
            HtmlHelper.GetHtmlInnerText(oldArticle.FullDescription),
            HtmlHelper.GetHtmlInnerText(newArticle.FullDescription));

        var oldTagList = connection.GetList<Tag>(
            "where id in (select tagId from tagmap where qStatus = 0 and tableName = @tableName and itemId = @itemId)",
            new { tableName, itemId }).ToList();

        var deletingTagList = oldTagList.FindAll(x => !tagList.Contains(x.Title)).ConvertAll(x => x.Title);
        var insertingTagList = tagList.FindAll(x => !oldTagList.Exists(t => t.Title.Equals(x)));

        var totalCount = titleDiffList.Count
                         + fullDescriptionDiffList.Count
                         + deletingTagList.Count
                         + insertingTagList.Count;
        var categoryChanged = oldArticle.CategoryId != newArticle.CategoryId;
        var thumbnailCopyrightChanged = !oldArticle.ThumbnailCopyright.Equals(newArticle.ThumbnailCopyright);
        var isPinnedChanged = oldArticle.IsPinned != newArticle.IsPinned;
        var isFeaturedChanged = oldArticle.IsFeatured != newArticle.IsFeatured;
        var isFocusNewsChanged = oldArticle.IsFocusNews != newArticle.IsFocusNews;
        var isAutoPublishTimeChanged = oldArticle.AutoPublishTime != newArticle.AutoPublishTime;
        var isDeleted = oldArticle.QStatus != newArticle.QStatus && newArticle.QStatus == 1;

        var boolList = new List<bool>
        {
            totalCount > 0,
            categoryChanged,
            thumbnailCopyrightChanged,
            isPinnedChanged,
            isFeaturedChanged,
            isFocusNewsChanged,
            isAutoPublishTimeChanged,
            isDeleted
        };

        if (!boolList.Contains(true)) return true;

        #region Insert to table

        var updateLogId = connection.Insert(new UpdateLog
        {
            AdminId = adminId,
            AddTime = currentTime,
            TableName = tableName,
            ItemId = itemId,
            QStatus = 0
        }) ?? 0;

        if (updateLogId <= 0) return false;

        foreach (var diff in titleDiffList)
        {
            connection.Insert(new UpdateContent
            {
                Content = diff.Text,
                LogId = updateLogId,
                ColumnName = nameof(Article.Title),
                Operation = diff.Operation
            });
        }

        foreach (var diff in fullDescriptionDiffList)
        {
            connection.Insert(new UpdateContent
            {
                Content = diff.Text,
                LogId = updateLogId,
                ColumnName = nameof(Article.FullDescription),
                Operation = diff.Operation
            });
        }

        foreach (var tag in deletingTagList)
        {
            connection.Insert(new UpdateContent
            {
                Content = tag,
                LogId = updateLogId,
                ColumnName = nameof(Tag),
                Operation = Operation.DELETE
            });
        }

        foreach (var tag in insertingTagList)
        {
            connection.Insert(new UpdateContent
            {
                Content = tag,
                LogId = updateLogId,
                ColumnName = nameof(Tag),
                Operation = Operation.INSERT
            });
        }

        if (categoryChanged)
        {
            var categoryList = QarCache.GetCategoryList(_memoryCache);

            connection.Insert(new UpdateContent
            {
                Content = categoryList.Find(x => x.Id == oldArticle.CategoryId)?.Title ?? string.Empty,
                LogId = updateLogId,
                ColumnName = nameof(Article.CategoryId),
                Operation = Operation.DELETE
            });

            connection.Insert(new UpdateContent
            {
                Content = categoryList.Find(x => x.Id == newArticle.CategoryId)?.Title ?? string.Empty,
                LogId = updateLogId,
                ColumnName = nameof(Article.CategoryId),
                Operation = Operation.INSERT
            });
        }

        // if (authorChanged)
        // {
        //     connection.Insert(new UpdateContent
        //     {
        //         Content = UserList.FirstOrDefault(x=>x.Id==oldArticle.AddTime)?.Name,
        //         LogId = updateLogId,
        //         ColumnName = nameof(Admin.Name),
        //         Operation = Operation.DELETE
        //     });
        //
        //     connection.Insert(new UpdateContent
        //     {
        //         Content =  UserList.FirstOrDefault(x=>x.Id==newArticle.AddTime)?.Name,
        //         LogId = updateLogId,
        //         ColumnName = nameof(Admin.Name),
        //         Operation = Operation.INSERT
        //     });
        // }

        if (thumbnailCopyrightChanged)
        {
            connection.Insert(new UpdateContent
            {
                Content = oldArticle.ThumbnailCopyright,
                LogId = updateLogId,
                ColumnName = nameof(Article.ThumbnailCopyright),
                Operation = Operation.DELETE
            });

            connection.Insert(new UpdateContent
            {
                Content = newArticle.ThumbnailCopyright,
                LogId = updateLogId,
                ColumnName = nameof(Article.ThumbnailCopyright),
                Operation = Operation.INSERT
            });
        }

        if (isAutoPublishTimeChanged)
        {
            if (oldArticle.AutoPublishTime > 0)
            {
                connection.Insert(new UpdateContent
                {
                    Content = oldArticle.AutoPublishTime.ToString(),
                    LogId = updateLogId,
                    ColumnName = nameof(Article.AutoPublishTime),
                    Operation = Operation.DELETE
                });
            }

            if (newArticle.AutoPublishTime > 0)
            {
                connection.Insert(new UpdateContent
                {
                    Content = newArticle.AutoPublishTime.ToString(),
                    LogId = updateLogId,
                    ColumnName = nameof(Article.AutoPublishTime),
                    Operation = Operation.INSERT
                });
            }
        }

        if (isPinnedChanged)
        {
            connection.Insert(new UpdateContent
            {
                Content = string.Empty,
                LogId = updateLogId,
                ColumnName = nameof(Article.IsPinned),
                Operation = newArticle.IsPinned == 1 ? Operation.INSERT : Operation.DELETE
            });
        }

        if (isFeaturedChanged)
        {
            connection.Insert(new UpdateContent
            {
                Content = string.Empty,
                LogId = updateLogId,
                ColumnName = nameof(Article.IsFeatured),
                Operation = newArticle.IsFeatured == 1 ? Operation.INSERT : Operation.DELETE
            });
        }

        if (isFocusNewsChanged)
        {
            connection.Insert(new UpdateContent
            {
                Content = string.Empty,
                LogId = updateLogId,
                ColumnName = nameof(Article.IsFocusNews),
                Operation = newArticle.IsFocusNews == 1 ? Operation.INSERT : Operation.DELETE
            });
        }

        if (isDeleted)
        {
            connection.Insert(new UpdateContent
            {
                Content = newArticle.QStatus.ToString(),
                LogId = updateLogId,
                ColumnName = nameof(Article.QStatus),
                Operation = Operation.DELETE
            });
        }

        #endregion

        return true;
    }
}