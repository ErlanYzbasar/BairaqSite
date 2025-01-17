using System.Security.Claims;
using System.Security.Principal;

namespace COMMON;

public static class IdentityExtension
{
    public static string RealName(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("RealName");
        return (claim != null) ? claim.Value : string.Empty;
    }

    public static int AdminId(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("AdminId");
        var strAdminId = (claim != null) ? claim.Value : string.Empty;
        var adminId = 0;
        if (int.TryParse(strAdminId, out adminId)) return adminId;
        return 0;
    }

    public static List<int> RoleIds(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("RoleIds");
        var roleIdStrs = (claim != null) ? claim.Value : string.Empty;
        if (string.IsNullOrEmpty(roleIdStrs)) return new List<int>();
        return roleIdStrs.Split(",").Select(int.Parse).ToList();
    }

    public static string RoleNames(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("RoleNames");
        return (claim != null) ? claim.Value : string.Empty;
    }

    public static bool IsSuperAdmin(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("isSuperAdmin");
        var strIsSuperAdmin = (claim != null) ? claim.Value : string.Empty;
        var isSuperAdmin = 0;
        if (int.TryParse(strIsSuperAdmin, out isSuperAdmin)) return isSuperAdmin == 1;
        return false;
    }

    public static string Role(this IIdentity identity)
    {
        var userIdentity = ((ClaimsIdentity)identity);
        var claims = userIdentity.Claims;
        var roleClaimType = userIdentity.RoleClaimType;
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault();
        return roles != null ? roles.Value : "";
    }

    public static string AvatarUrl(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("AvatarUrl");
        return (claim != null) ? claim.Value : string.Empty;
    }

    public static string SkinName(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("SkinName");
        return (claim != null) ? claim.Value : string.Empty;
    }

    public static int LoginTime(this IIdentity identity)
    {
        var claim = ((ClaimsIdentity)identity).FindFirst("LoginTime");
        var strLoginTime = (claim != null) ? claim.Value : string.Empty;
        var loginTime = 0;
        if (int.TryParse(strLoginTime, out loginTime)) return loginTime;
        return 0;
    }
}