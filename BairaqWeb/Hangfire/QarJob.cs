using System.Reflection;
using BairaqWeb.Caches;
using BairaqWeb.Controllers;
using COMMON;
using Dapper;
using DBHelper;
using Hangfire;
using MODEL;
using MODEL.ViewModels;
using Serilog;

namespace BairaqWeb.Hangfire;

public class QarJob
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public QarJob(IMemoryCache memoryCache, IWebHostEnvironment environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    #region Delete Old Log Files +JobDeleteOldLogFiles()

    public void JobDeleteOldLogFiles()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var logDirectoryPath = _environment.ContentRootPath +
                                   (_environment.ContentRootPath.EndsWith("/") ? "" : "/") + "logs";
            var directory = new DirectoryInfo(logDirectoryPath);
            if (!directory.Exists) return;
            var txtFiles = directory.GetFiles("*.txt");
            foreach (var file in txtFiles)
            {
                var timeDifference = DateTime.Now - file.CreationTime;
                if (timeDifference.Days > 7) file.Delete();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "jobDeleteOldLogFiles");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Job Save Relogin AdminIds +JobSaveReloginAdminIds()

    public void JobSaveReloginAdminIds()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            using var connection = Utilities.GetOpenConnection();
            var reloginAdminList = connection.GetList<Admin>("where reLogin = 1").ToList();
            foreach (var reloginAdmin in reloginAdminList)
                QarSingleton.GetInstance().AddReLoginAdmin(reloginAdmin.Id, reloginAdmin.UpdateTime);
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Автоматты жолданатын мақалаларды жолдау +JobPublishAutoPublishArticle()

    public void JobPublishAutoPublishArticle()
    {
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var pinnedIsChanged = false;
        var focusIsChanged = false;

        using (var connection = Utilities.GetOpenConnection())
        {
            var articleList = connection
                .Query<Article>(
                    "select id, isPinned, isFocusNews, autoPublishTime from article where QStatus = 2 and autoPublishTime <= @currentTime",
                    new { currentTime }).ToList();
            if (articleList is { Count: > 0 })
            {
                var querySql = string.Empty;
                foreach (var item in articleList)
                {
                    if (!pinnedIsChanged && item.IsPinned == 1) pinnedIsChanged = true;
                    if (!focusIsChanged && item.IsFocusNews == 1) focusIsChanged = true;
                    querySql +=
                        $"update article set addTime = {item.AutoPublishTime}, updateTime = {item.AutoPublishTime} , qStatus = 0 where id = {item.Id};";
                    if (querySql.Length > 900)
                        Task.Run(async () =>
                        {
                            await connection.ExecuteAsync(querySql);
                            querySql = string.Empty;
                        }).Wait();
                }

                if (!string.IsNullOrWhiteSpace(querySql))
                    Task.Run(async () =>
                    {
                        await connection.ExecuteAsync(querySql);
                        querySql = string.Empty;
                    }).Wait();
            }
        }

        if (pinnedIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetPinnedArticleList));
        if (focusIsChanged) QarCache.ClearCache(_memoryCache, nameof(QarCache.GetFocusArticleList));
    }

    #endregion

    #region Мақаланың көрлім санын сақтау +JobSaveArticleViewCount()

    public void JobSaveArticleViewCount()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            using var _connection = Utilities.GetOpenConnection();
            var updateSql = string.Empty;
            while (QarSingleton.GetInstance().ViewArticleIdCount() > 0)
            {
                var viewArticleId = QarSingleton.GetInstance().DequeueViewArticleId();
                if (viewArticleId > 0)
                {
                    updateSql += $" update article set viewCount = viewCount + 1 where id = {viewArticleId}; ";
                }

                if (updateSql.Length <= 2000) continue;
                _connection.Execute(updateSql);
                updateSql = string.Empty;
            }

            if (!string.IsNullOrEmpty(updateSql))
            {
                _connection.Execute(updateSql);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    public void JobSyncOldDbTag()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var takeCount = 500;
                var lastOldArticle = 0;
                using var connection = Utilities.GetOpenConnection();

                lastOldArticle = connection.Query<int?>("select max(oldId) from tag where 1 = 1")
                    .FirstOrDefault() ?? 0;

                var oldArticleList = new List<Tag>();

                #region Get Old Article List

                using var oldDbConnection = Utilities.GetOldDbConnection();
                var querySql =
                    @"SELECT tag_kk as  title ,news_id as oldId from news where news_id > @lastOldArticle and tag_kk is not null order by news_id asc limit @takeCount";
                var tagslist = oldDbConnection.Query<Tag>(querySql, new { lastOldArticle, takeCount }).ToList();
                if (tagslist.Count == 0) return;
                foreach (var tagList in tagslist)
                {
                    if (string.IsNullOrEmpty(tagList.Title)) continue;
                    foreach (var item in tagList.Title.Split(','))
                    {
                        int? res;
                        var foundItem = connection.GetList<Tag>("where title = @title", new { title = item.Trim() })
                            .FirstOrDefault();
                        if (foundItem != null)
                        {
                            res = foundItem.Id;
                        }
                        else
                        {
                            res = connection.Insert(new Tag
                            {
                                Title = item.Trim(),
                                LatynUrl = QarBaseController.GetDistinctLatynUrl(connection,
                                    nameof(Article), string.Empty, item.Trim(), 0, ""),
                                OldLatynUrl = string.Empty,
                                TaggedCount = 0,
                                OldId = tagList.OldId
                            });
                        }

                        connection.Insert(new Tagmap
                        {
                            TagId = res ?? 0,
                            ItemId = tagList.OldId,
                            TableName = nameof(Article),
                            QStatus = 0
                        });
                    }
                }
            }).Wait();
        }

        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Enqueue<QarJob>(x => x.JobSyncOldDbTag());
        }
    }


    #region Sync Collect Article Media +JobSyncOldDbArticle()

    public void JobSyncOldDbComment()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var takeCount = 500;
            var lastOldArticle = 0;
            using (var connection = Utilities.GetOpenConnection())
            {
                lastOldArticle = connection.Query<int?>("select max(oldId) from comment where 1 = 1")
                    .FirstOrDefault() ?? 0;
            }

            var commentList = new List<Comment>();
            using var oldDbConnection = Utilities.GetOldDbConnection();
            var querySql =
                "select comment_id as oldId ,comment_text as content, news_id as articleId ,is_show as qStatus ,user_name as userName , ip , email,UNIX_TIMESTAMP(created_at) as addTime from comment where comment_id >@lastOldArticle order by comment_id asc limit @takeCount ";
            commentList = oldDbConnection.Query<Comment>(querySql, new { takeCount, lastOldArticle }).ToList();
            if (commentList == null) return;
            using (var connection = Utilities.GetOpenConnection())
            {
                foreach (var comment in commentList)
                {
                    if (comment.ArticleId > 0)
                    {
                        comment.QStatus = comment.QStatus == 1 ? 0 : 1;
                        comment.Email ??= string.Empty;
                        comment.Content ??= string.Empty;
                        comment.UserName ??= string.Empty;
                        connection.Insert(comment);
                    }
                }
            }
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Enqueue<QarJob>(x => x.JobSyncOldDbComment());
        }
    }

    public void JobSyncOldDbUsers()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var adminList = new List<Admin>();
            // using (var connection = Utilities.GetOpenConnection())
            // {
            //     adminList = connection.GetList<Admin>().ToList();
            //     foreach (var admin in adminList)
            //     {
            //         connection.Update(admin);
            //     }
            // }
            using var oldDbConnection = Utilities.GetOldDbConnection();
            var querySql =
                "select user_id as id ,name, phone ,email,facebook,avatar as avatarUrl , UNIX_TIMESTAMP(created_at) as addTime,UNIX_TIMESTAMP(updated_at) as updateTime,user_url as latynUrl ,slogan as hiddenColumnJson,role_id as isSuper,about as description from users";
            adminList = oldDbConnection.Query<Admin>(querySql).ToList();
            using (var connection = Utilities.GetOpenConnection())
            {
                foreach (var admin in adminList)
                {
                  var newAdmin =   connection.GetList<Admin>("where latynUrl = @latynUrl and avatarUrl = '/images/default_avatar.png'",new{latynUrl = admin.LatynUrl}).FirstOrDefault();
                  if (newAdmin != null&&!string.IsNullOrEmpty(admin.AvatarUrl))
                  {
                      newAdmin.AvatarUrl = admin.AvatarUrl;
                      connection.Update(newAdmin);
                  }
                }
            }
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            // BackgroundJob.Enqueue<QarJob>(x => x.JobSyncOldDbComment());
        }
    }

    public void JobSyncOldDbArticle()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        var oldArticleList = new List<Article>();
        try
        {
            for (int i = 28495; i <= 75584; i++)
            {
                using var connection = Utilities.GetOpenConnection();
                Console.Clear();
                Console.Write(i);

                if (connection.RecordCount<Article>("where id = @i", new { i }) > 0) continue;
                var article = new Article
                {
                    Id = i,
                    CategoryId = 0,
                    Title = string.Empty,
                    LatynUrl = string.Empty,
                    RecArticleIds = string.Empty,
                    ThumbnailUrl = string.Empty,
                    ShortDescription = string.Empty,
                    FullDescription = string.Empty,
                    AddAdminId = 0,
                    SurveyId = 0,
                    UpdateAdminId = 0,
                    AutoPublishTime = 0,
                    FocusAdditionalFile = string.Empty,
                    SearchName = string.Empty,
                    // HasAudio = 0,
                    // HasVideo = 0,
                    CommentCount = 0,
                    LikeCount = 0,
                    // HasImage = 0,
                    IsFocusNews = 0,
                    IsList = 0,
                    IsTop = 0,
                    IsPinned = 0,
                    IsFeatured = 0,
                    ViewCount = 0,
                    ThumbnailCopyright = string.Empty,
                    AddTime = 0,
                    UpdateTime = 0,
                    AudioPath = String.Empty,
                    VideoPath = string.Empty,
                    QStatus = 1
                };
                connection.Execute(
                    "insert into article values(@id, @latynUrl, @addAdminId, @updateAdminId, @categoryId, @title, @searchName, @thumbnailUrl, @thumbnailCopyright, @shortDescription, @fullDescription, @viewCount, @commentCount, @isFocusNews, @isTop, @isList, @isPinned, @isFeatured, @focusAdditionalFile, @likeCount, @recArticleIds, @audioPath, @videoPath, @addTime, @updateTime, @autoPublishTime, @qStatus, @isGallery,@surveyId)",
                    article);
            }

//             Task.Run(() =>
//             {
//                 var takeCount = 1000;
//                 var lastOldArticle = 0;
//                 using (var connection = Utilities.GetOpenConnection())
//                 {
//                     lastOldArticle = connection.Query<int?>("select max(id) from article where title <> ''")
//                         .FirstOrDefault() ?? 0;
//                 }
//
//
//                 #region Get Old Article List
//
//                 using var oldDbConnection = Utilities.GetOldDbConnection();
//                 var querySql = $@"SELECT 
//     p.news_id as id,
//     CASE 
//         WHEN news_lang = 'kk' THEN news_name_kk
//         WHEN news_lang = 'ru' THEN news_name_ru
//         WHEN news_lang = 'en' THEN news_name_en
//         ELSE NULL
//     END AS title ,
//      CASE 
//         WHEN news_lang = 'kk' THEN news_text_kk
//         WHEN news_lang = 'ru' THEN news_text_ru
//         WHEN news_lang = 'en' THEN news_text_en
//         ELSE NULL
//     END AS fullDescription,
//     p.news_meta_description_en as shortDescription,
//     p.news_url as latynUrl,
//     p.news_image as thumbnailUrl,
//     p.sort_num as displayOrder,
//     p.view_count as viewCount,
//     p.is_show as qStatus,
//     UNIX_TIMESTAMP(p.created_at) as addTime,
// 	UNIX_TIMESTAMP(p.updated_at) as updateTime,
// 	UNIX_TIMESTAMP(p.news_date) as updateTime,
//     p.user_id as addAdminId,
//     p.user_id as updateAdminId,
//     p.news_lang as focusAdditionalFile,
//     p.news_audio as audioPath,
//     p.is_gallery as isGallery,
//     p.news_desc_image as thumbnailCopyright,
// CASE 
//         WHEN news_lang = 'kk' THEN news_meta_keywords_kk
//         WHEN news_lang = 'ru' THEN news_meta_keywords_ru
//         WHEN news_lang = 'en' THEN news_meta_keywords_en
//         ELSE NULL
//     END AS searchName
// FROM 
//     news AS p
// WHERE news_id > @lastOldArticle order by news_id limit @takeCount";
//                 oldArticleList = oldDbConnection
//                     .Query<Article>(querySql, new { lastOldArticle, takeCount }).ToList();
//
//                 #endregion
//
//                 // if (oldArticleList.Count == 0) return;
//                 // {
//                 //     using (var connection = Utilities.GetOpenConnection())
//                 //     {
//                 //         querySql = "update article set isPinned = 1 where id in (select )";
//                 //         connection.Execute();
//                 //     }
//                 // };
//
//                 #region Insert New Database
//
//                 using (var connection = Utilities.GetOpenConnection())
//                 {
//                     foreach (var oldArticle in oldArticleList)
//                     {
//                         using var tran = connection.BeginTransaction();
//                         try
//                         {
//                             #region Save Article
//
//                             if (string.IsNullOrEmpty(oldArticle.Title)) continue;
//                             if (string.IsNullOrEmpty(oldArticle.LatynUrl))
//                                 oldArticle.LatynUrl = QarBaseController.GetDistinctLatynUrl(connection,
//                                     nameof(Article), oldArticle.LatynUrl, oldArticle.Title, 0, "");
//                             oldArticle.Title = oldArticle.Title.Length > 255
//                                 ? oldArticle.Title[..250] + "..."
//                                 : oldArticle.Title;
//                             oldArticle.FullDescription =
//                                 HtmlAgilityPackHelper.ConvertShortcodeToHtml(oldArticle.FullDescription);
//                             // oldArticle.FullDescription =
//                             //     (oldArticle.FullDescription ?? string.Empty).Replace(Environment.NewLine, "<br/>");
//                             oldArticle.ShortDescription ??=
//                                 HtmlAgilityPackHelper.GetShortDescription(oldArticle.FullDescription);
//                             oldArticle.LatynUrl = oldArticle.LatynUrl.Replace("/article/", "");
//                             oldArticle.AudioPath ??= string.Empty;
//                             oldArticle.ThumbnailCopyright ??= string.Empty;
//                             oldArticle.RecArticleIds ??= string.Empty;
//                             oldArticle.QStatus = (byte)(oldArticle.QStatus == 1 ? 0 : 1);
//                             oldArticle.SurveyId = 0;
//                             oldArticle.VideoPath ??= string.Empty;
//                             var oldCategoryId = oldDbConnection
//                                 .Query<int?>("select rubric_id from news_rubric where news_id = @oldId",
//                                     new { oldId = oldArticle.Id }).FirstOrDefault() ?? 0;
//                             oldArticle.CategoryId =
//                                 connection.Query<int?>(
//                                     "select id from articlecategory where  oldId = @oldCategoryId and language = @lang",
//                                     new
//                                     {
//                                         oldCategoryId,
//                                         lang = oldArticle.FocusAdditionalFile == "kk"
//                                             ? "kz"
//                                             : oldArticle.FocusAdditionalFile
//                                     }).FirstOrDefault() ?? 0;
//                             oldArticle.FocusAdditionalFile = string.Empty;
//                             connection.Execute("insert into article values(@id, @latynUrl, @addAdminId, @updateAdminId, @categoryId, @title, @searchName, @thumbnailUrl, @thumbnailCopyright, @shortDescription, @fullDescription, @viewCount, @commentCount, @isFocusNews, @isTop, @isList, @isPinned, @isFeatured, @focusAdditionalFile, @likeCount, @recArticleIds, @audioPath, @videoPath, @addTime, @updateTime, @autoPublishTime, @qStatus, @isGallery,@surveyId)",
//                                                             oldArticle);
//
//                             // connection.Update(oldArticle);
//
//                             #endregion
//
//                             tran.Commit();
//                         }
//                         catch (Exception ex)
//                         {
//                             Log.Error(ex, key);
//                             tran.Rollback();
//                         }
//                     }
//                 }
//
//                 #endregion
//             }).Wait();
        }

        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            if (oldArticleList.Count != 0)
            {
                BackgroundJob.Enqueue<QarJob>(x => x.JobSyncOldDbArticle());
            }
        }
    }

    #endregion

    #region Sync Collect OldDb Category +JobSyncOldDbCategory()

    public void JobSyncOldDbCategory()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var oldCategoryList = new List<Articlecategory>();

                #region Get Old Category List

                using var oldDbConnection = Utilities.GetOldDbConnection();
                var querySql =
                    @"select rubric_id as  oldId, `rubric_name_kk` as title, rubric_meta_description_kk as shortDescription, rubric_url_kk as oldLatynUrl, sort_num as displayOrder, 
