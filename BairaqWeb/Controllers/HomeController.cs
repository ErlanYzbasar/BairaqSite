using System.Net;
using BairaqWeb.Attributes;
using BairaqWeb.Caches;
using COMMON;
using COMMON.Extensions;
using Dapper;
using DBHelper;
using MODEL;
using MODEL.ViewModels;
using Serilog;
using static System.Int32;

namespace BairaqWeb.Controllers;

[NoRole]
public class HomeController : QarBaseController
{
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Index +Index()

    public IActionResult Index()
    {
        ViewData["pinnedArticleList"] = QarCache.GetPinnedArticleList(_memoryCache, CurrentLanguage, 4);
        ViewData["focusArticleList"] = QarCache.GetFocusArticleList(_memoryCache, CurrentLanguage, 8);
        ViewData["youtubeArticleList"] = QarCache.GetYoutubeArticleList(_memoryCache, CurrentLanguage, 4);
        var categoryList = QarCache.GetCategoryList(_memoryCache, CurrentLanguage);
        ViewData["categoryList"] = categoryList;
        foreach (var category in categoryList.Where(x => !string.IsNullOrEmpty(x.BlockType)).ToList())
        {
            var takeCount = category.DisplayOrder switch
            {
                1 => 5,
                2 => 8,
                3 => 8,
                4 => 5,
                5 => 8,
                6 => 8,
                _ => 5
            };
            ViewData[$"block{category.DisplayOrder}Url"] = $"/{CurrentLanguage}/category/{category.LatynUrl}";
            ViewData[$"block{category.DisplayOrder}Title"] = category.Title;
            ViewData[$"block{category.DisplayOrder}ArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, takeCount, category.Id);
        }
        
        ViewData["latestArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 25);
        ViewData["regionArticleList"] = QarCache.GetRegionArticleList(_memoryCache, CurrentLanguage, 6);


        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Category +Category(string query)

    public IActionResult Category(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Return404();
        latynUrl = latynUrl[..^5];
        var category = QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
            .FirstOrDefault(x => x.LatynUrl.Equals(latynUrl, StringComparison.OrdinalIgnoreCase));
        ViewData["latynUrl"] = latynUrl;
        return category == null ? Redirect("/404.html") : Article($"category-{category.Id}");
    }

    #endregion

    #region Tag +Tag(string query)

    public IActionResult Tag(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl)) return Redirect("/404.html");
        return Article($"tag-{latynUrl}");
    }

    #endregion

    #region Author +Author(string query)

    public IActionResult Author(string query)
    {
        var latynUrl = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(latynUrl)) return Redirect("/404.html");
        return string.IsNullOrWhiteSpace(latynUrl) ? Redirect("/404.html") : Article($"author-{latynUrl}");
    }

    #endregion

    #region Мақала +Article(string query)

