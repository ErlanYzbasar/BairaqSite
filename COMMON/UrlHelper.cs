using System.Web;

namespace COMMON;

public static class UrlHelper
{
    public static string AddParam(string url, string key, string value)
    {
        try
        {
            if (url.StartsWith('/'))
            {
                var uri = new Uri(QarSingleton.GetInstance().GetSiteUrl() + url);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                queryParams[key] = value;
                var query = queryParams.ToString();
                return uri.AbsolutePath + (string.IsNullOrEmpty(query) ? "" : "?" + query);
            }
            else
            {
                var uriBuilder = new UriBuilder(new Uri(url));
                var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
                queryParams[key] = value;
                uriBuilder.Query = queryParams.ToString();
                return uriBuilder.ToString();
            }
        }
        catch
        {
            return url;
        }
    }
}