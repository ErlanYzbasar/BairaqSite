using BairaqWeb.Caches;
using COMMON;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BairaqWeb.Filters
{
    public class PermissionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is not Controller controller) return;
            if (context.ActionDescriptor is not ControllerActionDescriptor action) return;

            var controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString().ToLower();
            var actionName = context.HttpContext.Request.RouteValues["action"].ToString().ToLower();

            if (context.HttpContext.User.Identity.Role().Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var isSuper = context.HttpContext.User.Identity.IsSuperAdmin();
                var memoryCache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                if (isSuper)
                {
                    controller.ViewData["canView"] = true;
                    controller.ViewData["canCreate"] = true;
                    controller.ViewData["canEdit"] = true;
                    controller.ViewData["canDelete"] = true;
                    controller.ViewData["navigationList"] = QarCache.GetNavigationList(memoryCache);
                    return;
                }

                var language = context.HttpContext.Request.RouteValues["culture"].ToString();
                var method = context.HttpContext.Request.Method.ToUpper();

                var loginTime = context.HttpContext.User.Identity.LoginTime();
                if (QarSingleton.GetInstance()
                        .IsReLoginAdmin(context.HttpContext.User.Identity.AdminId(), out var updateTime) &&
                    loginTime < updateTime)
                {
                    switch (method)
                    {
                        case "GET":
                        {
                            controller.ViewData["reloginReason"] = "Your session has timed out. Please login again.";
                        }
                            break;
                        case "POST":
                        {
                            context.Result =
                                MessageHelper.RedirectAjax("Your session has timed out. Please login again.", "error",
                                    "", null);
                            return;
                        }
                    }
                }

                var roleIdList = context.HttpContext.User.Identity.RoleIds();
                var canViewNavigationIdList = new List<int>();
                foreach (var roleId in roleIdList)
                {
                    canViewNavigationIdList.AddRange(QarCache.GetNavigationIdListByRoleId(memoryCache, roleId));
                }

                controller.ViewData["navigationList"] = QarCache.GetNavigationList(memoryCache)
                    .Where(x => canViewNavigationIdList.Contains(x.Id)).ToList();

                // var controllerName = context.HttpContext.Request.RouteValues["controller"].ToString();
                // var actionName = context.HttpContext.Request.RouteValues["action"].ToString();
                var query = (context.HttpContext.Request.RouteValues["query"] ?? string.Empty).ToString().Trim()
                    .ToLower();
                QarCache.CheckNavigationPermission(memoryCache, roleIdList, controller, action, method, out var canView,
                    out var canCreate, out var canEdit, out var canDelete);
                controller.ViewData["canView"] = canView;
                controller.ViewData["canCreate"] = canCreate;
                controller.ViewData["canEdit"] = canEdit;
                controller.ViewData["canDelete"] = canDelete;
                switch (query)
                {
                    case "create":
                    {
                        if (!canCreate)
                        {
                            context.Result = new RedirectResult($"/{language}/{controllerName}/{actionName}/list");
                            return;
                        }
                    }
                        break;
                    case "edit":
                    {
                        if (!canEdit)
                        {
                            context.Result = new RedirectResult($"/{language}/{controllerName}/{actionName}/list");
                            return;
                        }
                    }
                        break;
                    case "list":
                    {
                        if (!canView)
                        {
                            context.Result = new RedirectResult($"/{language}/admin/profile");
                            return;
                        }
                    }
                        break;
                }

                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    if (!controllerName.Equals("QarBase", StringComparison.OrdinalIgnoreCase) &&
                        !actionName.Equals("AdditionalContent", StringComparison.OrdinalIgnoreCase))
                    {
                        if (actionName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) &&
                            actionName.EndsWith("List", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!canView)
                            {
                                context.Result = MessageHelper.RedirectAjax(
                                    QarCache.GetLanguageValue(memoryCache, "ls_Accessdenied", language), "error", "",
                                    new
                                    {
                                        start = 0, length = 10, keyword = "", total = 0, totalPage = 0,
                                        dataList = new List<object>()
                                    });
                            }
                        }
                        else if (actionName.StartsWith("Set", StringComparison.OrdinalIgnoreCase) &&
                                 actionName.EndsWith("Status", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!canDelete)
                            {
                                context.Result = MessageHelper.RedirectAjax(
                                    QarCache.GetLanguageValue(memoryCache, "ls_Accessdenied", language), "error", "",
                                    null);
                            }
                        }
                        else
                        {
                            if (context.HttpContext.Request.HasFormContentType)
                            {
                                string id = context.HttpContext.Request.Form["id"];

                                if (id == "0" && !canCreate)
                                {
                                    context.Result = MessageHelper.RedirectAjax(
                                        QarCache.GetLanguageValue(memoryCache, "ls_Accessdenied", language), "error",
                                        "", null);
                                }
                                else
                                {
                                    if (!canEdit)
                                    {
                                        context.Result = MessageHelper.RedirectAjax(
                                            QarCache.GetLanguageValue(memoryCache, "ls_Accessdenied", language),
                                            "error", "", null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}