    public IActionResult Article(string query)
    {
        var categoryList = QarCache.GetCategoryList(_memoryCache, CurrentLanguage);

        ViewData["categoryList"] = categoryList;
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["latestArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 25);
        ViewData["popularTagList"] = QarCache.GetPopularTagList(_memoryCache, CurrentLanguage, 10);
        ViewData["topArticleList"] = QarCache.GetTopArticleList(_memoryCache, CurrentLanguage, 100, 7);
        var categoryId = 0;
        string authorUrl = string.Empty, tagUrl = string.Empty;

        if (query.StartsWith("category-") && TryParse(query.Split('-')[1], out categoryId))
        {
            query = "list";
        }

        if (query.Length > 7 && query.StartsWith("author-") && !string.IsNullOrWhiteSpace(query[7..]))
        {
            authorUrl = query[7..];
            query = "list";
        }

        if (query.Length > 4 && query.StartsWith("tag-") && !string.IsNullOrWhiteSpace(query[4..]))
        {
            tagUrl = query[4..];
            query = "list";
        }

        ViewData["query"] = query;

        using var connection = Utilities.GetOpenConnection();
        switch (query)
        {
            case "list":
            {
                var page = GetIntQueryParam("page", 1);
                var pageSize = GetIntQueryParam("pageSize", 12);
                var keyword = GetStringQueryParam("keyword");
                page = page <= 1 ? 1 : page;
                pageSize = pageSize <= 0 ? 12 : pageSize;
                var querySql = " where qStatus = 0 ";
                var addAdminId = 0;

                var subSelectSql = string.Empty;
                var subOrderBySql = string.Empty;
                var isEmpty = false;
                var ogTitle = T("ls_Articles");
                if (categoryId > 0)
                {
                    var category = QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
                        .FirstOrDefault(x => x.Id == categoryId);
                    var subCategoryList = connection
                        .GetList<Articlecategory>("where qStatus = 0 and parentId = @categoryId", new { categoryId })
                        .ToList();
                    if (subCategoryList.Count > 0)
                    {
                        ViewData["subCategoryList"] = subCategoryList;
                        var categoryIdArr = subCategoryList.Select(x => x.Id).Append(categoryId).ToArray();
                        querySql += $" and categoryId in ({string.Join(',', categoryIdArr)}) ";
                    }
                    else
                    {
                        querySql += " and categoryId = @categoryId ";
                    }

                    if (category != null)
                    {
                        ogTitle = category.Title;
                        ViewData["categoryId"] = category.Id;
                        ViewData["categoryTitle"] = category.Title;
                    }
                }

                if (!string.IsNullOrWhiteSpace(authorUrl))
                {
                    querySql += "and addAdminId = @addAdminId";
                    var Id = (authorUrl ?? string.Empty).Trim().ToLower()?.Split("-")?.FirstOrDefault();
                    if (string.IsNullOrEmpty(Id)) return Redirect("/404.html");
                    TryParse(Id, out var adminId);

                    var author = connection
                        .GetList<Admin>("where id = @adminId", new { adminId })
                        .FirstOrDefault();
                    if (author != null)
                    {
                        addAdminId = author.Id;
                        
                        ViewData["author"] = author;
                        var adminRoleList = QarCache.GetAdminroleList(_memoryCache)
                            .FindAll(x => x.AdminId == author?.Id).ConvertAll(x => x.RoleId);
                        ViewData["adminRole"] = string.Join(",",
                            QarCache.GetRoleList(_memoryCache, CurrentLanguage)
                                .FindAll(x => adminRoleList.Contains(x.Id))
                                .ConvertAll(x => x.Name));
                        ViewData["articleCount"] = connection.Query<int?>(
                                "select count(1) from article where addAdminId = @authorId and qStatus = 0",
                                new { authorId = author?.Id })
                            .FirstOrDefault();
                    }
                }

                if (!string.IsNullOrWhiteSpace(tagUrl))
                {
                    var tag = connection.GetList<Tag>("where latynUrl = @tagUrl", new { tagUrl }).FirstOrDefault();
                    if (tag == null)
                    {
                        isEmpty = true;
                    }
                    else
                    {
                        connection.Execute("insert into taglog values (@tagId , @taggedTime) ",
                            new { tagId = tag.Id, taggedTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now) });
                        tag.TaggedCount++;
                        connection.Update(tag);
                        ogTitle = $"{T("ls_Tags")}:" + tag.Title;
                        ViewData["tagTitle"] = tag.Title;
                        var articleIds = connection
                            .Query<int>(
                                $"select itemId from {nameof(Tagmap).ToLower()} where qStatus = 0 and tagId = @tagId",
                                new { tagId = tag.Id }).ToArray();
                        if (articleIds.Length > 0)
                        {
                            querySql += $" and id in ({string.Join(',', articleIds)}) ";
                        }

                        ViewData["tagUrl"] = tagUrl;
                    }
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    querySql += " and match(title, fullDescription) against(@keyword in natural language mode) ";
                    ogTitle = T("ls_Search") + ':' + keyword;
                    ViewData["keyword"] = keyword;
                    // querySql += " and MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE) ";
                    // querySql += " and match(title, fullDescription) against(@keyword IN NATURAL LANGUAGE MODE) ";
                    // subSelectSql = ", (MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) * 2 AS title_relevance, (MATCH(fullDescription) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) AS description_relevance ";
                    // subOrderBySql = " (MATCH(title) AGAINST(@keyword IN NATURAL LANGUAGE MODE)) DESC, ";
                }

                object queryObj = new { categoryId, keyword, addAdminId };

                var total = isEmpty ? 0 : connection.RecordCount<Article>(querySql, queryObj);
                var articleList = isEmpty
                    ? new List<Article>()
                    : connection.Query<Article>(
                        $"select id, title, shortDescription,addAdminId, thumbnailUrl, latynUrl, addTime, viewCount {subSelectSql} from article {querySql} order by {subOrderBySql} addTime desc limit {(page - 1) * pageSize}, {pageSize} ",
                        queryObj).Select(x => new Article
                    {
                        Id = x.Id,
                        Title = x.Title,
                        ShortDescription = x.ShortDescription,
                        ThumbnailUrl = string.IsNullOrEmpty(x.ThumbnailUrl) ? NoImage : x.ThumbnailUrl,
                        LatynUrl = x.LatynUrl,
                        AddTime = x.AddTime,
                        ViewCount = x.ViewCount,
                    }).ToList();
                ViewData["page"] = page;
                ViewData["pageSize"] = pageSize;
                ViewData["total"] = total;
                ViewData["totalPage"] = total % pageSize == 0 ? total / pageSize : total / pageSize + 1;
                ViewData["articleList"] = articleList;

                if (!string.IsNullOrWhiteSpace(authorUrl))
                {
                    ogTitle = $"{T("ls_Author")}: {authorUrl}";
                }

                ViewData["og_title"] = ogTitle;
                ViewData["menuTitle"] = ogTitle;
                ViewData["keyword"] = keyword;
                return View($"~/Views/Themes/{CurrentTheme}/Home/ArticleList.cshtml");
            }
            default:
            {
                var latynUrl = query.Trim().ToLower();
                if (string.IsNullOrEmpty(latynUrl) || !latynUrl.EndsWith(".html")) return Return404();
                latynUrl = latynUrl[..^5];
                return CacheArticle(latynUrl);
            }
        }
    }

