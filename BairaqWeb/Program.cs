using System.Text.Encodings.Web;
using System.Text.Unicode;
using BairaqWeb;
using BairaqWeb.Filters;
using BairaqWeb.Hangfire;
using COMMON;
using Dapper;
using DBHelper;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.WebEncoders;
using Serilog;
using Serilog.Events;

var defaultTheme = "Bairaq";
var redirectUrl = string.Empty;
string domain = null;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
QarSingleton.GetInstance().SetSiteTheme(defaultTheme);
QarSingleton.GetInstance().SetConnectionString(builder.Configuration[$"{defaultTheme}:ConnectionString"]);
QarSingleton.GetInstance().SetSiteUrl(builder.Configuration[$"{defaultTheme}:SiteUrl"]);
redirectUrl = $"http://localhost:{builder.Configuration[$"{defaultTheme}:Port"]}";
if (string.IsNullOrEmpty(redirectUrl))
{
    throw new Exception($"RedirectUrl is empty defaultTheme ({defaultTheme})");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LoginPath = new PathString("/kz/admin/login/");
    options.AccessDeniedPath = new PathString("/kz/admin/login/");
    options.LogoutPath = new PathString("/kz/admin/signout/");
    options.Cookie.Path = "/";
    options.SlidingExpiration = true;
    options.Cookie.Domain = domain;
    options.Cookie.Name = "qar_cookie";
    options.Cookie.HttpOnly = true;
});

builder.Services.AddControllersWithViews((configure =>
{
    configure.Filters.Add(typeof(PermissionFilter));
    configure.Filters.Add(typeof(QarFilter));
})).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressConsumesConstraintForFormFileParameters = true;
    options.SuppressInferBindingSourcesForParameters = true;
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.Configure<FormOptions>(o =>
{
    o.ValueLengthLimit = int.MaxValue;
    o.ValueCountLimit = int.MaxValue;
    o.MultipartBodyLengthLimit = int.MaxValue;
    o.MemoryBufferThreshold = int.MaxValue;
    o.KeyLengthLimit = int.MaxValue;
});

builder.Services.Configure<WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

builder.Services.AddHangfire(x => x.UseMemoryStorage());
builder.Services.AddTransient<QarJob>();
builder.Services.AddHangfireServer();


var app = builder.Build();
app.UseMiddleware<RedirectMiddleware>();
var provider = new FileExtensionContentTypeProvider();
provider.Mappings.Remove(".xml");
provider.Mappings.Add(".xml", "application/xml");
provider.Mappings.Remove(".txt");
provider.Mappings.Add(".txt", "text/plain");
provider.Mappings.Remove(".xsl");
provider.Mappings.Add(".xsl", "text/xsl");
provider.Mappings.Remove(".exe");
provider.Mappings.Add(".exe", "application/exe");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx => { ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000"); }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "culture_action",
    pattern: "{culture=kz}/{action=Index}/{query?}",
    defaults: new { controller = "Home" },
    constraints: new { culture = "kz|tote|latyn|ru|en" } //|ru|en|zh-cn|tr
);

// app.MapControllerRoute(
//   name: "newspaper_default",
//   pattern: "{culture=kz}/{action=Newspaper}/{year}/{id?}",
//   defaults: new { controller = "Home" },
//   constraints: new { culture = "kz|tote|latyn" }//|ru|en|zh-cn|tr
// );

app.MapControllerRoute(
    name: "language_default",
    pattern: "{culture=kz}/{Home}/{action=Index}/{query?}",
    constraints: new { culture = "kz|tote|latyn|ru|en" } //|ru|en|zh-cn|tr
);

app.MapControllerRoute(
    name: "admin_default",
    pattern: "{culture=kz}/{controller=Admin}/{action=Login}/{query?}",
    constraints: new { culture = "kz|tote|latyn|ru|en|zh-cn|tr" }
);

app.MapFallbackToFile("404.html");
app.UseHangfireDashboard();
SimpleCRUD.SetDialect(SimpleCRUD.Dialect.MySQL);
SimpleCRUD.SetTableNameResolver(new QarTableNameResolver());
// BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncOldDbArticle(), TimeSpan.FromSeconds(0));
// BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobGenerateTagLatynUrl(), TimeSpan.FromSeconds(12));
// BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncOldDbCategory(), TimeSpan.FromSeconds(10));
// BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncOldDbArticle(), TimeSpan.FromMinutes(10));

// using (var _connection = Utilities.GetOldServerDBConnection())
// {
//     int maxId = _connection.Query<int>("select max(id) from article where qStatus <> 1").FirstOrDefault();
//     QarSingleton.GetInstance().SetIntValue("maxArticleId", maxId);
// }
// BackgroundJob.Schedule<QarJob>(q => q.JobSyncOldDbUsers(), TimeSpan.FromSeconds(0));
if (app.Environment.IsDevelopment())
{
    app.Run();
}
else
{
    // BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncCollectNewspaperMedia(), TimeSpan.FromSeconds(0));
    // BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncCollectArticleMedia(), TimeSpan.FromSeconds(20));
    // BackgroundJob.Schedule<BairaqWeb.Hangfire.QarJob>(q => q.JobSyncCollectOldServerArticleMedia(), TimeSpan.FromSeconds(10));

    RecurringJob.AddOrUpdate<QarJob>("jobSaveArticleViewCount", q => q.JobSaveArticleViewCount(), "*/1 * * * *");
    BackgroundJob.Schedule<QarJob>(q => q.JobSaveReloginAdminIds(), TimeSpan.FromMinutes(1));
    BackgroundJob.Schedule<QarJob>(q => q.JobSaveSiteMap(), TimeSpan.FromMinutes(0));
    RecurringJob.AddOrUpdate<QarJob>("job_save_currency_rate", q => q.JobSaveCurrencyRate(), "*/3 * * * *"); // 3 Min
    RecurringJob.AddOrUpdate<QarJob>("job_delete_old_log_files", q => q.JobDeleteOldLogFiles(), Cron.Daily); // 1 day
    RecurringJob.AddOrUpdate<QarJob>("job_publish_auto_publish_article", q => q.JobPublishAutoPublishArticle(),
        Cron.Minutely);
    RecurringJob.AddOrUpdate<QarJob>("job_save_site_map", q => q.JobSaveSiteMap(), "0 6 * * 1"); // Every Monday at 6 AM
    app.Run(redirectUrl);
}