using System.Globalization;
using BairaqWeb.Caches;
using COMMON;
using COMMON.Extensions;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Authorization;
using MODEL;
using MODEL.FormatModels;
using MODEL.ViewModels;
using Serilog;

namespace BairaqWeb.Controllers;

[Authorize(Roles = "Admin")]
public class CatalogController : QarBaseController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public CatalogController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Category +Category(string query)

    public IActionResult Category(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Articlecategory");
        switch (query)
        {
            case "create":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "edit":
            {
                var articleCategoryId = GetIntQueryParam("id", 0);
                if (articleCategoryId <= 0)
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                using (var connection = Utilities.GetOpenConnection())
                {
                    var category = connection
                        .GetList<Articlecategory>("where qStatus = 0 and id = @articleCategoryId ",
                            new { articleCategoryId }).FirstOrDefault();
                    if (category == null)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    ViewData["category"] = category;
                    ViewData["multiLanguageList"] = GetMultilanguageList(connection, nameof(Articlecategory),
                        new List<int> { category.Id });
                }

                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "list":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            default:
            {
                return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
            }
        }
    }

    #endregion

    #region Category +Category(Articlecategory item)

    [HttpPost]
    public IActionResult Category(Articlecategory item)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");

        if (string.IsNullOrEmpty(item.Language))
            return MessageHelper.RedirectAjax(T("ls_Selectlanguage"), "error", "", "language");

        //if (!QarCache.GetLanguageList(_memoryCache).Exists(x=>x.LanguageCulture.Equals(item.Language)))
        //    return MessageHelper.RedirectAjax(T("ls_Selectlanguage"), "error", "", "language");
        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "title");
        item.ShortDescription ??= string.Empty;
        item.LatynUrl = (item.LatynUrl ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(item.LatynUrl) && !RegexHelper.IsLatinString(item.LatynUrl))
            return MessageHelper.RedirectAjax("Use latyn, number, - ", "error", "", "latynUrl");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();

        using (var connection = Utilities.GetOpenConnection())
        {
            item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Articlecategory), item.LatynUrl, item.Title,
                item.Id, item.Language);
            int? res = 0;
            if (item.Id == 0)
            {
                if (item.DisplayOrder == 0)
                {
                    item.DisplayOrder =
                        connection.Query<int?>("select max(displayOrder) from articlecategory where qStatus = 0")
                            .FirstOrDefault() ?? 0;
                    item.DisplayOrder += 1;
                }

                res = connection.Insert(new Articlecategory
                {
                    Title = item.Title,
                    OldLatynUrl = item.LatynUrl,
                    LatynUrl = item.LatynUrl,
                    Language = item.Language,
                    BlockType = string.Empty,
                    ParentId = item.ParentId,
                    ShortDescription = item.ShortDescription,
                    DisplayOrder = item.DisplayOrder,
                    IsHidden = item.IsHidden,
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 0
                });
                if (res > 0)
                {
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                    return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                }
            }
            else
            {
                var category = connection
                    .GetList<Articlecategory>("where qStatus = 0 and id = @id", new { id = item.Id }).FirstOrDefault();
                if (category == null)
                    return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
                category.Title = item.Title;
                category.ParentId = item.ParentId;
                category.Language = item.Language;
                category.LatynUrl = item.LatynUrl;
                category.ShortDescription = item.ShortDescription;
                category.IsHidden = item.IsHidden;
                category.DisplayOrder = item.DisplayOrder;
                category.UpdateTime = currentTime;
                res = connection.Update(category);
                if (res > 0)
                {
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                    return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={category.Id}",
                        "");
                }
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get article category list +GetCategoryList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetCategoryList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using var connection = Utilities.GetOpenConnection();
        var querySql = " from articlecategory where qStatus = 0 ";
        object queryObj = new { keyword = "%" + keyword + "%" };
        var orderSql = "";
        if (!string.IsNullOrEmpty(keyword)) querySql += " and (title like @keyword)";

        if (model.OrderList != null && model.OrderList.Count > 0)
            foreach (var item in model.OrderList)
                switch (item.Column)
                {
                    case 3:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " displayOrder " + item.Dir;
                    }
                        break;
                    case 4:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                    }
                        break;
                }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var articleCategoryList = connection
            .Query<Articlecategory>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                queryObj).ToList();
        var languageList = QarCache.GetLanguageList(_memoryCache);
        var dataList = articleCategoryList.Select(x => new
        {
            x.Id,
            x.Title,
            Language = languageList
                .FirstOrDefault(l => l.LanguageCulture.Equals(x.Language, StringComparison.OrdinalIgnoreCase))
                ?.FullName,
            x.DisplayOrder,
            x.LatynUrl,
            AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
        }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion

    #region Set article category status +SetCategoryStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetCategoryStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
            {
                using var connection = Utilities.GetOpenConnection();
                using var tran = connection.BeginTransaction();
                try
                {
                    var articleCategoryList = connection
                        .GetList<Articlecategory>($"where qStatus = 0 and id in ({string.Join(",", idList)})")
                        .ToList();
                    foreach (var articleCategory in articleCategoryList)
                    {
                        articleCategory.QStatus = 1;
                        articleCategory.UpdateTime = currentTime;
                        connection.Update(articleCategory);
                    }

                    tran.Commit();
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCategoryList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetArticleList));
                    return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ActionName);
                    tran.Rollback();
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            default:
            {
                return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
            }
        }
    }

    #endregion

    #region Article +Article(string query)

    public IActionResult Article(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Articles");
        switch (query)
        {
            case "create":
            {
                ViewData["categoryList"] = QarCache.GetCategoryList(_memoryCache);
                var roleIdList = HttpContext.User.Identity.RoleIds();
                ViewData["surveyList"] = QarCache.GetPublishQuestionList(_memoryCache, CurrentLanguage);
                ViewData["canSchedule"] =
                    IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
                ViewData["canFocus"] =
                    IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");
                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "edit":
            {
                var articleId = GetIntQueryParam("id", 0);
                if (articleId <= 0)
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                using (var connection = Utilities.GetOpenConnection())
                {
                    var article = connection
                        .GetList<Article>("where qStatus <> 1 and id = @articleId ", new { articleId })
                        .FirstOrDefault();
                    if (article == null)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    ViewData["article"] = article;
                    ViewData["category"] = connection
                        .GetList<Articlecategory>("where qStatus = 0 and id = @id", new { id = article.CategoryId })
                        .FirstOrDefault();
                    ViewData["surveyList"] = QarCache.GetPublishQuestionList(_memoryCache, CurrentLanguage);
                    ViewData["multiLanguageList"] =
                        GetMultilanguageList(connection, nameof(Article), new List<int> { article.Id });
                    ViewData["tagList"] = connection.Query<string>(
                        "select title from tag where id in (select tagId from tagmap where qStatus = 0 and tableName = @tableName and itemId = @itemId)",
                        new { tableName = nameof(Article), itemId = article.Id }).ToList();
                }

                ViewData["categoryList"] = QarCache.GetCategoryList(_memoryCache);
                var roleIdList = HttpContext.User.Identity.RoleIds();
                ViewData["canSchedule"] =
                    IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
                ViewData["canFocus"] =
                    IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");
                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "list":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            default:
            {
                return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
            }
        }
    }

    #endregion

    #region Article +Article(Article item, string tags, byte isAutoPublish, string autoPublishTimeStr, byte publishNow)

    [HttpPost]
    public IActionResult Article(Article item, string tags, byte isAutoPublish, string autoPublishTimeStr,
        byte publishNow)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");

        if (item.CategoryId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Pstc"), "error", "", "");

        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "title");

        if (string.IsNullOrWhiteSpace(item.ThumbnailCopyright))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "thumbnailCopyright");

        if (string.IsNullOrEmpty(tags) || string.IsNullOrEmpty(tags = tags.Trim()))
            return MessageHelper.RedirectAjax(T("ls_Walat"), "error", "", null);

        var tagList = new List<string>();
        try
        {
            var tagModelList = JsonHelper.DeserializeObject<List<TagModel>>(tags);
            tagList = tagModelList.Select(x => x.Value).Distinct().ToList();
        }
        catch (Exception ex)
        {
            return MessageHelper.RedirectAjax(ex.Message, "error", "", null);
        }

        if (tagList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Walat"), "error", "", null);

        if (isAutoPublish > 0)
        {
            if (!DateTime.TryParseExact(autoPublishTimeStr, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dateTimeAutoPublish))
                return MessageHelper.RedirectAjax("Мақала автоматты жолданатын уақытын дұрыс жазыңыз!", "error", "",
                    null);
            item.AutoPublishTime = UnixTimeHelper.ConvertToUnixTime(dateTimeAutoPublish);
        }
        else
        {
            item.AutoPublishTime = 0;
        }

        var roleIdList = HttpContext.User.Identity.RoleIds();
        var canSchedule = IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "schedule");
        var canFocus = IsSuperAdmin() || QarCache.CheckArticlePermission(_memoryCache, roleIdList, "focus");

        item.ThumbnailUrl ??= string.Empty;
        item.ThumbnailCopyright ??= string.Empty;
        item.ShortDescription = string.IsNullOrWhiteSpace(item.ShortDescription)
            ? HtmlAgilityPackHelper.GetShortDescription(item.FullDescription)
            : item.ShortDescription;
        item.FullDescription = HtmlAgilityPackHelper.GetHtmlBoyInnerHtml(item.FullDescription);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        int? res = 0;
        var qStatus = (byte)(publishNow == 1 ? isAutoPublish > 0 ? 2 : 0 : 3);

        using (var connection = Utilities.GetOpenConnection())
        using (var tran = connection.BeginTransaction())
        {
            try
            {
              
                if (item.Id == 0)
                {
                    res = connection.Insert(new Article
                    {
                        CategoryId = item.CategoryId,
                        Title = item.Title,
                        LatynUrl = "",
                        RecArticleIds = string.Empty,
                        ThumbnailUrl = item.ThumbnailUrl,
                        SurveyId = item.SurveyId,
                        ShortDescription = item.ShortDescription,
                        FullDescription = item.FullDescription,
                        AddAdminId = GetAdminId(),
                        UpdateAdminId = GetAdminId(),
                        AutoPublishTime = item.AutoPublishTime,
                        FocusAdditionalFile = string.Empty,
                        SearchName = string.Empty,
                        // HasAudio = 0,
                        // HasVideo = 0,
                        CommentCount = 0,
                        LikeCount = 0,
                        // HasImage = 0,
                        AudioPath = string.Empty,
                        IsGallery = 0,
                        VideoPath = string.Empty,
                        IsFocusNews = (byte)(canFocus ? item.IsFocusNews : 0),
                        IsList = 0,
                        IsTop = 0,
                        IsPinned = item.IsPinned,
                        IsFeatured = item.IsFeatured,
                        ViewCount = 0,
                        ThumbnailCopyright = item.ThumbnailCopyright,
                        AddTime = currentTime,
                        UpdateTime = currentTime,
                        QStatus = qStatus
                    });
                    if (res > 0)
                    {
                        item.Id = res ?? 0; //ArticleId

                        UpdateMediaInfo(connection, item.ThumbnailUrl, "");
                        foreach (var mediaPath in HtmlAgilityPackHelper.GetMediaPathList(item.FullDescription))
                            UpdateMediaInfo(connection, mediaPath, "");

                        InsertOrEditTagList(connection, tagList, nameof(Article), item.Id);
                        if (item.IsPinned == 1)
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                        if (item.IsFocusNews == 1)
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
                        //Reset Article Latyn Url With Article id

                        item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Article), "", item.Title, item.Id, "");

                        connection.Execute("update article set latynUrl = @latynUrl where id = @articleId", new { articleId = item.Id, latynUrl = item.LatynUrl });
                        tran.Commit();
                        return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                    }
                }

                else
                {
                    item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Article), "", item.Title, item.Id, "");
                    var article = connection.GetList<Article>("where qStatus <> 1 and id = @id", new { id = item.Id })
                        .FirstOrDefault();
                    if (article == null)
                        return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
                    var oldArticle = article.Clone();

                    var pinnedIsChanged = article.IsPinned != item.IsPinned;
                    var focusIsChanged = article.IsFocusNews != item.IsFocusNews;

                    article.CategoryId = item.CategoryId;
                    article.Title = item.Title;
                    article.LatynUrl = item.LatynUrl;
                    article.IsPinned = item.IsPinned;
                    article.SurveyId = item.SurveyId;
                    article.IsFeatured = item.IsFeatured;
                    article.ThumbnailCopyright = item.ThumbnailCopyright;


                    if (!article.ThumbnailUrl.Equals(item.ThumbnailUrl))
                    {
                        UpdateMediaInfo(connection, item.ThumbnailUrl, article.ThumbnailUrl);
                        article.ThumbnailUrl = item.ThumbnailUrl;
                    }

                    if (canFocus) article.IsFocusNews = item.IsFocusNews;

                    if (canSchedule) article.AutoPublishTime = item.AutoPublishTime;

                    var oldMediaPathList = HtmlAgilityPackHelper.GetMediaPathList(article.FullDescription);
                    article.ShortDescription = item.ShortDescription;
                    article.FullDescription = item.FullDescription;
                    article.UpdateAdminId = GetAdminId();
                    article.UpdateTime = currentTime;
                    article.QStatus = qStatus;
                    res = connection.Update(article);
                    
                    var success = UpdateDiff(connection, oldArticle, article, tagList, currentTime);
                    if (res > 0 && success)
                    {
                        InsertOrEditTagList(connection, tagList, nameof(Article), article.Id);
                        var newMediaList = HtmlAgilityPackHelper.GetMediaPathList(item.FullDescription);
                        foreach (var oldMediaPath in oldMediaPathList)
                            if (!newMediaList.Contains(oldMediaPath))
                                UpdateMediaInfo(connection, "", oldMediaPath); //Remove Old Media

                        foreach (var newMediaPath in newMediaList)
                            if (!oldMediaPathList.Contains(newMediaPath))
                                UpdateMediaInfo(connection, newMediaPath, ""); //Add New Media
                        var languageList = QarCache.GetLanguageList(_memoryCache);
                        languageList.ForEach(x =>
                            _memoryCache.Remove(GetArticleCacheName(article.LatynUrl, x.LanguageCulture)));

                        if (pinnedIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                        if (focusIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));

                        tran.Commit();
                        return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={article.Id}",
                            "");
                    }
                }
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Log.Error(ex, ActionName);
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get Article List +GetArticleList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetArticleList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        var timeSelectSql = string.Empty;
        if (!string.IsNullOrWhiteSpace(model.DateTimeStart) && !string.IsNullOrWhiteSpace(model.DateTimeEnd))
        {
            var startTime = UnixTimeHelper.ConvertToUnixTime(model.DateTimeStart);
            var endTime = UnixTimeHelper.ConvertToUnixTime(model.DateTimeEnd);
            if (startTime > 0 && endTime > 0 && startTime < endTime)
            {
                timeSelectSql = $" and addTime >= {startTime} and addTime <= {endTime} ";
            }
        }

        using var connection = Utilities.GetOpenConnection();
        var querySql = " from article where qStatus <> 1 " + timeSelectSql;
        object queryObj = new { keyword };
        var orderSql = "";
        if (!string.IsNullOrWhiteSpace(keyword))
            querySql += " and match(title, fullDescription) against(@keyword in natural language mode) ";

        if (model.OrderList != null && model.OrderList.Count > 0)
            foreach (var item in model.OrderList)
                switch (item.Column)
                {
                    case 3:
                    case 4:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                    }
                        break;
                }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var adminList = QarCache.GetAllAdminList(_memoryCache);
        var categoryList = QarCache.GetCategoryList(_memoryCache);
        var dataList = connection
            .Query<Article>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                queryObj).Select(x => new
            {
                x.Id,
                x.Title,
                AddAdmin = adminList.FirstOrDefault(a => a.Id == x.AddAdminId)?.Name,
                UpdateAdmin = adminList.FirstOrDefault(a => a.Id == x.UpdateAdminId)?.Name,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl)
                    ? NoImage
                    : x.ThumbnailUrl.Replace("_big.", "_small."),
                AutoPublishTime = x.QStatus == 2
                    ? UnixTimeHelper.UnixTimeToDateTime(x.AutoPublishTime).ToString("dd/MM/yyyy HH:mm")
                    : "",
                LatynUrl =
                    $"/{categoryList.FirstOrDefault(c => c.Id == x.CategoryId)?.Language}/article/{x.LatynUrl}.html",
                x.ViewCount,
                x.ThumbnailCopyright,
                x.QStatus,
                AddTime = x.AddTime > 0
                    ? UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
                    : ""
            }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion

    #region Set Article Status +SetArticleStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetArticleStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
            {
                using var connection = Utilities.GetOpenConnection();
                using var tran = connection.BeginTransaction();
                try
                {
                    var articleList = connection
                        .GetList<Article>("where qStatus <> 1 and id in @idList", new { idList }).ToList();
                    foreach (var article in articleList)
                    {
                        article.QStatus = 1;
                        article.UpdateTime = currentTime;
                        UpdateMediaInfo(connection, "", article.ThumbnailUrl);
                        connection.Update(article);
                    }

                    tran.Commit();
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetTopArticleList));
                    return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ActionName);
                    tran.Rollback();
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            default:
            {
                return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
            }
        }
    }

    #endregion

    #region Survey +Survey(string query)

    public IActionResult Survey(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Surveys");
        switch (query)
        {
            case "create":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "edit":
            {
                var questionId = GetIntQueryParam("id", 0);
                if (questionId <= 0)
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                using (var connection = Utilities.GetOpenConnection())
                {
                    var question = connection
                        .GetList<Question>("where qStatus <> 1 and id = @questionId ",
                            new { questionId }).FirstOrDefault();
                    if (question == null)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    ViewData["question"] = question;

                    ViewData["answerList"] = connection
                        .GetList<Answer>("where qStatus = 0 and questionId = @questionId",
                            new { questionId = question.Id }).ToList();
                    ViewData["multiLanguageList"] = GetMultilanguageList(connection, nameof(question),
                        new List<int> { question.Id });
                }

                return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
            }
            case "list":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            default:
            {
                return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
            }
        }
    }

    #endregion

    #region Survey +Survey(Question item)

    [HttpPost]
    public IActionResult Survey(Question item, string answerItemJson)
    {
        if (string.IsNullOrEmpty(item.Title))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "title");
        if (string.IsNullOrEmpty(item.Type))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "Type");

        List<Answer> answerItemList;
        try
        {
            answerItemList = JsonHelper.DeserializeObject<List<Answer>>(answerItemJson);
        }
        catch (Exception ex)
        {
            return MessageHelper.RedirectAjax(ex.Message, "error", "", null);
        }

        for (var i = 0; i < answerItemList.Count; i++)
            if (string.IsNullOrWhiteSpace(answerItemList[i].Content))
                answerItemList.Remove(answerItemList[i]);
        if (answerItemList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "Content");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();

        using (var connection = Utilities.GetOpenConnection())
        {
            if (item.Id == 0)
            {
                var res = connection.Insert(new Question
                {
                    Title = item.Title,
                    Type = item.Type,
                    Language = item.Language,
                    AddAdminId = GetAdminId(),
                    UpdateAdminId = GetAdminId(),
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 3
                });


                const int displayOrder = 0;
                foreach (var answerItem in answerItemList)
                    connection.Insert(new Answer
                    {
                        QuestionId = res ?? 0,
                        VoteCount = 0,
                        Content = answerItem.Content,
                        DisplayOrder = displayOrder + 1,
                        AddTime = currentTime,
                        UpdateTime = currentTime,
                        QStatus = 0
                    });
                if (res > 0)
                {
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPublishQuestionList));
                    return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list", "");
                }
            }
            else
            {
                var question = connection
                    .GetList<Question>("where qStatus <> 1 and id = @questionId", new { questionId = item.Id })
                    .FirstOrDefault();
                if (question == null)
                    return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");

                question.Title = item.Title;
                question.Type = item.Type;
                question.Language = item.Language;
                question.UpdateAdminId = GetAdminId();
                question.UpdateTime = currentTime;
                connection.Update(question);

                var currentAnswerList = connection.GetList<Answer>("WHERE qStatus <> 1 and questionId = @questionId ",
                    new { questionId = question.Id }).ToList();
                foreach (var currentAnswer in currentAnswerList)
                {
                    var newAnswer = answerItemList.FirstOrDefault(x => x.Id == currentAnswer.Id);
                    if (newAnswer != null)
                        currentAnswer.Content = newAnswer.Content;
                    else
                        currentAnswer.QStatus = 1;
                    currentAnswer.UpdateTime = currentTime;
                    connection.Update(currentAnswer);
                }

                var displayOrder = currentAnswerList.Where(x => x.QStatus != 1).Max(x => x.DisplayOrder);
                foreach (var answerItem in answerItemList)
                    if (!currentAnswerList.Any(x => x.Id == answerItem.Id))
                        connection.Insert(new Answer
                        {
                            QuestionId = question.Id,
                            VoteCount = 0,
                            Content = answerItem.Content,
                            DisplayOrder = displayOrder + 1,
                            AddTime = currentTime,
                            UpdateTime = currentTime,
                            QStatus = 0
                        });
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPublishQuestionList));
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                    $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list", "");
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get Survey List GetSurveyList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetSurveyList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using var connection = Utilities.GetOpenConnection();
        var querySql = " from question where qStatus <> 1 ";
        object queryObj = new { keyword = "%" + keyword + "%" };
        var orderSql = "";
        if (!string.IsNullOrEmpty(keyword)) querySql += " and (title like @keyword)";

        if (model.OrderList != null && model.OrderList.Count > 0)
            foreach (var item in model.OrderList)
                switch (item.Column)
                {
                    case 3:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " displayOrder " + item.Dir;
                    }
                        break;
                    case 4:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                    }
                        break;
                }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

        orderSql = " qStatus = 0 desc,  " + orderSql;
        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var questionList = connection
            .Query<Question>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                queryObj).ToList();
        var languageList = QarCache.GetLanguageList(_memoryCache);
        var dataList = questionList.Select(x => new
        {
            x.Id,
            x.Title,
            Language = languageList
                .FirstOrDefault(l => l.LanguageCulture.Equals(x.Language, StringComparison.OrdinalIgnoreCase))
                ?.FullName,
            AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm"),
            x.QStatus
        }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion

    #region SetSurveyStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetSurveyStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
            {
                using var connection = Utilities.GetOpenConnection();
                using var tran = connection.BeginTransaction();
                try
                {
                    var questionList = connection
                        .GetList<Question>($"where qStatus = 0 and id in ({string.Join(",", idList)})")
                        .ToList();
                    foreach (var question in questionList)
                    {
                        question.QStatus = 1;
                        question.UpdateTime = currentTime;
                        connection.Update(question);
                    }

                    tran.Commit();
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPublishQuestionList));
                    return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ActionName);
                    tran.Rollback();
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            case "hide":
            {
                using var connection = Utilities.GetOpenConnection();
                using var tran = connection.BeginTransaction();
                try
                {
                    var question = connection
                        .GetList<Question>($"where qStatus <> 1 and id in ({string.Join(",", idList)})")
                        .FirstOrDefault();
                    if (question == null)
                        return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");

                    if (question.QStatus == 0)
                    {
                        question.QStatus = 3;
                    }
                    else
                    {
                        question.QStatus = 0;
                        connection.Execute("update question set qStatus = 3 where qStatus <> 1;");
                    }

                    question.UpdateTime = currentTime;
                    connection.Update(question);
                    tran.Commit();
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPublishQuestionList));
                    return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ActionName);
                    tran.Rollback();
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            default:
            {
                return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
            }
        }
    }

    #endregion

    #region Get Advertise List +GetAdvertiseList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetAdvertiseList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyWord = (model.Keyword ?? string.Empty).Trim();
        using (var _connection = Utilities.GetOpenConnection())
        {
            var querySql = " from advertise where (qStatus = 0 or qStatus = 3)";
            object queryObj = new { keyWord = "%" + keyWord + "%" };
            var orderSql = "";
            if (!string.IsNullOrEmpty(keyWord))
            {
                querySql += " and (title like @keyWord)";
            }

            if (model.OrderList != null && model.OrderList.Count > 0)
            {
                foreach (var item in model.OrderList)
                {
                    switch (item.Column)
                    {
                        case 3:
                        {
                            orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " displayOrder " + item.Dir;
                        }
                            break;
                        case 4:
                        {
                            orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                        }
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(orderSql))
            {
                orderSql = " addTime desc ";
            }

            orderSql = "CASE WHEN qStatus = 0 THEN 0 ELSE 1 END, " + orderSql;

            var total = _connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
            var totalPage = total % length == 0 ? total / length : total / length + 1;
            var advertiseList = _connection
                .Query<Advertise>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                    queryObj).ToList();

            var dataList = advertiseList.Select(x => new
            {
                x.Id,
                x.DisplayOrder,
                x.Language,
                x.Title,
                x.QStatus,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? NoImage : x.ThumbnailUrl,
                x.ViewCount,
                AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
            }).ToList();
            return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
                new { start, length, keyWord, total, totalPage, dataList });
        }
    }

    #endregion

    #region Set Advertise Status +SetAdvertiseStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetAdvertiseStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count() == 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", null);
        var currentTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now);
        switch (manageType)
        {
            case "delete":
            {
                using (var _connection = Utilities.GetOpenConnection())
                {
                    using (var _tran = _connection.BeginTransaction())
                    {
                        try
                        {
                            var advertiseList = _connection
                                .GetList<Advertise>($"where qStatus = 0 and id in ({string.Join(",", idList)})")
                                .ToList();
                            foreach (var advertise in advertiseList)
                            {
                                advertise.QStatus = 1;
                                advertise.UpdateTime = currentTime;
                                _connection.Update(advertise);
                            }

                            _tran.Commit();
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetAdvertiseList));
                            return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ActionName);
                            _tran.Rollback();
                            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                        }
                    }
                }
            }
            case "hide":
            {
                using (var _connection = Utilities.GetOpenConnection())
                {
                    using (var _tran = _connection.BeginTransaction())
                    {
                        try
                        {
                            var advertise = _connection
                                .GetList<Advertise>($"where qStatus <> 1 and id in ({string.Join(",", idList)})")
                                .FirstOrDefault();

                            if (advertise == null)
                                return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");

                            if (advertise.QStatus == 0)
                            {
                                advertise.QStatus = 3;
                            }
                            else
                            {
                                advertise.QStatus = 0;
                                // _connection.Execute("update surveys set qStatus = 3 where qStatus <> 1;");
                            }

                            advertise.UpdateTime = currentTime;
                            _connection.Update(advertise);
                            _tran.Commit();
                            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetAdvertiseList));
                            return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ActionName);
                            _tran.Rollback();
                            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                        }
                    }
                }
            }
            default:
            {
                return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
            }
        }
    }

    #endregion
    
    #region About +About()

    public IActionResult About()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_Abouttheproject");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion
    
    #region Rules +Rules()

    public IActionResult Rules()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_Websiterules");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion
    
    #region Security +Security()

    public IActionResult Security()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_Security");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion
    
    #region Security +Security()

    public IActionResult Advertise()
    {
        var additionalType = ActionName;
        using (var connection = Utilities.GetOpenConnection())
        {
            ViewData["additionalContent"] = AdditionalContent(connection, additionalType);
        }

        ViewData["title"] = T("ls_Advertise");

        ViewData["showFieldList"] = new List<string>
        {
            nameof(Additionalcontent.Title),
            nameof(Additionalcontent.FullDescription)
        };
        return View("~/Views/Console/QarBase/AdditionalContent.cshtml");
    }

    #endregion
    

    #region Advertise +NewsInput(string query)

    public IActionResult NewsInput(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Suggestedarticles");
        switch (query)
        {
            case "list":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            case "accept":
            {
                var sugeestionId = GetIntQueryParam("id", 0);
                if (sugeestionId <= 0)
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            default:
            {
                return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
            }
        }
    }

    #endregion


    [HttpPost]
    public IActionResult NewsInput(Newsinput item, IFormFile file)
    {
        if (item == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");
        if (file == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "file");
        if (string.IsNullOrWhiteSpace(item.UserName))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "userName");
        if (string.IsNullOrWhiteSpace(item.Email))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "email");
        if (string.IsNullOrWhiteSpace(item.Phone))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "phone");
        if (!RegexHelper.IsPhoneNumber(item.Phone, out string newPhoneNumber))
            return MessageHelper.RedirectAjax(T("ls_Peavpn"), "error", "", "phone");
        if (!RegexHelper.IsEmail(item.Email))
            return MessageHelper.RedirectAjax(T("ls_Peavea"), "error", "", "email");
        var relativePath = string.Empty;
        var absolutePath = string.Empty;
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var dateTime = UnixTimeHelper.UnixTimeToDateTime(currentTime);

        try
        {
            var fileFormat = Path.GetExtension(file.FileName).ToLower();
            if (!fileFormat.Equals(".pdf") && !fileFormat.Equals(".docx"))
            {
                var message = T("ls_Ieffnoefas").Replace("{name}", fileFormat).Replace("{extensions}", ".pdf,.docx");
                return MessageHelper.RedirectAjax(message, "error", "", "file");
            }

            var fileName = dateTime.ToString("ddHHmmssfff");
            relativePath =
                $"/uploads/files/{dateTime.Year}/{(dateTime.Month < 10 ? $"0{dateTime.Month}" : dateTime.Month)}/{fileName}{fileFormat}";
            absolutePath = PathHelper.Combine(_environment.WebRootPath, relativePath);
            FileHelper.EnsureDir(absolutePath);
            using var stream = System.IO.File.OpenWrite(absolutePath);
            file.CopyTo(stream);

            using var connection = Utilities.GetOpenConnection();
            var id = connection.Insert(new Newsinput
            {
                AddTime = currentTime,
                UpdateTime = currentTime,
                Ip = GetIPAddress(),
                Email = item.Email,
                FilePath = relativePath,
                Phone = newPhoneNumber,
                UserName = item.UserName,
                QStatus = 3
            });
            if (id > 0)
            {
                return MessageHelper.RedirectAjax(T("ls_Savedsuccessfully"), "success", "", "");
            }
        }
        catch
        {
            return MessageHelper.RedirectAjax(T("ls_Oswwptal"), "error", "", "");
        }

        return MessageHelper.RedirectAjax(T("ls_Oswwptal"), "error", "", "");
    }


    [HttpPost]
    public IActionResult SetNewsinputStatus(string manageType, List<int> idList)
    {
        manageType = (manageType ?? string.Empty).Trim().ToLower();
        if (idList == null || idList.Count == 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", null);
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        switch (manageType)
        {
            case "delete":
            {
                using var connection = Utilities.GetOpenConnection();
                using var tran = connection.BeginTransaction();
                try
                {
                    var articleList = connection
                        .GetList<Article>("where qStatus <> 1 and id in @idList", new { idList }).ToList();
                    foreach (var article in articleList)
                    {
                        article.QStatus = 1;
                        article.UpdateTime = currentTime;
                        UpdateMediaInfo(connection, "", article.ThumbnailUrl);
                        connection.Update(article);
                    }

                    tran.Commit();
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetTopArticleList));
                    return MessageHelper.RedirectAjax(T("ls_Deletedsuccessfully"), "success", "", "");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ActionName);
                    tran.Rollback();
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            default:
            {
                return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", null);
            }
        }
    }

    #region Get Survey List GetSurveyList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetNewsInputList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using var connection = Utilities.GetOpenConnection();
        var querySql = " from newsinput where qStatus <> 1 ";
        object queryObj = new { keyword = "%" + keyword + "%" };
        var orderSql = "";
        if (!string.IsNullOrEmpty(keyword)) querySql += " and (title like @keyword)";
        if (model.OrderList != null && model.OrderList.Count > 0)
            foreach (var item in model.OrderList)
                switch (item.Column)
                {
                    case 4:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                    }
                        break;
                }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";
        orderSql = " qStatus  desc,  " + orderSql;
        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var questionList = connection
            .Query<Newsinput>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}",
                queryObj).ToList();
        var languageList = QarCache.GetLanguageList(_memoryCache);
        var dataList = questionList.Select(x => new
        {
            x.Id,
            x.Email,
            x.Phone,
            x.AddTime,
            x.UserName,
            x.FilePath,
            x.QStatus
        }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion


    #region UpdateLog +UpdateLog(string query)

    public IActionResult UpdateLog(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Updatelog");

        switch (query)
        {
            case "view":
            {
                var itemId = GetIntQueryParam("id", 0);
                if (itemId <= 0)
                    return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");

                using (var connection = Utilities.GetOpenConnection())
                {
                    var updateLogList = connection.Query<UpdateLog>(
                        "select id, adminId, addTime from updatelog where qStatus = 0 and tableName = @tableName and itemId = @itemId order by addTime desc",
                        new { tableName = nameof(MODEL.Article), itemId }).ToList();

                    var updateContentList = connection
                        .GetList<UpdateContent>("where logId in @logIdList",
                            new { logIdList = updateLogList.ConvertAll(x => x.Id) })
                        .ToList();

                    ViewData["updateLogList"] = updateLogList;
                    ViewData["updateContentList"] = updateContentList;
                    ViewData["adminList"] = QarCache.GetAllAdminList(_memoryCache);
                }

                return View($"~/Views/Console/{ControllerName}/{ActionName}/View.cshtml");
            }
            case "list":
            {
                return View($"~/Views/Console/{ControllerName}/{ActionName}/List.cshtml");
            }
            default:
            {
                return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
            }
        }
    }

    #endregion

    #region Get update log list +GetUpdateLogList(ApiUnifiedModel model)

    [HttpPost]
    public IActionResult GetUpdateLogList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        object queryObj = new { keyword = "%" + keyword + "%", tableName = nameof(MODEL.Article) };

        var orderSql = "";
        if (model.OrderList is { Count: > 0 })
        {
            foreach (var item in model.OrderList)
            {
                switch (item.Column)
                {
                    case 2:
                    {
                        orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                    }
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

        var conditionSql = string.Empty;

        if (!string.IsNullOrWhiteSpace(keyword))
            conditionSql =
                " and itemId in (select id from article where match(title, fullDescription) against(@keyword in natural language mode)) "; // qStatus <> 1 and

        var querySql =
            $"from (select itemId from updatelog where qStatus = 0 and tableName = @tableName {conditionSql} order by {orderSql}) as ordered ";

        using var connection = Utilities.GetOpenConnection();

        var total = connection.Query<int>($"select count(distinct itemId) {querySql}", queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;

        var itemIdList = connection
            .Query<int>(
                $"select distinct itemId {querySql} limit {start}, {length}", queryObj)
            .ToList();

        var adminList = QarCache.GetAllAdminList(_memoryCache);
        var categoryList = QarCache.GetCategoryList(_memoryCache);

        var dataList = connection
            .Query<Article>(
                "select id, title, categoryId, addAdminId, updateAdminId, thumbnailUrl, latynUrl, updateTime from article where id in @itemIdList", // qStatus = 0 and
                new { itemIdList })
            .Select(x => new
            {
                x.Id,
                x.Title,
                AddAdmin = adminList.FirstOrDefault(a => a.Id == x.AddAdminId)?.Name,
                UpdateAdmin = adminList.FirstOrDefault(a => a.Id == x.UpdateAdminId)?.Name,
                ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl)
                    ? NoImage
                    : x.ThumbnailUrl.ConvertImgSize(ImgSize.Small),
                LatynUrl =
                    $"/{categoryList.FirstOrDefault(c => c.Id == x.CategoryId)?.Language}/article/{x.LatynUrl}.html",
                UpdateTime = UnixTimeHelper.UnixTimeToDateTime(x.UpdateTime).ToString("dd/MM/yyyy HH:mm")
            }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion
}