    #endregion

    #region Save Aritcle Rating +SaveArticleRating(int articleId, string ratingType)

    [HttpPost]
    public IActionResult SaveArticleRating(int articleId, string ratingType)
    {
        ratingType = (ratingType ?? string.Empty).Trim().ToLower();
        if (articleId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "articleId");

        string[] ratingTypeArr = { "satisfied", "dissatisfied", "funny", "outrageous" };

        if (string.IsNullOrEmpty(ratingType) || !ratingTypeArr.Contains(ratingType))
            return MessageHelper.RedirectAjax(T("ls_Managetypeerror"), "error", "", ratingType);

        const string cookieKey = "ratingAritcleIds";

        try
        {
            var articleIdList = new List<int>();
            if (Request.Cookies.TryGetValue(cookieKey, out var articleIds))
            {
                articleIdList = articleIds.Split(',').Select(x => Convert.ToInt32(x)).ToList();
                if (articleIdList.Contains(articleId))
                    return MessageHelper.RedirectAjax("Сіз баға бергенсіз!", "error", "", "");
            }

            var res = 0;

            using (var connection = Utilities.GetOpenConnection())
            {
                if (connection.RecordCount<Rating>("where qStatus = 0 and articleId = @articleId ",
                        new { articleId }) == 0)
                {
                    connection.Insert(new Rating
                    {
                        ArticleId = articleId,
                        Satisfied = 0,
                        Dissatisfied = 0,
                        Funny = 0,
                        Outrageous = 0,
                        QStatus = 0
                    });

                    res = 1;
                }
                else
                {
                    res = connection.Query<int>(
                        $"select {ratingType} from {nameof(Rating).ToLower()} where qStatus = 0 and articleId = @articleId ",
                        new { articleId }).FirstOrDefault() + 1;
                }

                connection.Execute(
                    $"update rating set {ratingType} = {ratingType} + 1 where qStatus = 0 and articleId = @articleId",
                    new { articleId });
            }

            articleIdList.Add(articleId);
            Response.Cookies.Append(cookieKey, string.Join(',', articleIdList));
            return MessageHelper.RedirectAjax(T("ls_Tyfyo"), "success", "", res);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SaveAritcleRating");
            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
        }
    }

    #endregion

    #region Additional Content +AC(string query)

    public IActionResult Ac(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrWhiteSpace(query) || !query.EndsWith(".html")) return Return404();
        
        var categoryList = QarCache.GetCategoryList(_memoryCache, CurrentLanguage);
        ViewData["categoryList"] = categoryList;

