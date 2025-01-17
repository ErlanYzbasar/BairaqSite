using BairaqWeb.Attributes;
using BairaqWeb.Caches;
using COMMON;
using Dapper;
using DBHelper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using MODEL;
using MODEL.FormatModels;
using Serilog;

namespace BairaqWeb.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : QarBaseController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public AdminController(IMemoryCache memoryCache, IWebHostEnvironment environment) : base(memoryCache, environment)
    {
        _memoryCache = memoryCache;
        _environment = environment;
    }

    private static void AddNavigation(Navigation item)
    {
        using var connection = Utilities.GetOpenConnection();
        connection.Insert(item);
    }

    private static void AddPermission(Permission item)
    {
        using var connection = Utilities.GetOpenConnection();
        connection.Insert(item);
    }


    #region Login +Login()

    [AllowAnonymous]
    public IActionResult Login()
    {
        // var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        // AddNavigation(new Navigation
        // {
        //     NavigationTypeId = 1,
        //     ParentId = 1, //Catalog
        //     NavTitle = "ls_Proverb",
        //     NavUrl = "/catalog/proverb/list",
        //     Target = "_self",
        //     HasIcon = 0,
        //     Icon = "",
        //     Description = "",
        //     AddTime = currentTime,
        //     UpdateTime = currentTime,
        //     DisplayOrder = 1,
        //     NoChild = 1,
        //     IsLock = 0,
        //     QStatus = 0
        // });

  
        if (HttpContext.User.Identity.IsAuthenticated) return Redirect($"/{CurrentLanguage}/catalog/article/list");
        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region Кіру +Login(string email, string password, int remember)

    [HttpPost]
    [AllowAnonymous]
    public IActionResult Login(string email, string password, string remember)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(email = email.Trim()))
            return MessageHelper.RedirectAjax(T("ls_Peye"), "error", "", "email");
        if (!RegexHelper.IsEmail(email = email.Trim().ToLower()))
            return MessageHelper.RedirectAjax(T("ls_Peavea"), "error", "", "email");
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(password = password.Trim()))
            return MessageHelper.RedirectAjax(T("ls_Peyp"), "error", "", "password");
        using var connection = Utilities.GetOpenConnection();
        try
        {
            var admin = connection.GetList<Admin>("where qStatus = 0 and email = @email", new { email })
                .FirstOrDefault();
            if (admin == null)
                return MessageHelper.RedirectAjax(T("ls_Weop"), "error", "", "");

            var errorLog = connection
                .GetList<Adminloginerrorlog>("where adminId = @adminId", new { adminId = admin.Id })
                .FirstOrDefault();
            var currentTime = DateTime.Now;

            var lastErrorTime = errorLog != null
                ? UnixTimeHelper.UnixTimeToDateTime(errorLog.LastErrorTime)
                : currentTime;
            var ts = new TimeSpan(currentTime.Ticks - lastErrorTime.Ticks);
            if (errorLog != null && errorLog.ErrorCount >= 5 && ts.TotalHours < 2)
                return MessageHelper.RedirectAjax(
                    "Құпия сөз терудің сәтсіз әрекеті көп болғандықтан жүйеге кіру 2 сағатқа бұғатталды ", "error",
                    "", null);

            password = Md5Helper.PasswordMd5Encrypt(password);
            if (!admin.Password.Equals(password))
                return MessageHelper.RedirectAjax(T("ls_Weop"), "error", "", "");

            var roleIdList = connection
                .Query<int>("select roleId from adminrole where qStatus = 0 and adminId = @adminId",
                    new { adminId = admin.Id }).ToList();
            var roleList = QarCache.GetRoleList(_memoryCache, CurrentLanguage).Where(x => roleIdList.Contains(x.Id))
                .ToList();
            SaveLoginInfoToCookie(admin.Email, admin.Name, admin.Id, roleList.Select(x => x.Id).ToList(),
                string.Join(" | ", roleList.Select(x => x.Name).ToArray()), admin.IsSuper == 1, admin.AvatarUrl,
                admin.SkinName);
            if (errorLog != null)
            {
                errorLog.ErrorCount = 0;
                connection.Update(errorLog);
            }
            return MessageHelper.RedirectAjax(T("ls_Loginsuccessfully"), "success",
                $"/{CurrentLanguage}/catalog/article/list", null);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Login");
            return MessageHelper.RedirectAjax("Сайт қателыгі, басқарушымен хабарласыңыз! ", "error", "", null);
        }
    }

    #endregion

    #region Signout +Signout()

    public IActionResult Signout()
    {
        var reason = GetStringQueryParam("reason");
        if (!string.IsNullOrEmpty(reason)) return Redirect($"/{CurrentLanguage.ToLower()}/admin/login");

        QarSingleton.GetInstance().RemoveReLoginAdmin(GetAdminId());

        var cookieExpireTime = UnixTimeHelper.ConvertToUnixTime(DateTime.Now.AddDays(-1 * ExpireDayCount));
        using (var connection = Utilities.GetOpenConnection())
        {
            var admin = connection.GetList<Admin>("where relogin = 1 and id = @adminId", new { adminId = GetAdminId() })
                .FirstOrDefault();
            if (admin != null && admin.UpdateTime < cookieExpireTime)
            {
                admin.ReLogin = 0;
                connection.Update(admin);
            }
        }

        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect($"/{CurrentLanguage.ToLower()}/admin/login");
    }

    #endregion

    #region Profile +Profile(string query)

    public IActionResult Profile(string query)
    {
        using (var connection = Utilities.GetOpenConnection())
        {
            var adminId = GetAdminId();
            ViewData["admin"] = connection.GetList<Admin>("where qStatus = 0 and id = @adminId", new { adminId })
                .FirstOrDefault();
        }

        return View($"~/Views/Console/{ControllerName}/{ActionName}.cshtml");
    }

    #endregion

    #region ProfileInfo +ProfileInfo(Admin item)

    [NoRole]
    [HttpPost]
    public IActionResult ProfileInfo(Admin item)
    {
        item.Phone ??= string.Empty;
        if (string.IsNullOrWhiteSpace(item.Name))
            return MessageHelper.RedirectAjax(T("ls_Peyn"), "error", "", "name");

        if (string.IsNullOrWhiteSpace(item.Email))
            return MessageHelper.RedirectAjax(T("ls_Peye"), "error", "", "email");

        if (!RegexHelper.IsEmail(item.Email = item.Email.Trim().ToLower()))
            return MessageHelper.RedirectAjax(T("ls_Peavea"), "error", "", "email");

        var phoneNumber = string.Empty;
        if (!string.IsNullOrWhiteSpace(item.Phone) && RegexHelper.IsPhoneNumber(item.Phone, out phoneNumber))
            return MessageHelper.RedirectAjax(T("ls_Petcpn"), "error", "", "phone");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using (var connection = Utilities.GetOpenConnection())
        {
            object queryObj = new { adminId = GetAdminId(), email = item.Email };
            if (connection.RecordCount<Admin>("where qStatus = 0 and id <> @adminId and email = @email", queryObj) > 0)
                return MessageHelper.RedirectAjax(T("ls_Peavea"), "error", "", "email");
            var admin = connection.GetList<Admin>("where qStatus = 0 and id = @adminId", queryObj).FirstOrDefault();
            admin.Email = item.Email;
            admin.Name = item.Name;
            admin.Phone = phoneNumber;
            admin.ReLogin = 1;
            admin.UpdateTime = currentTime;
            if (connection.Update(admin) > 0)
            {
                var roleIds = HttpContext.User.Identity.RoleIds();
                var roleNames = HttpContext.User.Identity.RoleNames();
                SaveLoginInfoToCookie(admin.Email, admin.Name, admin.Id, roleIds, roleNames, admin.IsSuper == 1,
                    admin.AvatarUrl, admin.SkinName);
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                    $"/{CurrentLanguage}/admin/profile", "");
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Change Password +ChangePassword(string oldPassword, string newPassword, string confirmPassword)

    [NoRole]
    [HttpPost]
    public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(oldPassword))
            return MessageHelper.RedirectAjax(T("ls_Peyp"), "error", "", "oldPassword");

        if (string.IsNullOrEmpty(newPassword))
            return MessageHelper.RedirectAjax(T("ls_Peanp"), "error", "", "newPassword");

        if (newPassword.Length < 6 || newPassword.Length > 20)
            return MessageHelper.RedirectAjax(
                T("ls_Pmcbmamc").Replace("{min}", "6").Replace("{max}", "20"),
                "error", "", "newPassword");

        if (string.IsNullOrEmpty(confirmPassword))
            return MessageHelper.RedirectAjax(T("ls_Confirmnewpassword"), "error", "", "confirmPassword");


        if (!newPassword.Equals(confirmPassword))
            return MessageHelper.RedirectAjax(T("ls_Confirmnewpassword"), "error", "", "confirmPassword");

        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using (var connection = Utilities.GetOpenConnection())
        {
            object queryObj = new { adminId = GetAdminId() };
            var admin = connection.GetList<Admin>("where qStatus = 0 and id = @adminId", queryObj).FirstOrDefault();
            if (admin == null)
                return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
            oldPassword = Md5Helper.PasswordMd5Encrypt(oldPassword);
            if (!admin.Password.Equals(oldPassword))
                return MessageHelper.RedirectAjax(T("ls_Opii"), "error", "", "oldPassword");
            admin.Password = Md5Helper.PasswordMd5Encrypt(newPassword);
            admin.UpdateTime = currentTime;
            if (connection.Update(admin) > 0)
                return MessageHelper.RedirectAjax(T("ls_Passwordchangedsuccessfully"), "success",
                    $"/{CurrentLanguage}/admin/profile", "");
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Save Skin +SaveSkin(string skinName)

    [NoRole]
    [HttpPost]
    public IActionResult SaveSkin(string skinName)
    {
        skinName = string.IsNullOrWhiteSpace(skinName) ? "default" : skinName;
        string[] skins = { "dark", "light", "default" };
        if (!skins.Contains(skinName))
            return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        using (var connection = Utilities.GetOpenConnection())
        {
            object queryObj = new { adminId = GetAdminId() };
            var admin = connection.GetList<Admin>("where qStatus = 0 and id = @adminId", queryObj).FirstOrDefault();
            if (admin == null)
                return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
            admin.SkinName = skinName;
            admin.UpdateTime = currentTime;
            if (connection.Update(admin) > 0)
            {
                var roleIds = HttpContext.User.Identity.RoleIds();
                var roleNames = HttpContext.User.Identity.RoleNames();
                SaveLoginInfoToCookie(admin.Email, admin.Name, admin.Id, roleIds, roleNames, admin.IsSuper == 1,
                    admin.AvatarUrl, admin.SkinName);
                return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success", "", "");
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Role +Role(string query)

    public IActionResult Role(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Administratortype");
        switch (query)
        {
            case "create":
                {
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        ViewData["permissionGroupList"] = connection.GetList<Permission>("where qStatus = 0")
                            .GroupBy(x => x.TableName).ToList();
                        ViewData["allNavigationList"] = QarCache.GetNavigationList(_memoryCache);
                    }

                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "edit":
                {
                    var roleId = GetIntQueryParam("id", 0);
                    if (roleId <= 0)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        var role = connection.GetList<Role>("where qStatus = 0 and id = @roleId ", new { roleId })
                            .FirstOrDefault();
                        if (role == null)
                            return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                        ViewData["role"] = role;
                        ViewData["multiLanguageList"] =
                            GetMultilanguageList(connection, nameof(Role), new List<int> { role.Id });
                        ViewData["permissionGroupList"] = connection.GetList<Permission>("where qStatus = 0")
                            .GroupBy(x => x.TableName).ToList();
                        ViewData["allNavigationList"] = QarCache.GetNavigationList(_memoryCache);
                        ViewData["rolePermissionList"] = connection
                            .GetList<Rolepermission>("where qStatus = 0 and roleId = @roleId", new { roleId = role.Id })
                            .ToList();
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

    #region Role +Role(Role item, string multiLanguageJson,string permissionJson)

    [HttpPost]
    public IActionResult Role(Role item, string multiLanguageJson, string permissionJson)
    {
        if (string.IsNullOrEmpty(item.Name))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "name");


        List<Multilanguage> multiLanguageList = null;
        try
        {
            multiLanguageList = JsonHelper.DeserializeObject<List<Multilanguage>>(multiLanguageJson);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ActionName);
            return MessageHelper.RedirectAjax(T("ls_Edjd"), "error", "", "multiLanguageJson");
        }

        List<Rolepermission> rolePermissionList = null;
        try
        {
            rolePermissionList = JsonHelper.DeserializeObject<List<Rolepermission>>(permissionJson);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ActionName);
            return MessageHelper.RedirectAjax(T("ls_Edjd"), "error", "", "");
        }

        item.Description ??= string.Empty;
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        int? res = 0;
        using (var connection = Utilities.GetOpenConnection())
        {
            if (item.Id == 0)
            {
                res = connection.Insert(new Role
                {
                    Name = item.Name,
                    Description = item.Description,
                    AddTime = currentTime,
                    UpdateTime = currentTime,
                    QStatus = 0
                });
                if (res > 0)
                {
                    SaveRolePermissionList(connection, res ?? 0, rolePermissionList);
                    SaveMultilanguageList(connection, multiLanguageList, nameof(Role), res ?? 0);
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRoleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRolePermissionList));
                    QarCache.ClearCache(_memoryCache, $"{nameof(QarCache.GetNavigationIdListByRoleId)}_{res ?? 0}");
                    return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}", "");
                }
            }
            else
            {
                var role = connection.GetList<Role>("where qStatus = 0 and id = @id", new { id = item.Id })
                    .FirstOrDefault();
                if (role == null)
                    return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");
                role.Name = item.Name;
                role.Description = item.Description;
                role.UpdateTime = currentTime;
                res = connection.Update(role);
                if (res > 0)
                {
                    SaveRolePermissionList(connection, role.Id, rolePermissionList);
                    SaveMultilanguageList(connection, multiLanguageList, nameof(Role), role.Id);
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRoleList));
                    QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRolePermissionList));
                    QarCache.ClearCache(_memoryCache, $"{nameof(QarCache.GetNavigationIdListByRoleId)}_{role.Id}");
                    var adminRoleIdList = connection.Query<int>(
                        "select adminId from adminrole where qStatus = 0 and roleId = @roleId",
                        new { roleId = role.Id });
                    var adminList =
                        connection.GetList<Admin>($"where qStatus = 0 and id in ({string.Join(",", adminRoleIdList)})");
                    if (adminList != null)
                        foreach (var admin in adminList)
                            QarSingleton.GetInstance().AddReLoginAdmin(admin.Id, admin.UpdateTime);
                    return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                        $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={role.Id}", "");
                }
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get role list +GetRoleList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetRoleList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using var connection = Utilities.GetOpenConnection();
        var querySql = " from role where qStatus = 0 ";
        object queryObj = new { keyword = "%" + keyword + "%" };
        var orderSql = "";
        if (!string.IsNullOrEmpty(keyword)) querySql += " and (name like @keyword)";
        if (model.OrderList != null && model.OrderList.Count > 0)
            foreach (var item in model.OrderList)
                switch (item.Column)
                {
                    case 3:
                        {
                            orderSql += (string.IsNullOrEmpty(orderSql) ? "" : ",") + " addTime " + item.Dir;
                        }
                        break;
                }

        if (string.IsNullOrEmpty(orderSql)) orderSql = " addTime desc ";

        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var roleList = connection
            .Query<Role>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}", queryObj)
            .ToList();
        var languageList = QarCache.GetLanguageList(_memoryCache);
        var dataList = roleList.Select(x => new
        {
            x.Id,
            x.Name,
            x.Description,
            AddTime = UnixTimeHelper.UnixTimeToDateTime(x.AddTime).ToString("dd/MM/yyyy HH:mm")
        }).ToList();
        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion

    #region Set role status +SetRoleStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetRoleStatus(string manageType, List<int> idList)
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
                        var roleList = connection
                            .GetList<Role>($"where qStatus = 0 and id in ({string.Join(",", idList)})").ToList();
                        foreach (var role in roleList)
                        {
                            role.QStatus = 1;
                            role.UpdateTime = currentTime;
                            connection.Update(role);
                        }

                        tran.Commit();
                        QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRoleList));
                        QarCache.ClearCache(_memoryCache, nameof(QarCache.GetRolePermissionList));
                        foreach (var role in roleList)
                            QarCache.ClearCache(_memoryCache,
                                $"{nameof(QarCache.GetNavigationIdListByRoleId)}_{role.Id}");
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

    #region Admin +Person(string query)

    public IActionResult Person(string query)
    {
        query = (query ?? string.Empty).Trim().ToLower();
        ViewData["query"] = query;
        ViewData["title"] = T("ls_Administrators");
        switch (query)
        {
            case "create":
                {
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        ViewData["roleList"] = connection.GetList<Role>("where qStatus = 0").ToList();
                    }

                    return View($"~/Views/Console/{ControllerName}/{ActionName}/CreateOrEdit.cshtml");
                }
            case "edit":
                {
                    var adminId = GetIntQueryParam("id", 0);
                    if (adminId <= 0)
                        return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                    using (var connection = Utilities.GetOpenConnection())
                    {
                        var admin = connection.GetList<Admin>("where qStatus = 0 and id = @adminId ", new { adminId })
                            .FirstOrDefault();
                        if (admin == null)
                            return Redirect($"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/list");
                        ViewData["admin"] = admin;
                        ViewData["roleList"] = connection.GetList<Role>("where qStatus = 0").ToList();
                        ViewData["adminRoleList"] = connection
                            .GetList<Adminrole>("where qStatus = 0 and adminId = @adminId ", new { adminId = admin.Id })
                            .ToList();
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

    #region Admin +Person(Admin item, List<int> roleIdList)

    [HttpPost]
    public IActionResult Person(Admin item, List<int> roleIdList)
    {
        item.Description ??= string.Empty;

        if (roleIdList.Count <= 0)
            return MessageHelper.RedirectAjax(T("ls_Calo"), "error", "", "roleIdList");

        if (string.IsNullOrEmpty(item.Name))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "name");

        if (string.IsNullOrEmpty(item.Email))
            return MessageHelper.RedirectAjax(T("ls_Tfir"), "error", "", "email");

        if (item.Id == 0 && string.IsNullOrEmpty(item.Password))
            return MessageHelper.RedirectAjax(T("ls_Peyp"), "error", "", "password");

        if (!string.IsNullOrEmpty(item.Password) && (item.Password.Length < 6 || item.Password.Length > 20))
            return MessageHelper.RedirectAjax(
                T("ls_Pmcbmamc").Replace("{min}", "6").Replace("{max}", "20"),
                "error", "", "password");

        var phoneNumber = string.Empty;
        if (!string.IsNullOrEmpty(item.Phone) && !RegexHelper.IsPhoneNumber(item.Phone, out phoneNumber))
            return MessageHelper.RedirectAjax(T("ls_Petcpn"), "error", "", "phone");

        item.Phone = phoneNumber;
        var currentTime = UnixTimeHelper.GetCurrentUnixTime();
        int? res = 0;


        using (var connection = Utilities.GetOpenConnection())
        {
            if (item.Id == 0)
            {
                using var tran = connection.BeginTransaction();
                try
                {
                    if (connection.RecordCount<Admin>("where qStatus = 0 and email = @email",
                            new { email = item.Email }) >
                        0)
                        return MessageHelper.RedirectAjax(T("ls_Eaiar"), "error", "",
                            "email");
                    res = connection.Insert(new Admin
                    {
                        Name = item.Name,
                        Description = item.Description,
                        IsSuper = 0,
                        AvatarUrl = "",
                        Email = item.Email,
                        Password = Md5Helper.PasswordMd5Encrypt(item.Password),
                        HiddenColumnJson = "",
                        Phone = item.Phone,
                        LatynUrl = item.LatynUrl = GetDistinctLatynUrl(connection, nameof(Article), "", item.Name, item.Id, ""),
                        ReLogin = 0,
                        SkinName = "light",
                        AddTime = currentTime,
                        UpdateTime = currentTime,
                        QStatus = 0
                    });

                    if (res > 0)
                    {
                        foreach (var roleId in roleIdList)
                            connection.Insert(new Adminrole
                            {
                                RoleId = roleId,
                                AdminId = res ?? 0,
                                AddTime = currentTime,
                                UpdateTime = currentTime,
                                QStatus = 0
                            });
                        tran.Commit();
                        return MessageHelper.RedirectAjax(T("ls_Addedsuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={res}",
                            "");
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    Log.Error(ex, ActionName);
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
            else
            {
                using var tran = connection.BeginTransaction();
                try
                {
                    var admin = connection.GetList<Admin>("where qStatus = 0 and id = @id", new { id = item.Id })
                        .FirstOrDefault();
                    if (admin == null)
                        return MessageHelper.RedirectAjax(T("ls_Idoiiw"), "error", "", "");

                    if (admin.IsSuper == 1 && admin.Id != GetAdminId())
                        return MessageHelper.RedirectAjax(T("ls_Accessdenied"), "error", "", "");

                    admin.Name = item.Name;
                    if (connection.RecordCount<Admin>("where qStatus = 0 and id <> @adminId and email = @email",
                            new { adminId = admin.Id, email = item.Email }) > 0)
                        return MessageHelper.RedirectAjax(T("ls_Eaiar"), "error", "",
                            "email");
                    if (!string.IsNullOrEmpty(item.Password))
                        admin.Password = Md5Helper.PasswordMd5Encrypt(item.Password);
                    admin.Email = item.Email;
                    admin.Description = item.Description;
                    admin.Phone = item.Phone;
                    admin.ReLogin = 1;
                    admin.UpdateTime = currentTime;
                    res = connection.Update(admin);
                    if (res > 0)
                    {
                        var adminRoleList = connection
                            .GetList<Adminrole>("where qStatus = 0 and adminId = @adminId ",
                                new { adminId = admin.Id }).ToList();
                        foreach (var adminRole in adminRoleList)
                            if (!roleIdList.Contains(adminRole.RoleId))
                            {
                                adminRole.QStatus = 1;
                                adminRole.UpdateTime = currentTime;
                                connection.Update(adminRole);
                            }

                        foreach (var roleId in roleIdList)
                            if (!adminRoleList.Exists(x => x.RoleId == roleId))
                            {
                                var adminRole = connection
                                    .GetList<Adminrole>("where adminId = @adminId and roleId = @roleId ",
                                        new { adminId = admin.Id, roleId }).FirstOrDefault();
                                if (adminRole != null)
                                {
                                    adminRole.QStatus = 0;
                                    adminRole.UpdateTime = currentTime;
                                    connection.Update(adminRole);
                                }
                                else
                                {
                                    connection.Insert(new Adminrole
                                    {
                                        RoleId = roleId,
                                        AdminId = admin.Id,
                                        AddTime = currentTime,
                                        UpdateTime = currentTime,
                                        QStatus = 0
                                    });
                                }
                            }

                        tran.Commit();
                        QarSingleton.GetInstance().AddReLoginAdmin(admin.Id, admin.UpdateTime);
                        return MessageHelper.RedirectAjax(T("ls_Updatesuccessfully"), "success",
                            $"/{CurrentLanguage}/{ControllerName.ToLower()}/{ActionName.ToLower()}/edit?id={admin.Id}",
                            "");
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    Log.Error(ex, ActionName);
                    return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
                }
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Savefailed"), "error", "", "");
    }

    #endregion

    #region Get admin list +GetPersonList(APIUnifiedModel model)

    [HttpPost]
    public IActionResult GetPersonList(ApiUnifiedModel model)
    {
        var start = model.Start > 0 ? model.Start : 0;
        var length = model.Length > 0 ? model.Length : 10;
        var keyword = (model.Keyword ?? string.Empty).Trim();
        using var connection = Utilities.GetOpenConnection();
        var querySql = " from admin where qStatus = 0 and isSuper <>  1 ";
        object queryObj = new { keyword = "%" + keyword + "%" };
        var orderSql = "";
        if (!string.IsNullOrEmpty(keyword)) querySql += " and (name like @keyword)";
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

        var total = connection.Query<int>("select count(1) " + querySql, queryObj).FirstOrDefault();
        var totalPage = total % length == 0 ? total / length : total / length + 1;
        var adminList = connection
            .Query<Admin>("select * " + querySql + " order by " + orderSql + $" limit {start} , {length}", queryObj)
            .ToList();
        var adminRoleList = new List<Adminrole>();
        var roleList = QarCache.GetRoleList(_memoryCache, CurrentLanguage);
        var dataList = new List<object>();
        if (adminList.Count > 0)
        {
            adminRoleList = connection
                .GetList<Adminrole>(
                    $"where qStatus = 0 and adminId in ({string.Join(",", adminList.Select(x => x.Id).ToArray())})")
                .ToList();
            foreach (var admin in adminList)
            {
                var currentRoleList = roleList
                    .Where(r => adminRoleList.Exists(ar => ar.AdminId == admin.Id && ar.RoleId == r.Id)).ToList();
                dataList.Add(new
                {
                    admin.Id,
                    admin.Name,
                    admin.Email,
                    admin.IsSuper,
                    Role = string.Join(", ", currentRoleList.Select(r => r.Name).ToArray()),
                    AddTime = UnixTimeHelper.UnixTimeToDateTime(admin.AddTime).ToString("dd/MM/yyyy HH:mm")
                });
            }
        }

        return MessageHelper.RedirectAjax(T("ls_Searchsuccessful"), "success", "",
            new { start, length, keyword, total, totalPage, dataList });
    }

    #endregion

    #region Set admin status +SetPersonStatus(string manageType,List<int> idList)

    [HttpPost]
    public IActionResult SetPersonStatus(string manageType, List<int> idList)
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
                        var adminList = connection
                            .GetList<Admin>($"where qStatus = 0 and id in ({string.Join(",", idList)})").ToList();
                        foreach (var admin in adminList)
                        {
                            if (admin.IsSuper == 1) continue;
                            admin.QStatus = 1;
                            admin.UpdateTime = currentTime;
                            connection.Update(admin);
                        }

                        tran.Commit();
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
}