using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MODEL;
using Serilog;
using SkiaSharp;

namespace COMMON;

public static class HtmlAgilityPackHelper
{
    private static readonly string[] Base64ImagePrefixs =
    {
        "data:image/png;base64,", "data:image/jpeg;base64,", "data:image/gif;base64,", "data:image/svg+xml;base64,",
        "data:image/bmp;base64,", "data:image/x-icon;base64,", "data:image/webp;base64,"
    };


    #region Convert Html +ConvertHtmlTextNode(string html, string language, string userAgent, string qUrl)

    public static string ConvertHtmlTextNode(string html, string language, string userAgent, string qUrl)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var htmlLangNode = document.DocumentNode.SelectSingleNode("/html");
        if (htmlLangNode != null)
        {
            htmlLangNode.SetAttributeValue("lang", "kk");
        }

        var staticTextNodes =
            document.DocumentNode.SelectNodes("//*[contains(@rel,'qar-static-text')]/text()[normalize-space(.) != '']");
        var scriptTextNodes = document.DocumentNode.SelectNodes("//script/text()[normalize-space(.) != '']");
        var textNodes = document.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']");
        if (textNodes != null)
        {
            foreach (var node in textNodes)
            {
                if (staticTextNodes != null && staticTextNodes.Contains(node)) continue;
                if (scriptTextNodes != null && scriptTextNodes.Contains(node)) continue;
                var innerHtml = WebUtility.HtmlDecode(node.InnerHtml);
                switch (language)
                {
                    case "tote":
                    {
                        node.InnerHtml = Cyrl2ToteHelper.Cyrl2Tote(innerHtml);
                    }
                        break;
                    case "latyn":
                    {
                        node.InnerHtml = Cyrl2LatynHelper.Cyrl2Latyn(innerHtml);
                    }
                        break;
                }
            }
        }

        var inputNodes = document.DocumentNode.SelectNodes("//input[contains(@type,'text')]|//textarea");
        if (inputNodes != null)
        {
            foreach (var node in inputNodes)
            {
                var placeholder = node.Attributes["placeholder"] != null
                    ? node.Attributes["placeholder"].Value
                    : string.Empty;
                if (string.IsNullOrEmpty(placeholder) ||
                    string.IsNullOrEmpty(placeholder = placeholder.Trim())) continue;
                switch (language)
                {
                    case "tote":
                    {
                        placeholder = Cyrl2ToteHelper.Cyrl2Tote(placeholder);
                    }
                        break;
                    case "latyn":
                    {
                        placeholder = Cyrl2LatynHelper.Cyrl2Latyn(placeholder);
                    }
                        break;
                }

                node.SetAttributeValue("placeholder", placeholder);
            }
        }

        var textareaNodes = document.DocumentNode.SelectNodes("//textarea");
        if (textareaNodes != null)
        {
            foreach (var node in textareaNodes)
            {
                var innerHtml = WebUtility.HtmlDecode(node.InnerHtml);
                switch (language)
                {
                    case "tote":
                    {
                        node.InnerHtml = Cyrl2ToteHelper.Cyrl2Tote(innerHtml);
                    }
                        break;
                    case "latyn":
                    {
                        node.InnerHtml = Cyrl2LatynHelper.Cyrl2Latyn(innerHtml);
                    }
                        break;
                }
            }
        }


        var metaNodes = document.DocumentNode.SelectNodes(
            @"//meta[contains(@name,'keywords')]|//meta[contains(@name,'description')]|//meta[contains(@name,'title')]|//meta[contains(@name,'site_name')]
                                                                                   |//meta[contains(@property,'description')]|//meta[contains(@property,'title')]|//meta[contains(@property,'site_name')]");
        if (metaNodes != null)
        {
            foreach (var node in metaNodes)
            {
                var content = node.Attributes["content"] != null ? node.Attributes["content"].Value : string.Empty;
                if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(content = content.Trim())) continue;
                switch (language)
                {
                    case "tote":
                    {
                        content = Cyrl2ToteHelper.Cyrl2Tote(content);
                    }
                        break;
                    case "latyn":
                    {
                        content = Cyrl2LatynHelper.Cyrl2Latyn(content);
                    }
                        break;
                }

                node.SetAttributeValue("content", content);
            }
        }