        query = query[..^5];
        var ac = QarCache.GetAdditionalContentList(_memoryCache, CurrentLanguage)
            .FirstOrDefault(x => x.AdditionalType.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (ac == null) return Return404();

        ViewData["ac"] = ac;
        ViewData["query"] = query;

        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion


    [HttpPost]
    public IActionResult Comment(Comment comment)
    {
        if (comment == null)
            return MessageHelper.RedirectAjax(T("ls_Objectisempty"), "error", "", "");
        if (string.IsNullOrWhiteSpace(comment.Content))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "content");
        if (string.IsNullOrWhiteSpace(comment.UserName))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "userName");
        if (string.IsNullOrWhiteSpace(comment.Email))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "email");
        if (!RegexHelper.IsEmail(comment.Email))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "email");
        if (comment.ArticleId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Oswwptal"), "error", "", "");
        var currentTIme = UnixTimeHelper.GetCurrentUnixTime();
        try
        {
            using var connection = Utilities.GetOpenConnection();

            var articleId = connection
                .Query<int>("select id from article where qStatus = 0 and id = @articleId",
                    new { articleId = comment.ArticleId })
                .FirstOrDefault();
            if (articleId <= 0)
                return MessageHelper.RedirectAjax(T("ls_Oswwptal"), "error", "", "");
            connection.Insert(new Comment
            {
                ArticleId = comment.ArticleId,
                Ip = GetIPAddress(),
                Content = comment.Content,
                Email = comment.Email,
                UserName = comment.UserName,
                OldId = 0,
                AddTime = currentTIme,
                QStatus = 0
            });
            return MessageHelper.RedirectAjax(T("ls_Savedsuccessfully"), "success", "reload", "");
        }
        catch
        {
            return MessageHelper.RedirectAjax(T("ls_Oswwptal"), "error", "", "");
        }
    }

    #region Question +Question(int questionId, string answerIds)

    [HttpPost]
    public IActionResult Question(int questionId, string answerIds)
    {
        if (questionId <= 0)
            return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");

        if (string.IsNullOrEmpty(answerIds))
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", "");
        List<int> answerIdList = null;
        try
        {
            var articleIdList = new List<int>();
            const string cookieKey = "question";
            if (Request.Cookies.TryGetValue(cookieKey, out var questionIdStr))
            {
                if (questionIdStr == questionId.ToString())
                    return MessageHelper.RedirectAjax(T("ls_Youalreadyappreciated") + "!", "error", "", "");
            }

            answerIdList = JsonHelper.DeserializeObject<List<int>>(answerIds);
            if (answerIdList.Count == 0)
                return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", "");
            using (var _connection = Utilities.GetOpenConnection())
            {
                var answerList = _connection
                    .GetList<Answer>("where qStatus = 0 and  questionId = @questionId", new { questionId })
                    .ToList();
                foreach (var answer in answerList)
                {
                    if (answerIdList.Contains(answer.Id))
                    {
                        answer.VoteCount += 1;
                        _connection.Update(answer);
                    }
                }

                Response.Cookies.Append(cookieKey, questionId.ToString());
                var allVouteCount = answerList.Sum(x => x.VoteCount);
                QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPublishQuestionList));
                return MessageHelper.RedirectAjax(T("ls_Savedsuccessfully"), "success", "", answerList.Select(x =>
                    new
                    {
                        answerId = x.Id,
                        x.VoteCount,
                        allVouteCount
                    }).ToList());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SaveAritcleRating");
            return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
        }
    }

    #endregion

    #region CacheArticle -CacheArticle(string latynUrl)

    private IActionResult CacheArticle(string latynUrl)
    {
        var (model, newLatynUrl) = GetArticle(latynUrl, CurrentLanguage);

        if (model?.Article == null)
        {
            return string.IsNullOrEmpty(newLatynUrl)
                ? Return404()
                : RedirectPermanent($"/{CurrentLanguage}/article/{newLatynUrl}.html");
        }

        var article = model.Article;

        ViewData["article"] = article;
        ViewData["lastArticle"] = model.LastArticle;
        ViewData["nextArticle"] = model.NextArticle;
        ViewData["rating"] = model.Rating;
        ViewData["recArticleList"] = model.RecArticleList;
        ViewData["question"] = QarCache.GetPublishQuestionList(_memoryCache, CurrentLanguage)
            .FirstOrDefault(x => x.Id == article.SurveyId);

        ViewData["og_title"] = article.Title;
        ViewData["og_type"] = "article";
        ViewData["og_description"] = WebUtility.HtmlDecode(article.ShortDescription);
        ViewData["og_image"] = article.ThumbnailUrl.ConvertImgSize(ImgSize.Big);
        ViewData["og_url"] = $"/{CurrentLanguage}/article/{article.LatynUrl}.html";

        var tagList = model.TagList;
        if (tagList != null)
        {
            ViewData["tagList"] = tagList;
            ViewData["og_keywords"] = string.Join(',', tagList.Select(x => x.Title).ToArray());
        }

        ViewData["latestArticleList"] = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 7);
        ViewData["menuTitle"] = QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
            .FirstOrDefault(x => x.Id == article.CategoryId)?.Title;
        ViewData["menuLink"] = $"/{CurrentLanguage}/category/" +
                               QarCache.GetCategoryList(_memoryCache, CurrentLanguage)
                                   .FirstOrDefault(x => x.Id == article.CategoryId)?.LatynUrl + ".html";

        if (HttpContext.User.Identity is { IsAuthenticated: true })
        {
            ViewData["canEditArticle"] = true;
        }
        if (Request.Cookies.TryGetValue("question", out var questionIdStr))
        {
            ViewData["questionIdStr"] = questionIdStr;
        }

        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/ArticleView.cshtml");
    }

    private (ArticleCacheModel, string) GetArticle(string latynUrl, string language)
    {
        language = language switch
        {
            "latyn" or "tote" => "kz",
            _ => language
        };
        if (string.IsNullOrEmpty(latynUrl)) return (null, string.Empty);

        var cacheName = GetArticleCacheName(latynUrl, language);

        if (!_memoryCache.TryGetValue(cacheName, out ArticleCacheModel model))
        {
            using var connection = Utilities.GetOpenConnection();

            var article = connection
                .GetList<Article>("where qStatus in (0,2,3) and latynUrl = @latynUrl", new { latynUrl })
                .FirstOrDefault();

            if (article == null)
            {
                return (null, latynUrl);
            }

            var lastArticle = connection
                .Query<Article>(
                    "select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 and id < @articleId order by id desc limit 1",
                    new { articleId = article.Id }).FirstOrDefault();
            var nextArticle = connection
                .Query<Article>(
                    "select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 and id > @articleId order by id asc limit 1",
                    new { articleId = article.Id }).FirstOrDefault();

            if (lastArticle != null)
            {
                lastArticle.ThumbnailUrl = string.IsNullOrWhiteSpace(lastArticle.ThumbnailUrl)
                    ? NoImage
                    : lastArticle.ThumbnailUrl;
            }

            if (nextArticle != null)
            {
                nextArticle.ThumbnailUrl = string.IsNullOrWhiteSpace(nextArticle.ThumbnailUrl)
                    ? NoImage
                    : nextArticle.ThumbnailUrl;
            }

            var recArticleList = new List<Article>();
            var excludeNextLastSql = "and id <> @lastArticleId and id <> @nextArticleId ";
            object queryObj = new
                { id = article.Id, lastArticleId = lastArticle?.Id ?? 0, nextArticleId = nextArticle?.Id ?? 0 };
            if (!string.IsNullOrEmpty(article.RecArticleIds))
            {
                recArticleList = connection.Query<Article>(
                    $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} and id in ({article.RecArticleIds})",
                    queryObj).ToList();
            }

            if (recArticleList.Count < 1)
            {
                var rLastArticle = connection
                    .Query<Article>(
                        $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} and id<>@id order by id desc limit 5",
                        queryObj).ToList();
                if (rLastArticle != null)
                {
                    recArticleList.AddRange(rLastArticle);
                }
            }

            if (recArticleList.Count < 2)
            {
                var rNextArticle = connection.Query<Article>(
                    $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} {(recArticleList.Count > 0 ? $" and id not in ({string.Join(",", recArticleList.Select(x => x.Id).ToArray())}) " : "")} and id > @id order by id asc limit 1",
                    queryObj).FirstOrDefault();
                if (rNextArticle == null)
                {
                    rNextArticle = connection.Query<Article>(
                        $"select id, latynUrl, title, shortDescription, thumbnailUrl, addTime from article where qStatus = 0 {excludeNextLastSql} {(recArticleList.Count > 0 ? $"and id not in ({string.Join(",", recArticleList.Select(x => x.Id).ToArray())})" : "")}  and id < @id order by id desc limit 1",
                        queryObj).FirstOrDefault();
                }

                if (rNextArticle != null)
                {
                    recArticleList.Add(rNextArticle);
                }
            }

            foreach (var recArticle in recArticleList)
            {
                recArticle.ThumbnailUrl = string.IsNullOrEmpty(recArticle.ThumbnailUrl)
                    ? NoImage
                    : recArticle.ThumbnailUrl;
            }


            var tagList = connection
                .GetList<Tag>($"where id in (select tagId from {nameof(Tagmap).ToLower()} where itemId = @itemId)",
                    new { itemId = article.Id }).ToList();
            model = new ArticleCacheModel
            {
                Article = article,
                LastArticle = lastArticle,
                NextArticle = nextArticle,
                Rating = null,
                RecArticleList = recArticleList,
                TagList = tagList
            };
        }

        if (model?.Article == null) return (null, string.Empty);
        var saveCacheTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now.AddDays(-10));
        if (model.Article.AddTime > saveCacheTime)
        {
            _memoryCache.Set(cacheName, model, TimeSpan.FromMinutes(1));
        }

        QarSingleton.GetInstance().EnqueueViewArticleId(model.Article.Id);
        return (model, string.Empty);
    }

    #endregion
}