is_show_main as isShowMain, is_show_footer as isShowFooter, is_show as isHidden,
UNIX_TIMESTAMP(created_at) as addTime,UNIX_TIMESTAMP(updated_at) as updateTime,is_region as parentId ,'kz' as language from rubric where need_id is null;";
                oldCategoryList = oldDbConnection.Query<Articlecategory>(querySql).ToList();
                querySql =
                    @"select rubric_id as  oldId, `rubric_name_ru` as title, rubric_meta_description_ru as shortDescription, rubric_url_ru as oldLatynUrl, sort_num as displayOrder, 
is_show_main as isShowMain, is_show_footer as isShowFooter, is_show as isHidden,
UNIX_TIMESTAMP(created_at) as addTime,UNIX_TIMESTAMP(updated_at) as updateTime,is_region as parentId ,'ru' as language from rubric where need_id is null;";
                oldCategoryList.AddRange(oldDbConnection.Query<Articlecategory>(querySql).ToList());
                querySql =
                    @"select rubric_id as  oldId, `rubric_name_en` as title, rubric_meta_description_en as shortDescription, rubric_url_en as oldLatynUrl, sort_num as displayOrder, 
is_show_main as isShowMain, is_show_footer as isShowFooter, is_show as isHidden,
UNIX_TIMESTAMP(created_at) as addTime,UNIX_TIMESTAMP(updated_at) as updateTime,is_region as parentId ,'en' as language from rubric where need_id is null;";
                oldCategoryList.AddRange(oldDbConnection.Query<Articlecategory>(querySql).ToList());

                #endregion

                if (oldCategoryList == null || oldCategoryList.Count == 0) return;

                #region Insert New Database

                using (var connection = Utilities.GetOpenConnection())
                {
                    foreach (var oldCategory in oldCategoryList)
                    {
                        int? res = 0;
                        using var tran = connection.BeginTransaction();
                        try
                        {
                            if (string.IsNullOrEmpty(oldCategory.Title)) continue;

                            if (string.IsNullOrEmpty(oldCategory.LatynUrl))
                                oldCategory.LatynUrl = QarBaseController.GetDistinctLatynUrl(connection,
                                    nameof(Articlecategory), oldCategory.LatynUrl, oldCategory.Title, 0, "");

                            oldCategory.Title = oldCategory.Title.Length > 255
                                ? oldCategory.Title.Substring(0, 250) + "..."
                                : oldCategory.Title;
                            // if(oldCategory.ParentId ==1)
                            // {
                            //     switch (oldCategory.Language)
                            //     {
                            //         case "kz":
                            //             oldCategory.ParentId = oldCategoryList.FirstOrDefault(x=>x.Title.Equals("Барлық аймақтар")).Id;
                            //             break;
                            //         case "ru":
                            //             oldCategory.ParentId = oldCategoryList.FirstOrDefault(x=>x.Title.Equals("Все регионы")).Id;
                            //             break;
                            //         case "en":
                            //             oldCategory.ParentId = oldCategoryList.FirstOrDefault(x=>x.Title.Equals("All regions")).Id;
                            //             break;
                            //     }
                            // }
                            res = connection.Insert(new Articlecategory
                            {
                                Title = oldCategory.Title ?? string.Empty,
                                LatynUrl = oldCategory.LatynUrl ?? string.Empty,
                                OldLatynUrl = oldCategory.OldLatynUrl ?? string.Empty,
                                ParentId = oldCategory.ParentId,
                                DisplayOrder = oldCategory.DisplayOrder,
                                BlockType = oldCategory.BlockType ?? string.Empty,
                                IsHidden = (byte)(oldCategory.IsHidden == 1 ? 0 : 1),
                                ShortDescription = oldCategory.ShortDescription ?? string.Empty,
                                AddTime = oldCategory.AddTime,
                                IsShowMain = oldCategory.IsShowMain,
                                Language = oldCategory.Language,
                                UpdateTime = oldCategory.UpdateTime,
                                QStatus = 0
                            });

                            #endregion

                            tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, key);
                            tran.Rollback();
                        }
                    }
                }

                #endregion
            }).Wait();
        }

        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Курс мәнін сақтау +JobSaveCurrencyRate()

    public void JobSaveCurrencyRate()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            using (var connection = Utilities.GetOpenConnection())
            {
                var currencyList = connection.GetList<Currency>("where qStatus = 0").ToList();
                var currentTime = UnixTimeHelper.GetCurrentUnixTime();
                currencyList = HtmlAgilityPackHelper.GetMig_kzCurrencyRateList(currencyList);
                foreach (var currency in currencyList)
                {
                    currency.UpdateTime = currentTime;
                    connection.Update(currency);
                }
            }

            QarCache.ClearCache(_memoryCache, nameof(QarCache.GetCurrencyList));
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Job Generate Tag LatynUrl +JobGenerateTagLatynUrl()

    public void JobGenerateTagLatynUrl()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(() =>
            {
                var takeCount = 1000;

                using var connection = Utilities.GetOpenConnection();
                var querySql = "where latynUrl = '' order by id asc limit @takeCount";
                object queryObj = new { takeCount };

                var tagList = connection.GetList<Tag>(querySql, queryObj).ToList();

                if (tagList.Count == 0) return;

                var sql = string.Empty;

                foreach (var tag in tagList)
                {
                    var url = QarBaseController.GetDistinctLatynUrl(connection, nameof(Tag), "",
                        tag.Title, tag.Id, "");

                    sql += $"update tag set latynUrl = '{url}' where id = {tag.Id};";

                    if (sql.Length > 600)
                    {
                        connection.Execute(sql);
                        sql = string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(sql))
                {
                    connection.Execute(sql);
                    sql = string.Empty;
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobGenerateTagLatynUrl(), TimeSpan.FromSeconds(5));
        }
    }

    #endregion


    #region Sitemap ты сақтау +JobSaveSiteMap()

    public void JobSaveSiteMap()
    {
        var key = MethodBase.GetCurrentMethod().Name;
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            var pageIndex = 1;
            var pageSize = 1000;
            var webRoot = _environment.WebRootPath;
            var siteMapFullPath = webRoot + "/sitemap.xml";
            if (File.Exists(siteMapFullPath)) File.Delete(siteMapFullPath);
            using var connection = Utilities.GetOpenConnection();
            while (true)
            {
                var articleList = connection
                    .GetList<Article>("where qStatus = 0 and id >= @minId and id < @maxId order by Id",
                        new { minId = (pageIndex - 1) * pageSize, maxId = pageIndex * pageSize }).Select(x =>
                        new Article
                        {
                            Id = x.Id,
                            QStatus = x.QStatus,
                            UpdateTime = x.UpdateTime,
                            LatynUrl = x.LatynUrl,
                            ThumbnailUrl = x.ThumbnailUrl,
                            Title = x.Title
                        }).ToList();

                if (articleList == null || articleList.Count == 0) break;
                var articleSiteMapPath = $"article-sitemap{pageIndex++}.xml";
                var articleSiteMapFullPath = webRoot + "/sitemap/xml/" + articleSiteMapPath;
                if (File.Exists(articleSiteMapFullPath)) File.Delete(articleSiteMapFullPath);
                var articleSiteMapDoc = SiteMapHalper.LoadXml(articleSiteMapFullPath, SiteMapType.ArticleSiteMap);
                var lastTime = string.Empty;
                foreach (var item in articleList)
                {
                    if (item.QStatus == 0)
                    {
                        SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                            $"{QarSingleton.GetInstance().GetSiteUrl()}/kz/article/" + item.LatynUrl + ".html",
                            UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                            $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                            item.ThumbnailUrl.Replace("_small", "_big"), item.Title);
                        SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                            $"{QarSingleton.GetInstance().GetSiteUrl()}/latyn/article/" + item.LatynUrl + ".html",
                            UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                            $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                            item.ThumbnailUrl.Replace("_small", "_big"), Cyrl2ToteHelper.Cyrl2Tote(item.Title));
                        SiteMapHalper.AddOrUpdateArticleLinkToSiteMapXml(articleSiteMapDoc,
                            $"{QarSingleton.GetInstance().GetSiteUrl()}/tote/article/" + item.LatynUrl + ".html",
                            UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime), "weekly", "0.7",
                            $"{QarSingleton.GetInstance().GetSiteUrl()}" +
                            item.ThumbnailUrl.Replace("_small", "_big"), Cyrl2LatynHelper.Cyrl2Latyn(item.Title));
                    }

                    lastTime = UnixTimeHelper.AstanaUnixTimeToString(item.UpdateTime);
                }

                SiteMapHalper.SaveXml(articleSiteMapDoc, articleSiteMapFullPath);
                var siteMapDoc = SiteMapHalper.LoadXml(siteMapFullPath, SiteMapType.SiteMap);
                SiteMapHalper.AddOrUpdateSiteMapXml(siteMapDoc,
                    $"{QarSingleton.GetInstance().GetSiteUrl()}/sitemap/xml/" + articleSiteMapPath, lastTime);
                SiteMapHalper.SaveXml(siteMapDoc, siteMapFullPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, key);
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectArticleMedia()

    public void JobSyncCollectArticleMedia()
    {
        var key = "JobSyncCollectArticleMedia";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                var takeCount = 500;
                using var connection = Utilities.GetOpenConnection();
                var total = connection.RecordCount<Article>("where qStatus <> 1");
                var leftCount = connection.RecordCount<Article>("where qStatus = 5");
                var articleList = connection
                    .GetList<Article>("where qStatus = 5 order by id desc limit @takeCount ", new { takeCount })
                    .ToList();
                foreach (var article in articleList)
                {
                    var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                    var saveDirectoryPath = _environment.WebRootPath;
                    var newArticle =
                        await HtmlAgilityPackHelper.DownloadArticleMedia(article, baseUrl, saveDirectoryPath);
                    if (newArticle == null) continue;
                    // newArticle.UpdateTime = UnixTimeHelper.GetCurrentUnixTime()
                    newArticle.QStatus = 0;
                    connection.Update(newArticle);
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectArticleMedia");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectArticleMedia(), TimeSpan.FromSeconds(1));
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectOldServerArticleMedia()

    public void JobSyncCollectOldServerArticleMedia()
    {
        var key = "JobSyncCollectOldServerArticleMedia";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                var takeCount = 100;
                var maxId = QarSingleton.GetInstance().GetIntValue("maxArticleId");
                var articleList = new List<Article>();

                using (var connection = Utilities.GetOldServerDbConnection())
                {
                    articleList = connection
                        .GetList<Article>("where id <= @maxId and qStatus <> 1 order by id desc limit @takeCount ",
                            new { takeCount, maxId }).ToList();
                }

                QarSingleton.GetInstance().SetIntValue("maxArticleId", maxId - articleList.Count);

                var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                var saveDirectoryPath = _environment.WebRootPath;

                foreach (var article in articleList)
                    await HtmlAgilityPackHelper.DownloadArticleMedia(article, baseUrl, saveDirectoryPath);
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectOldServerArticleMedia");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectOldServerArticleMedia(), TimeSpan.FromSeconds(60));
        }
    }

    #endregion

    #region Sync Collect Article Media +JobSyncCollectAdminAvatar()

    public void JobSyncCollectAdminAvatar()
    {
        var key = "JobSyncCollectAdminAvatar";
        if (QarSingleton.GetInstance().GetRunStatus(key)) return;
        QarSingleton.GetInstance().SetRunStatus(key, true);
        try
        {
            Task.Run(async () =>
            {
                using var connection = Utilities.GetOpenConnection();
                var adminList = connection.GetList<Admin>("where qStatus <> 1 ").ToList();
                foreach (var admin in adminList)
                {
                    var baseUrl = QarSingleton.GetInstance().GetSiteUrl();
                    var saveDirectoryPath = _environment.WebRootPath;

                    var newAdmin =
                        await HtmlAgilityPackHelper.DownloadAdminAvatar(admin, baseUrl, saveDirectoryPath);
                    if (newAdmin == null) continue;
                    newAdmin.QStatus = 0;
                    connection.Update(newAdmin);
                }
            }).Wait();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "JobSyncCollectAdminAvatar");
        }
        finally
        {
            QarSingleton.GetInstance().SetRunStatus(key, false);
            BackgroundJob.Schedule<QarJob>(q => q.JobSyncCollectAdminAvatar(), TimeSpan.FromMinutes(1));
        }
    }

    #endregion
}