        var imgNodes = document.DocumentNode.SelectNodes(@"//img");
        if (imgNodes != null)
        {
            foreach (var node in imgNodes)
            {
                var alt = node.Attributes["alt"] != null ? node.Attributes["alt"].Value : string.Empty;
                var dataCopyright = node.Attributes["data-copyright"] != null
                    ? node.Attributes["data-copyright"].Value
                    : string.Empty;
                switch (language)
                {
                    case "tote":
                    {
                        alt = Cyrl2ToteHelper.Cyrl2Tote(alt);
                        dataCopyright = Cyrl2ToteHelper.Cyrl2Tote(dataCopyright);
                    }
                        break;
                    case "latyn":
                    {
                        alt = Cyrl2LatynHelper.Cyrl2Latyn(alt);
                        dataCopyright = Cyrl2LatynHelper.Cyrl2Latyn(dataCopyright);
                    }
                        break;
                }

                node.SetAttributeValue("alt", alt);
                if (!string.IsNullOrEmpty(dataCopyright))
                {
                    node.SetAttributeValue("data-copyright", dataCopyright);
                }
            }
        }


        var staticANodes = document.DocumentNode.SelectNodes("//a[contains(@rel,'ankui-static-text')]");
        var aNodes = document.DocumentNode.SelectNodes("//a");
        if (aNodes != null)
        {
            foreach (var node in aNodes)
            {
                if (staticANodes != null && staticANodes.Contains(node)) continue;
                var href = node.Attributes["href"] != null ? node.Attributes["href"].Value : string.Empty;
                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(href = href.Trim())) continue;
                if (href.Substring(0, 1).Equals("/"))
                {
                    node.SetAttributeValue("href", qUrl + href);
                }
            }
        }

        return document.DocumentNode.OuterHtml;
    }

    #endregion

    #region HTML ішіндегі Body-дың мазмұнын алу +GetHtmlBoyInnerHtml(string html)

    public static string GetHtmlBoyInnerHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var bodyNode = document.DocumentNode.SelectSingleNode("//body");
        if (bodyNode != null)
        {
            return bodyNode.InnerHtml;
        }

        return html;
    }

    #endregion

    #region Html ішніде ешқандай мазмұн жоқ?  +HtmlContentIsEmpty(string html)

    public static bool HtmlContentIsEmpty(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml("<body>" + html + "</body>");
        var bodyNode = document.DocumentNode.SelectSingleNode("//body");
        var innerText = bodyNode.InnerText;
        return string.IsNullOrEmpty(innerText) || string.IsNullOrEmpty(innerText.Trim());
    }

    #endregion

    #region Get Short Description +GetShortDescription(string fullDescription)

    public static string GetShortDescription(string fullDescription, int length = 200)
    {
        if (string.IsNullOrWhiteSpace(fullDescription)) return string.Empty;
        try
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(fullDescription);
            var shortNode = htmlDocument.DocumentNode.ChildNodes.FirstOrDefault();
            var shortDescription = WebUtility.HtmlDecode(shortNode?.InnerText ?? string.Empty).Trim();
            while (string.IsNullOrWhiteSpace(shortDescription) || shortDescription.Length < 20)
            {
                shortNode = shortNode?.NextSibling;
                if (shortNode == null) break;
                shortDescription = shortNode.InnerText ?? string.Empty;
                shortDescription = WebUtility.HtmlDecode(shortDescription).Trim();
            }

            if (shortDescription.Length > length)
            {
                shortDescription = shortDescription[..(length - 3)];
                var lastWhitespaceIndex = shortDescription.LastIndexOf(" ", StringComparison.Ordinal);
                if (lastWhitespaceIndex > 0)
                {
                    shortDescription = shortDescription[..lastWhitespaceIndex];
                }

                string[] symbols = { ",", "?", "!", ":", ".", " ", "\"", "%", "'" };
                if (symbols.Any(x => x.Equals(shortDescription[shortDescription.Length - 1])))
                {
                    shortDescription = shortDescription[..^2];
                }

                shortDescription += "...";
            }

            return shortDescription;
        }
        catch
        {
            return string.Empty;
        }
    }

    #endregion

    #region Get Image Path List +GetMediaPathList(string fullDescription)

    public static List<string> GetMediaPathList(string fullDescription)
    {
        var mediaPathList = new List<string>();
        if (string.IsNullOrWhiteSpace(fullDescription))
            return mediaPathList;
        try
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(fullDescription);

            var imgNodes = htmlDocument.DocumentNode.SelectNodes("//img");
            if (imgNodes != null)
            {
                foreach (var imgNode in imgNodes)
                {
                    var src = imgNode.Attributes["src"] != null ? imgNode.Attributes["src"].Value : string.Empty;
                    if (!string.IsNullOrEmpty(src))
                    {
                        mediaPathList.Add(src);
                    }
                }
            }

            var videoNodes = htmlDocument.DocumentNode.SelectNodes("//video");
            if (videoNodes != null)
            {
                foreach (var videoNode in videoNodes)
                {
                    var src = videoNode.Attributes["src"] != null ? videoNode.Attributes["src"].Value : string.Empty;
                    if (!string.IsNullOrEmpty(src))
                    {
                        mediaPathList.Add(src);
                    }

                    var videoNodeSourceNode = videoNode.SelectSingleNode("./source");
                    if (videoNodeSourceNode != null)
                    {
                        src = videoNodeSourceNode.Attributes["src"] != null
                            ? videoNodeSourceNode.Attributes["src"].Value
                            : string.Empty;
                        if (!string.IsNullOrEmpty(src))
                        {
                            mediaPathList.Add(src);
                        }
                    }
                }
            }

            var audioNodes = htmlDocument.DocumentNode.SelectNodes("//audio");
            if (audioNodes != null)
            {
                foreach (var audioNode in audioNodes)
                {
                    var src = audioNode.Attributes["src"] != null ? audioNode.Attributes["src"].Value : string.Empty;
                    if (!string.IsNullOrEmpty(src))
                    {
                        mediaPathList.Add(src);
                    }

                    var audioNodeSourceNode = audioNode.SelectSingleNode("./source");
                    if (audioNodeSourceNode != null)
                    {
                        src = audioNodeSourceNode.Attributes["src"] != null
                            ? audioNodeSourceNode.Attributes["src"].Value
                            : string.Empty;
                        if (!string.IsNullOrEmpty(src))
                        {
                            mediaPathList.Add(src);
                        }
                    }
                }
            }

            var pdfNodes = htmlDocument.DocumentNode.SelectNodes("//a[@data-pdf]");
            if (pdfNodes != null)
            {
                foreach (var pdfNode in pdfNodes)
                {
                    var href = pdfNode.Attributes["href"] != null ? pdfNode.Attributes["href"].Value : string.Empty;
                    if (!string.IsNullOrEmpty(href))
                    {
                        mediaPathList.Add(href);
                    }
                }
            }


            return mediaPathList;
        }
        catch
        {
            return mediaPathList;
        }
    }

    #endregion

    #region Is Iframe +IsIframe(string embedCode)

    public static bool IsIframe(string embedCode)
    {
        var document = new HtmlDocument();
        document.LoadHtml(embedCode);
        var iframeNode = document.DocumentNode.SelectSingleNode("//iframe");
        return iframeNode != null;
    }

    #endregion

    #region Check Social EmbedCode +CheckSocialEmbedCode(string embedCode)

    public static bool CheckSocialEmbedCode(string embedCode)
    {
        string[] allowDomains =
        {
            "telegram.org", "www.telegram.org", "instagram.com", "www.instagram.com", "facebook.com",
            "www.facebook.com", "youtube.com", "www.youtube.com", "tiktok.com", "www.tiktok.com","www.x.com","x.com","www.twitter.com","twitter.com","platform.twitter.com"
        };
        var document = new HtmlDocument();
        document.LoadHtml(embedCode);
        var scriptNodes = document.DocumentNode.SelectNodes("//script");
        if (scriptNodes is { Count: > 0 })
        {
            foreach (var scriptNode in scriptNodes)
            {
                var urlString = scriptNode.Attributes["src"] != null
                    ? scriptNode.Attributes["src"].Value
                    : string.Empty;
                urlString = urlString.StartsWith("//") ? $"https:{urlString}" : urlString;
                if (!urlString.StartsWith("http://") && !urlString.StartsWith("https://"))
                {
                    urlString = "https://" + urlString;
                }

                var uri = new Uri(urlString);
                var host = uri.Host.ToLower();
                if (!allowDomains.Contains(host))
                {
                    return false;
                }

                var scriptContent = scriptNode.InnerHtml ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(scriptContent))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Сайт html-ын алу +GetHtmlWebAsync(string url)

    private static async Task<string> GetHtmlWebAsync(string url)
    {
        var client = new HttpClient();
        using var response = await client.GetAsync(url);
        using var content = response.Content;
        return await content.ReadAsStringAsync();
    }

    #endregion

    #region Mig.kz тен қазіргі курсті оқу +GetMig_kzCurrencyRateList(List<Currency> currencyList)

    public static List<Currency> GetMig_kzCurrencyRateList(List<Currency> currencyList)
    {
        if (currencyList == null) return new List<Currency>();
        try
        {
            var html = GetHtmlWebAsync("https://mig.kz/").Result;
            var document = new HtmlDocument();
            document.LoadHtml(html);
            var trNodes = document.DocumentNode.SelectNodes("//div[contains(@class, 'informer')]//tr");
            if (trNodes is { Count: > 0 })
            {
                foreach (var trNode in trNodes)
                {
                    // var tdBuyNode = trNode.SelectSingleNode(".//td[contains(@class, 'buy')]");
                    var tdCurrencyNode = trNode.SelectSingleNode(".//td[contains(@class, 'currency')]");
                    var tdSellNode = trNode.SelectSingleNode(".//td[contains(@class, 'sell')]");
                    if (tdCurrencyNode != null && tdSellNode != null)
                    {
                        var title = tdCurrencyNode.InnerText ?? string.Empty;
                        var currentCurrency = currencyList.FirstOrDefault(x =>
                            x.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                        if (currentCurrency != null && decimal.TryParse(tdSellNode.InnerText, out var rate))
                        {
                            currentCurrency.Rate = Convert.ToUInt32(rate * currentCurrency.IntRatio);
                        }
                    }
                }
            }

            return currencyList;
        }
        catch
        {
            return currencyList;
        }
    }

    #endregion

    #region Download Newspaper Media And Save DownloadAdminAvatar(Admin admin, string siteUrl, string directoryPath)

    public static async Task<Admin> DownloadAdminAvatar(Admin admin, string siteUrl, string directoryPath)
    {
        if (admin == null) return null;

        var savedFilePathList = new List<string>();
        try
        {
            if (!string.IsNullOrWhiteSpace(admin.AvatarUrl))
            {
                try
                {
                    var size = string.Empty;
                    var fileName = Path.GetFileNameWithoutExtension(admin.AvatarUrl);

                    if (fileName.EndsWith("_big", StringComparison.OrdinalIgnoreCase)) size = "_big.";
                    else if (fileName.EndsWith("_middle", StringComparison.OrdinalIgnoreCase)) size = "_middle.";
                    else if (fileName.EndsWith("_small", StringComparison.OrdinalIgnoreCase)) size = "_small.";

                    if (!string.IsNullOrWhiteSpace(size))
                    {
                        var bigPath = PathHelper.Combine(directoryPath, admin.AvatarUrl.Replace(size, "_big."));
                        var middlePath = PathHelper.Combine(directoryPath, admin.AvatarUrl.Replace(size, "_middle."));
                        var smallPath = PathHelper.Combine(directoryPath, admin.AvatarUrl.Replace(size, "_small."));

                        savedFilePathList.Add(bigPath);
                        savedFilePathList.Add(middlePath);
                        savedFilePathList.Add(smallPath);

                        await DownloadFileAsync(GetFullUrl(admin.AvatarUrl.Replace(size, "_big."), siteUrl), bigPath);
                        await DownloadFileAsync(GetFullUrl(admin.AvatarUrl.Replace(size, "_middle."), siteUrl),
                            middlePath);
                        await DownloadFileAsync(GetFullUrl(admin.AvatarUrl.Replace(size, "_small."), siteUrl),
                            smallPath);
                    }
                    else
                    {
                        var absPath = PathHelper.Combine(directoryPath, admin.AvatarUrl);
                        savedFilePathList.Add(absPath);
                        await DownloadFileAsync(GetFullUrl(admin.AvatarUrl, siteUrl), absPath);
                    }
                }
                catch (Exception)
                {
                    admin.QStatus = 6;
                }
            }

            return admin;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DownloadAdminAvatar");
            foreach (var filePath in savedFilePathList)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            return null;
        }
        finally
        {
            if (admin.QStatus == 6)
            {
                foreach (var filePath in savedFilePathList)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }
    }

    #endregion

    #region Download Article Media And Save DownloadArticleMedia(Article article, string siteUrl, string directoryPath)

    public static async Task<Article> DownloadArticleMedia(Article article, string siteUrl, string directoryPath)
    {
        if (article == null) return null;

        var savedFilePathList = new List<string>();
        try
        {
            if (!string.IsNullOrWhiteSpace(article.ThumbnailUrl))
            {
                try
                {
                    var size = string.Empty;
                    var fileName = Path.GetFileNameWithoutExtension(article.ThumbnailUrl);

                    if (fileName.EndsWith("_big", StringComparison.OrdinalIgnoreCase)) size = "_big.";
                    else if (fileName.EndsWith("_middle", StringComparison.OrdinalIgnoreCase)) size = "_middle.";
                    else if (fileName.EndsWith("_small", StringComparison.OrdinalIgnoreCase)) size = "_small.";

                    if (!string.IsNullOrWhiteSpace(size))
                    {
                        var bigPath = PathHelper.Combine(directoryPath, article.ThumbnailUrl.Replace(size, "_big."));
                        var middlePath =
                            PathHelper.Combine(directoryPath, article.ThumbnailUrl.Replace(size, "_middle."));
                        var smallPath =
                            PathHelper.Combine(directoryPath, article.ThumbnailUrl.Replace(size, "_small."));

                        savedFilePathList.Add(bigPath);
                        savedFilePathList.Add(middlePath);
                        savedFilePathList.Add(smallPath);

                        await DownloadFileAsync(GetFullUrl(article.ThumbnailUrl.Replace(size, "_big."), siteUrl),
                            bigPath);
                        await DownloadFileAsync(GetFullUrl(article.ThumbnailUrl.Replace(size, "_middle."), siteUrl),
                            middlePath);
                        await DownloadFileAsync(GetFullUrl(article.ThumbnailUrl.Replace(size, "_small."), siteUrl),
                            smallPath);
                    }
                    else
                    {
                        var absPath = PathHelper.Combine(directoryPath, article.ThumbnailUrl);
                        savedFilePathList.Add(absPath);
                        await DownloadFileAsync(GetFullUrl(article.ThumbnailUrl, siteUrl), absPath);
                    }
                }
                catch (Exception)
                {
                    article.QStatus = 6;
                }
            }

            if (!string.IsNullOrWhiteSpace(article.FullDescription))
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(article.FullDescription);
                var imageNodes = htmlDocument.DocumentNode.SelectNodes("//img");
                if (imageNodes != null)
                {
                    foreach (var image in imageNodes)
                    {
                        var imageUrl = image?.Attributes["src"]?.Value ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                        if (Base64ImagePrefixs.Any(x => imageUrl.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                        {
                            var match = Regex.Match(imageUrl, @"^data:image\/(\w+);base64,");
                            if (match.Success)
                            {
                                var fileFormat = "." + match.Groups[1].Value;
                                var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}{fileFormat}";
                                var relativePath = $"/uploads/images/{fileName}";
                                var absPath = PathHelper.Combine(directoryPath, relativePath);
                                SaveImageFromBase64(imageUrl, absPath);
                                image?.SetAttributeValue("src", relativePath);
                                savedFilePathList.Add(absPath);
                            }
                        }
                        else
                        {
                            try
                            {
                                imageUrl = GetFullUrl(imageUrl, siteUrl);
                                var absPath = PathHelper.Combine(directoryPath, imageUrl.Replace(siteUrl, ""));
                                savedFilePathList.Add(absPath);
                                await DownloadFileAsync(imageUrl, absPath);
                            }
                            catch (Exception)
                            {
                                article.QStatus = 6;
                            }
                        }
                    }
                }

                var audioNodes = htmlDocument.DocumentNode.SelectNodes("//audio");
                if (audioNodes != null)
                {
                    foreach (var audio in audioNodes)
                    {
                        var audioUrl = audio?.Attributes["src"]?.Value ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(audioUrl))
                            continue;
                        try
                        {
                            var absPath = PathHelper.Combine(directoryPath, audioUrl);
                            await DownloadFileAsync(GetFullUrl(audioUrl, siteUrl), absPath);
                            savedFilePathList.Add(absPath);
                        }
                        catch (Exception)
                        {
                            article.QStatus = 6;
                        }
                    }
                }

                var videoNodes = htmlDocument.DocumentNode.SelectNodes("//video");
                if (videoNodes != null)
                {
                    foreach (var video in videoNodes)
                    {
                        var videoUrl = video?.Attributes["src"]?.Value ?? string.Empty;
                        if (string.IsNullOrEmpty(videoUrl))
                            continue;

                        try
                        {
                            var absPath = PathHelper.Combine(directoryPath, videoUrl);
                            await DownloadFileAsync(GetFullUrl(videoUrl, siteUrl), absPath);
                            savedFilePathList.Add(absPath);
                        }
                        catch (Exception)
                        {
                            article.QStatus = 6;
                        }
                    }
                }

                var iframeNodes = htmlDocument.DocumentNode.SelectNodes("//iframe");
                if (iframeNodes != null)
                {
                    foreach (var iframe in iframeNodes)
                    {
                        var iframeUrl = iframe?.Attributes["src"]?.Value ?? string.Empty;
                        if (string.IsNullOrEmpty(iframeUrl))
                            continue;

                        if (iframeUrl.StartsWith("/uploads/iframe/") &&
                            iframeUrl.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var absPath = PathHelper.Combine(directoryPath, iframeUrl);
                                await DownloadFileAsync(GetFullUrl(iframeUrl, siteUrl), absPath);
                                savedFilePathList.Add(absPath);
                            }
                            catch (Exception)
                            {
                                article.QStatus = 6;
                            }
                        }
                    }
                }
            }

            return article;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DownloadArticleMedia");
            foreach (var filePath in savedFilePathList.Where(File.Exists))
            {
                File.Delete(filePath);
            }

            return null;
        }
        finally
        {
            if (article.QStatus == 6)
            {
                foreach (var filePath in savedFilePathList.Where(File.Exists))
                {
                    File.Delete(filePath);
                }
            }
        }
    }

    #endregion

    #region Download Image +DownloadFileAsync(string imageUrl, string savePath)

    private static async Task DownloadFileAsync(string imageUrl, string savePath)
    {
        if (savePath.Contains("http", StringComparison.OrdinalIgnoreCase))
            return;
        if (File.Exists(savePath))
            return;
        if (!Directory.Exists(Path.GetDirectoryName(savePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? string.Empty);
        }

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode();

        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        await File.WriteAllBytesAsync(savePath, imageBytes);
    }

    #endregion

    #region Get Full Url +GetFullUrl(string url, string siteUrl)

    private static string GetFullUrl(string url, string siteUrl)
    {
        url = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : siteUrl + url;
        var uArr = url.Split("://");
        var newUrl = "";
        for (var i = 0; i < uArr.Length; i++)
        {
            newUrl += uArr[i].Replace("//", "/") + (i == uArr.Length - 1 ? "" : "://");
        }

        return newUrl;
    }

    #endregion

    #region Save Image From Base64 +SaveImageFromBase64(string base64String, string filePath)

    private static void SaveImageFromBase64(string base64String, string filePath)
    {
        // Remove the prefix "data:image/png;base64," if it exists
        foreach (var base64ImagePrefix in Base64ImagePrefixs)
        {
            base64String = base64String.Replace(base64ImagePrefix, "");
        }

        // Convert Base64 String to byte[]
        var imageBytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(imageBytes);
        // Decode the byte[] into a SKBitmap
        using var bitmap = SKBitmap.Decode(ms);
        // Encode the SKBitmap into a SKData object
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        // Save the SKData object to a file
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }

    #endregion

    #region Get Youtube Url +GetYoutubeUrl(string url)

    public static string GetYoutubeUrl(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var iframeNode = doc.DocumentNode.SelectSingleNode("//iframe");
        return iframeNode != null ? iframeNode.GetAttributeValue("src", string.Empty) : string.Empty;
    }

    #endregion
}