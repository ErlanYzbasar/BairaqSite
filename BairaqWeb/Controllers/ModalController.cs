using BairaqWeb.Attributes;
using COMMON;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Authorization;
using MODEL;
using MODEL.FormatModels;
using SkiaSharp;

namespace BairaqWeb.Controllers;

[NoRole]
[Authorize(Roles = "Admin")]
public class ModalController : QarBaseController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public ModalController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    [AllowAnonymous]
    public IActionResult NewsInput()
    {
        return View($"~/Views/Themes/{CurrentTheme}/{ControllerName}/{ActionName}.cshtml");
    }

    public IActionResult DateTimePicker()
    {
        ViewData["elementId"] = GetStringQueryParam("elementId", string.Empty);
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #region Admin Relogin +Relogin()

    public IActionResult Relogin()
    {
        ViewData["reloginReason"] = GetStringQueryParam("reloginReason");
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Upload Avatar +UploadAvatar()

    public IActionResult UploadAvatar()
    {
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [Authorize(Roles = "Admin,User")]
    [HttpPost]
    public IActionResult UploadAvatar(IFormFile fileAvatar, string cropInfoStr)
    {
        if (fileAvatar == null)
            return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", null);
        if (!fileAvatar.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                fileAvatar.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);

        if (string.IsNullOrEmpty(cropInfoStr))
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);
        var cropInfoModel = JsonHelper.DeserializeObject<CropInfoModel>(cropInfoStr);
        if (cropInfoModel == null)
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);
        
        var tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var fileFormat = Path.GetExtension(fileAvatar.FileName).ToLower();
        if (fileFormat.Equals(".svg"))
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);
        var webRoot = _environment.WebRootPath + "/uploads/avatar/";
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using var stream = new MemoryStream();
        fileAvatar.CopyTo(stream);
        var fileBytes = stream.ToArray();
        var uploadImage = SKBitmap.Decode(fileBytes);
        var minWidth = 100;
        var minHeight = 100;
        if (uploadImage.Width < minWidth * 2 || uploadImage.Height < minHeight * 2)
            return MessageHelper.RedirectAjax(
                T("ls_Puaiwewhp")
                    .Replace("{width}", (2 * minWidth).ToString()).Replace("{height}", (2 * minHeight).ToString()),
                "error", "", null);
        if (fileFormat.Equals(".jpeg")) fileFormat = ".jpg";
        var path = ImgHandler.CutImage(webRoot, tempKey + fileFormat, stream, Convert.ToInt32(cropInfoModel.X),
            Convert.ToInt32(cropInfoModel.Y), Convert.ToInt32(cropInfoModel.Width),
            Convert.ToInt32(cropInfoModel.Height), cropInfoModel.Rotate, minWidth, minHeight);
        var relativePath = "/uploads/avatar/" + tempKey + "_big" + ".webp";
        relativePath = relativePath.Replace("_big.", "_small.");
        using (var connection = Utilities.GetOpenConnection())
        {
            if (HttpContext.User.Identity.Role().Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminId = GetAdminId();
                var admin = connection.GetList<Admin>("where qStatus = 0 and id = @adminId", new { adminId })
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(admin.AvatarUrl))
                {
                    if (System.IO.File.Exists(_environment.WebRootPath + admin.AvatarUrl))
                        System.IO.File.Delete(_environment.WebRootPath + admin.AvatarUrl);

                    if (System.IO.File.Exists(_environment.WebRootPath +
                                              admin.AvatarUrl.Replace("_small.", "_middle.")))
                        System.IO.File.Delete(_environment.WebRootPath +
                                              admin.AvatarUrl.Replace("_small.", "_middle."));

                    if (System.IO.File.Exists(
                            _environment.WebRootPath + admin.AvatarUrl.Replace("_small.", "_big.")))
                        System.IO.File.Delete(
                            _environment.WebRootPath + admin.AvatarUrl.Replace("_small.", "_big."));
                }

                admin.AvatarUrl = relativePath;
                admin.UpdateTime = UnixTimeHelper.GetCurrentUnixTime();
                if (connection.Update(admin) > 0)
                {
                    var roleIds = HttpContext.User.Identity.RoleIds();
                    var roleNames = HttpContext.User.Identity.RoleNames();
                    SaveLoginInfoToCookie(admin.Email, admin.Name, admin.Id, roleIds, roleNames, admin.IsSuper == 1,
                        admin.AvatarUrl, admin.SkinName);
                    return MessageHelper.RedirectAjax(T("ls_Uploadedsuccessfully"), "success", "",
                        relativePath.Replace("_small.", "_middle."));
                }
            }
            else if (HttpContext.User.Identity.Role().Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                //Save to User Avatar
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", relativePath);
    }

    #endregion

    #region Upload Thumbnail +UploadThumbnail()

    [Authorize(Roles = "Admin")]
    public IActionResult UploadThumbnail()
    {
        var ratioStr = GetStringQueryParam("ratio", "1/1");
        if (string.IsNullOrEmpty(ratioStr) || ratioStr.Split("/").Length != 2 ||
            !int.TryParse(ratioStr.Split("/")[0], out var firstNumber) ||
            !int.TryParse(ratioStr.Split("/")[1], out var secondNumber))
        {
            ViewData["message"] = T("ls_Managetypeerror") + "(Ratio Format)";
            var errorModal = "~/Views/Modal/Error.cshtml";
            return View($"{errorModal}");
        }

        ViewData["ratio"] = $"{firstNumber} / {secondNumber}";
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult UploadThumbnail(IFormFile fileThumbnail, string cropInfoStr, string aspectRatio)
    {
        if (fileThumbnail == null)
            return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", null);
        if (!fileThumbnail.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                fileThumbnail.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);

        if (string.IsNullOrEmpty(cropInfoStr))
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);
        var cropInfoModel = JsonHelper.DeserializeObject<CropInfoModel>(cropInfoStr);
        if (cropInfoModel == null)
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);

        var tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var fileFormat = Path.GetExtension(fileThumbnail.FileName).ToLower();
        var webRoot = _environment.WebRootPath + "/uploads/thumbnail/";
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using var stream = new MemoryStream();
        fileThumbnail.CopyTo(stream);
        var fileBytes = stream.ToArray();
        var uploadImage = SKBitmap.Decode(fileBytes);
        var minWidth = 300;
        var minHeight = 300;
        if (!string.IsNullOrEmpty(aspectRatio))
        {
            var arr = aspectRatio.Split("/");
            int rWidth, rHeight;
            if (arr.Length == 2 && int.TryParse(arr[0], out rWidth) && int.TryParse(arr[1], out rHeight))
            {
                if (rWidth / rHeight > 0)
                    minHeight = minWidth * rHeight / rWidth;
                else
                    minWidth = minHeight * rWidth / rHeight;
            }
        }

        if (uploadImage.Width < minWidth || uploadImage.Height < minHeight)
            return MessageHelper.RedirectAjax(
                T("ls_Puaiwewhp").Replace("{width}", minWidth.ToString())
                    .Replace("{height}", minHeight.ToString()), "error", "", null);
        if (fileFormat.Equals(".jpeg")) fileFormat = ".jpg";
        var path = ImgHandler.CutImage(webRoot, tempKey + fileFormat, stream, Convert.ToInt32(cropInfoModel.X),
            Convert.ToInt32(cropInfoModel.Y), Convert.ToInt32(cropInfoModel.Width),
            Convert.ToInt32(cropInfoModel.Height), cropInfoModel.Rotate, minWidth, minHeight);
        var relativePath = "/uploads/thumbnail/" + tempKey + "_big.webp";
        relativePath = relativePath.Replace("_big.", "_small.");
        using (var connection = Utilities.GetOpenConnection())
        {
            connection.Insert(new Mediainfo
            {
                Path = relativePath,
                Format = fileFormat,
                Length = fileThumbnail.Length.ToString(),
                UseCount = 0,
                AddTime = currentTime,
                UpdateTime = currentTime,
                QStatus = 0
            });
        }

        return MessageHelper.RedirectAjax(T("ls_Uploadedsuccessfully"), "success", "",
            relativePath.Replace("_small.", "_big."));
    }

    #endregion

    #region Upload Article Thumbnail +UploadArticleThumbnail()

    [Authorize(Roles = "Admin")]
    public IActionResult UploadArticleThumbnail()
    {
        var ratioStr = GetStringQueryParam("ratio", "1/1");
        if (string.IsNullOrEmpty(ratioStr) || ratioStr.Split("/").Length != 2 ||
            !int.TryParse(ratioStr.Split("/")[0], out var firstNumber) ||
            !int.TryParse(ratioStr.Split("/")[1], out var secondNumber))
        {
            ViewData["message"] = T("ls_Managetypeerror") + "(Ratio Format)";
            var errorModal = "~/Views/Console/Modal/Error.cshtml";
            return View($"{errorModal}");
        }

        ViewData["ratio"] = $"{firstNumber} / {secondNumber}";
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult UploadArticleThumbnail(IFormFile fileThumbnail, string cropInfoStr, string aspectRatio)
    {
        if (fileThumbnail == null)
            return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", null);
        if (!fileThumbnail.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                fileThumbnail.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);

        if (string.IsNullOrEmpty(cropInfoStr))
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);
        var cropInfoModel = JsonHelper.DeserializeObject<CropInfoModel>(cropInfoStr);
        if (cropInfoModel == null)
            return MessageHelper.RedirectAjax(T("ls_Tiiii"), "error", "", null);

        var tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var fileFormat = Path.GetExtension(fileThumbnail.FileName).ToLower();
        var webRoot = _environment.WebRootPath + "/uploads/thumbnail/";
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using var stream = new MemoryStream();
        fileThumbnail.CopyTo(stream);
        var fileBytes = stream.ToArray();
        var uploadImage = SKBitmap.Decode(fileBytes);
        var minWidth = 150;
        var minHeight = 150;
        if (!string.IsNullOrEmpty(aspectRatio))
        {
            var arr = aspectRatio.Split("/");
            int rWidth, rHeight;
            if (arr.Length == 2 && int.TryParse(arr[0], out rWidth) && int.TryParse(arr[1], out rHeight))
            {
                if (rWidth / rHeight > 0)
                    minHeight = minWidth * rHeight / rWidth;
                else
                    minWidth = minHeight * rWidth / rHeight;
            }
        }

        if (uploadImage.Width < minWidth * 2 || uploadImage.Height < minHeight * 2)
            return MessageHelper.RedirectAjax(
                T("ls_Puaiwewhp")
                    .Replace("{width}", (2 * minWidth).ToString()).Replace("{height}", (2 * minHeight).ToString()),
                "error", "", null);

        if (fileFormat.Equals(".jpeg")) fileFormat = ".jpg";
        var path = ImgHandler.CutImage(webRoot, tempKey + fileFormat, stream, Convert.ToInt32(cropInfoModel.X),
            Convert.ToInt32(cropInfoModel.Y), Convert.ToInt32(cropInfoModel.Width),
            Convert.ToInt32(cropInfoModel.Height), cropInfoModel.Rotate, minWidth, minHeight);
        var relativePath = "/uploads/thumbnail/" + tempKey + "_big.webp";
        relativePath = relativePath.Replace("_big.", "_small.");
        using (var connection = Utilities.GetOpenConnection())
        {
            connection.Insert(new Mediainfo
            {
                Path = relativePath,
                Format = fileFormat,
                Length = fileThumbnail.Length.ToString(),
                UseCount = 0,
                AddTime = currentTime,
                UpdateTime = currentTime,
                QStatus = 0
            });
        }

        return MessageHelper.RedirectAjax(T("ls_Uploadedsuccessfully"), "success", "",
            relativePath.Replace("_small.", "_big."));
    }

    #endregion

    #region Upload Editor Image +UploadEditorImage()

    public IActionResult UploadEditorImage()
    {
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [HttpPost]
    public IActionResult UploadEditorImage(IFormFile imgFile, string alt, string copyright)
    {
        if (imgFile == null)
            return MessageHelper.RedirectAjax(T("ls_Chooseaimage"), "error", "", null);

        if (!imgFile.ContentType.Contains("image") || !ImageFileExtensions.Any(item =>
                imgFile.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase)))
            return MessageHelper.RedirectAjax(T("ls_Tiiions"), "error", "", null);

        var tempKey = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var fileFormat = Path.GetExtension(imgFile.FileName).ToLower();
        var webRoot = _environment.WebRootPath;
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var relativePath = "/uploads/images/" + tempKey + fileFormat;
        var absolutePath = webRoot + relativePath;
        FileHelper.EnsureDir(absolutePath);
        using (var stream = System.IO.File.OpenWrite(absolutePath))
        {
            imgFile.CopyTo(stream);
        }

        using (var connection = Utilities.GetOpenConnection())
        {
            connection.Insert(new Mediainfo
            {
                Path = relativePath,
                Format = fileFormat,
                Length = imgFile.Length.ToString(),
                UseCount = 0,
                AddTime = currentTime,
                UpdateTime = currentTime,
                QStatus = 0
            });
        }

        alt ??= string.Empty;
        copyright ??= string.Empty;

        return MessageHelper.RedirectAjax(T("ls_Uploadedsuccessfully"), "success", "",
            new { relativePath, alt, copyright });
    }

    #endregion

    #region Editor-ға audio кіргізу +UploadEditorFile()

    public IActionResult UploadEditorFile()
    {
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
    public IActionResult UploadEditorFile(IFormFile file)
    {
        if (file == null)
            return MessageHelper.RedirectAjax("Аудио құжатын таңдаңыз! ", "error", "", null);

        if (!(file.ContentType.Contains("audio/mp3") || file.ContentType.Contains("audio/mpeg") ||
              file.ContentType.Contains("video/mp4") || file.ContentType.Contains("application/pdf")))
            return MessageHelper.RedirectAjax("mp3,mp4,mpeg,pdf түріндегі құжат таңдаңыз! ", "error", "", null);

        var fileFormat = Path.GetExtension(file.FileName).ToLower();

        if (fileFormat != ".mp3" && fileFormat != ".mpeg" && fileFormat != ".mp4" && fileFormat != ".pdf")
            return MessageHelper.RedirectAjax("mp3,mp4,mpeg,pdf түріндегі құжат таңдаңыз! ", "error", "", null);

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        var fileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileFormat;
        var relativePath = "/uploads/files/" + fileName;
        var absolutePath = _environment.WebRootPath + relativePath;
        if (!Directory.Exists(Path.GetDirectoryName(absolutePath)))
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        using (var fileStream = System.IO.File.OpenWrite(absolutePath))
        {
            file.CopyTo(fileStream);
            fileStream.Flush();
            using (var connection = Utilities.GetOpenConnection())
            {
                connection.Insert(new Mediainfo
                {
                    Path = relativePath,
                    Format = fileFormat,
                    Length = file.Length.ToString(),
                    UseCount = 0,
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 0
                });
            }
        }

        var resData = string.Empty;
        switch (fileFormat)
        {
            case ".mp3":
            case ".mpeg":
                {
                    resData = $"<audio controls><source src=\"{relativePath}\" type=\"audio/mp3\"></audio>";
                }
                break;
            case ".mp4":
                {
                    resData = $"<video controls width=\"250\"><source src=\"{relativePath}\" type=\"video/mp4\"></video>";
                }
                break;
            case ".pdf":
                {
                    resData = $"<a href=\"{relativePath}\" data-pdf=\"upload\" target=\"_blank\">{fileName}</a>";
                }
                break;
        }

        return MessageHelper.RedirectAjax(T("ls_Uploadedsuccessfully"), "success", "", resData);
    }

    #endregion

    #region Editor Social Embedcode +EditorSocialEmbedcode()

    public IActionResult EditorSocialEmbedcode()
    {
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    [HttpPost]
    public IActionResult EditorSocialEmbedcode(string embedCode)
    {
        if (string.IsNullOrWhiteSpace(embedCode))
            return MessageHelper.RedirectAjax(T("ls_Requiredfields"), "error", "", "embedCode");

        if (!HtmlAgilityPackHelper.CheckSocialEmbedCode(embedCode))
            return MessageHelper.RedirectAjax(T("ls_Teciions"), "error", "", "embedCode");

        if (!HtmlAgilityPackHelper.IsIframe(embedCode))
        {
            var relativePath = $"/uploads/iframe/{Guid.NewGuid().ToString("N")}.html";
            var absoludePath = _environment.WebRootPath + relativePath;
            if (!Directory.Exists(Path.GetDirectoryName(absoludePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(absoludePath));
                
            var htmlContent = @$"<!DOCTYPE html>
            <html lang=""kk"" style=""margin:0;padding:0;"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Embedded Content</title>
            <body style=""margin:0;padding:0;"">
                {embedCode}
            </body>
            </html>";

            System.IO.File.WriteAllText(absoludePath, htmlContent);
            embedCode =
                $"<iframe src=\"{relativePath}\" data-embed=\"social\" frameborder=\"0\" style=\"width:100%;min-height:200px;\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>\n";
        }
        return MessageHelper.RedirectAjax("Сәтті жолданды!", "success", "", embedCode);
    }

    #endregion
}