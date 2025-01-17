using BairaqWeb.Caches;

namespace BairaqWeb.Controllers;

[ApiController]
[Route("api/[action]")]
public class APIController : QarApiBaseController
{
    private readonly IMemoryCache _memoryCache;

    public APIController(IMemoryCache memoryCache, IConfiguration configuration) : base(memoryCache, configuration)
    {
        _memoryCache = memoryCache;
    }

    [HttpPost]
    public IActionResult Index()
    {
        var pinnedArticle = QarCache.GetPinnedArticleList(_memoryCache, CurrentLanguage, 1);
        var latestArticleList = QarCache.GetArticleList(_memoryCache, CurrentLanguage, 25);
        var popularTagList = QarCache.GetPopularTagList(_memoryCache, CurrentLanguage, 10);
        var featuredArticleList = QarCache.GetFeaturedArticleList(_memoryCache, CurrentLanguage, 3);
        var categoryList = QarCache.GetCategoryList(_memoryCache, CurrentLanguage);
        var regionArticleList = QarCache.GetRegionArticleList(_memoryCache, CurrentLanguage, 6);
        var focusArticleList = QarCache.GetFocusArticleList(_memoryCache, CurrentLanguage, 2);
        var topArticleList = QarCache.GetTopArticleList(_memoryCache, CurrentLanguage, 100, 25);

        var response = new
        {
            PinnedArticle = pinnedArticle,
            LatestArticleList = latestArticleList,
            PopularTagList = popularTagList,
            FeaturedArticleList = featuredArticleList,
            RegionArticleList = regionArticleList,
            FocusArticleList = focusArticleList,
            TopArticleList = topArticleList,
            Categories = categoryList.Select(category =>
            {
                var takeCount = category.BlockType switch
                {
                    "block1" => 8,
                    "block2" => 5,
                    "block3" => 5,
                    "block4" => 5,
                    _ => 4
                };

                return new
                {
                    BlockType = category.BlockType,
                    Url = $"/{CurrentLanguage}/category/{category.LatynUrl}",
                    Title = category.Title,
                    ArticleList = QarCache.GetArticleList(_memoryCache, CurrentLanguage, takeCount, category.Id)
                };
            })
        };

        return Ok(response);
    }
}