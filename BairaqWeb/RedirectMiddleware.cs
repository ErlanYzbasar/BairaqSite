using BairaqWeb.Caches;
using Dapper;
using DBHelper;
using MODEL;

namespace BairaqWeb
{
    public class RedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // var requestPath = context.Request.Path.Value ?? string.Empty;
            // if (requestPath.Length > 1)
            // {
            //     requestPath = requestPath[1..].ToLower(); //Remove first /
            //     if (requestPath.EndsWith(".css") || requestPath.EndsWith(".js") || requestPath.EndsWith(".png")
            //         || requestPath.EndsWith(".jpg") || requestPath.EndsWith(".jpeg") || requestPath.EndsWith(".pdf")
            //         || requestPath.EndsWith(".pdf") || requestPath.EndsWith(".txt") || requestPath.EndsWith(".xml"))
            //     {
            //         await _next(context);
            //         return;
            //     }
            //
            //     var pArr = requestPath.Split("/");
            //     if (pArr.Length > 0 && !pArr[0].Equals("kz", StringComparison.OrdinalIgnoreCase))
            //     {
            //         var memoryCache = context.RequestServices.GetService<IMemoryCache>();
            //         var categoryList = QarCache.GetCategoryList(memoryCache, "kz");
            //         if (categoryList.Any(x => x.OldLatynUrl.Equals(pArr[0], StringComparison.OrdinalIgnoreCase)))
            //         {
            //             context.Response.Redirect(
            //                 $"/kz/category/{categoryList.FirstOrDefault(x => x.OldLatynUrl.Equals(pArr[0], StringComparison.OrdinalIgnoreCase)).LatynUrl}.html",
            //                 permanent: true);
            //             return;
            //         }
            //
            //         using var connection = Utilities.GetOpenConnection();
            //         if (pArr[0].Equals("tag", StringComparison.OrdinalIgnoreCase))
            //         {
            //             var tag = connection
            //                 .GetList<Tag>("where oldLatynUrl = @oldLatynUrl", new { oldLatynUrl = pArr[1] })
            //                 .FirstOrDefault();
            //
            //             if (tag != null)
            //             {
            //                 context.Response.Redirect($"/kz/tag/{tag.LatynUrl}.html", permanent: true);
            //                 return;
            //             }
            //         }
            //
            //         var latynUrl = connection
            //             .Query<string>(
            //                 "select latynUrl from article where qStatus = 0 and oldLatynUrl = @oldLatynUrl ",
            //                 new { oldLatynUrl = pArr[0] }).FirstOrDefault();
            //         if (!string.IsNullOrEmpty(latynUrl))
            //         {
            //             context.Response.Redirect($"/kz/article/{latynUrl}.html", permanent: true);
            //             return;
            //         }
            //     }
            // }

            await _next(context);
        }
    }
}