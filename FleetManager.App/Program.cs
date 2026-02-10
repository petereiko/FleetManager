

using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DVLA.Business.UserModule;
using FleetManager.App;
using FleetManager.App.Areas.Admin.Controllers;
using FleetManager.Business;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.ReportsDto;
using FleetManager.Business.GoogleMap.Options;
using FleetManager.Business.GoogleRoutesApi.Interfaces;
using FleetManager.Business.GoogleRoutesApi.Services;
using FleetManager.Business.Hubs;
using FleetManager.Business.Implementations;
using FleetManager.Business.Implementations.AdminDashboardService;
using FleetManager.Business.Implementations.CompanyBranchModule;
using FleetManager.Business.Implementations.CompanyDashboardModule;
using FleetManager.Business.Implementations.CompanyModule;
using FleetManager.Business.Implementations.CompanyOnboardingModule;
using FleetManager.Business.Implementations.ContactDirectoryModule;
using FleetManager.Business.Implementations.DriverDashboardModule;
using FleetManager.Business.Implementations.DriverVehicleModule;
using FleetManager.Business.Implementations.DutyOfCareModule;
using FleetManager.Business.Implementations.EmailModule;
using FleetManager.Business.Implementations.FineAndTollModule;
using FleetManager.Business.Implementations.FuelLogModule;
using FleetManager.Business.Implementations.MaintenanceModule;
using FleetManager.Business.Implementations.ManageDriverModule;
using FleetManager.Business.Implementations.NotificationModule;
using FleetManager.Business.Implementations.RentalModule;
using FleetManager.Business.Implementations.RepairModule;
using FleetManager.Business.Implementations.ReportHubModule;
using FleetManager.Business.Implementations.ReportModule;
using FleetManager.Business.Implementations.ScheduleModule;
using FleetManager.Business.Implementations.TripLocationModule;
using FleetManager.Business.Implementations.TripModule;
using FleetManager.Business.Implementations.TripReportModule;
using FleetManager.Business.Implementations.UserModule;
using FleetManager.Business.Implementations.VehicleModule;
using FleetManager.Business.Implementations.VendorModule;
using FleetManager.Business.Implementations.Webhooks;
using FleetManager.Business.Interfaces.AdminDashboardModule;
using FleetManager.Business.Interfaces.CompanyBranchModule;
using FleetManager.Business.Interfaces.CompanyDashboardModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.CompanyOnboardingModule;
using FleetManager.Business.Interfaces.ContactDirectoryModule;
using FleetManager.Business.Interfaces.DriverDashboardModule;
using FleetManager.Business.Interfaces.DriverProfileModule;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.DutyOfCareModule;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.FineAndTollModule;
using FleetManager.Business.Interfaces.FuelLogModule;
using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.RentalModule;
using FleetManager.Business.Interfaces.RepairModule;
using FleetManager.Business.Interfaces.ReportHubModule;
using FleetManager.Business.Interfaces.ReportModule;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.TripLocationModule;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.TripReportModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.Interfaces.VendorModule;
using FleetManager.Business.Interfaces.WebhookModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.UtilityModels.AuthenticationModule;
using FleetManager.Business.UtilityModels.CommonSecurity;
using FleetManager.Business.UtilityModels.PdfService;
using FleetManager.Business.UtilityModels.RedisConfiguration;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PuppeteerSharp;
using StackExchange.Redis;
using System.Configuration;
using System.Globalization;
using System.Text;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("en-GB"); // or whatever culture is correct
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<FleetManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddMemoryCache();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // For development
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});
//builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// Add Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    //Configure Identity Options
    options.SignIn.RequireConfirmedAccount = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(1);
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
})
    .AddEntityFrameworkStores<FleetManagerDbContext>()
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddRoleManager<RoleManager<ApplicationRole>>()
    .AddUserStore<UserStore<ApplicationUser, ApplicationRole, FleetManagerDbContext, string, IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>>()
.AddRoleStore<RoleStore<ApplicationRole, FleetManagerDbContext, string, ApplicationUserRole, IdentityRoleClaim<string>>>()
.AddDefaultTokenProviders();


builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();


//Add UserManager<ApplicationUser>, RoleManager<ApplicationRole>, and SignInManager<ApplicationUser>
builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddScoped<RoleManager<ApplicationRole>>();
builder.Services.AddScoped<SignInManager<ApplicationUser>>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(540); // Set session duration
    options.Cookie.HttpOnly = true; // Make the cookie HTTP only
    options.Cookie.IsEssential = true; // Make the cookie essential
});


builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomClaimsPrincipalFactory>();

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<ICompanyManagementService, CompanyService>();
builder.Services.AddTransient<IBranchService, BranchService>(); 
builder.Services.AddTransient<ICompanyAdminService, CompanyAdminService>();
builder.Services.AddTransient<IAdminVehicleService, AdminVehicleService>();
builder.Services.AddTransient<IManageDriverService, ManageDriverService>();
builder.Services.AddTransient<IDriverProfileService, DriverProfileService>();
builder.Services.AddTransient<IDriverVehicleService, DriverVehicleService>();
builder.Services.AddTransient<IDriverDutyOfCareService, DriverDutyOfCareService>();
builder.Services.AddTransient<IFuelLogService, FuelLogService>();
builder.Services.AddTransient<IFineAndTollService, FineAndTollService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IVendorService, VendorService>();
builder.Services.AddTransient<IRentalService, RentalService>();
builder.Services.AddTransient<IReportService, ReportService>();
builder.Services.AddTransient<IReportExportService, ReportExportService>();
builder.Services.AddTransient<IContactDirectoryService, ContactDirectoryService>();
builder.Services.AddTransient<IMaintenanceService, MaintenanceService>();
builder.Services.AddTransient<ITimeOffService, TimeOffService>();
builder.Services.AddTransient<ITimeOffCategoryService, TimeOffCategoryService>();
builder.Services.AddTransient<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddTransient<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddTransient<ICompanyOwnerDashboardService, CompanyOwnerDashboardService>();
builder.Services.AddTransient<IRepairService, RepairService>();
builder.Services.AddTransient<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddTransient<NotificationWorker>();
builder.Services.AddTransient<ITripService, TripService>();
builder.Services.AddTransient<ITripReportService, TripReportService>();
builder.Services.AddTransient<IDriverDashboardService, DriverDashboardService>();
builder.Services.AddTransient<IReportHubService, ReportHubService>();




//builder.Services.AddSingleton<IGoogleRoutesService, FakeRoutesService>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IAuditRepo, AuditRepo>();
builder.Services.AddTransient<IAuthUser, AuthUser>();
builder.Services.AddTransient<BackgroundJobService>();


builder.Services.AddSingleton<IRedisService, RedisService>();

// ✅ Location Service
builder.Services.AddScoped<ITripLocationService, TripLocationService>();
builder.Services.AddScoped<ILocationFilterService, LocationFilterService>();
builder.Services.AddScoped<LocationProcessingJob>();


// DbContextFActory
//builder.Services.AddDbContextFactory<FleetManagerDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDataProtection()
    // .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyRingPath"]))
    // .SetApplicationName("FleetManager")
    ;
// --- Id protector + filter registration ----------------------------------
builder.Services.AddSingleton<IIdProtector, DataProtectionIdProtector>();
builder.Services.AddScoped<UnprotectIdActionFilter>();

// Add MVC and register the UnprotectIdActionFilter globally so it runs for all actions
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<UnprotectIdActionFilter>();
});


//Pdf Serivce
// 1) MVC + Razor‑to‑string
//builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();

// 2) Download & launch Chromium ONCE at startup, then register the Browser
//    We block on the async calls here so we produce a real Browser, not a Task<Browser>.

var browser = Puppeteer
    .LaunchAsync(new LaunchOptions { Headless = true })
    .GetAwaiter()
    .GetResult();
builder.Services.AddSingleton(browser);

// 3) Your PDF service
builder.Services.AddScoped<IPdfService, PuppeteerPdfService>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "FleetManager.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(540);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ------------------------
// Add Authentication for JWT + Cookies together
// ------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
})
.AddPolicyScheme("SmartAuth", "JWT or Cookie", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;

        return IdentityConstants.ApplicationScheme; // Identity cookie
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JWT Secret not configured"))
        ),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ------------------------
// Authorization policies
// ------------------------
builder.Services.AddAuthorization(options =>
{
    // Web access for Drivers
    options.AddPolicy("DriverWeb", policy => policy.RequireRole("Driver"));

    // API access for Drivers using JWT
    options.AddPolicy("DriverApi", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole("Driver");
    });
});

//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("DriverWeb", policy => policy.RequireRole("Driver"));
//    options.AddPolicy("DriverApi", policy =>
//    {
//        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
//        policy.RequireRole("Driver");
//    });
//});


// ✅ Redis Configuration
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
builder.Services.Configure<LocationTrackingSettings>(builder.Configuration.GetSection("LocationTrackingSettings"));

var redisSettings = builder.Configuration.GetSection("RedisSettings").Get<RedisSettings>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisSettings.ConnectionString);
    configuration.AbortOnConnectFail = redisSettings.AbortOnConnectFail;
    configuration.ConnectTimeout = redisSettings.ConnectTimeout;
    configuration.SyncTimeout = redisSettings.SyncTimeout;

    return ConnectionMultiplexer.Connect(configuration);
});



//builder.Services.AddHangfire(configuration => configuration
//    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
//    .UseSimpleAssemblyNameTypeSerializer()
//    .UseDefaultTypeSerializer()
//    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
//    {
//        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
//        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
//        QueuePollInterval = TimeSpan.FromSeconds(15),
//        UseRecommendedIsolationLevel = true,
//        DisableGlobalLocks = true
//    }));

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "default", "locations", "notifications" };
});
//Google Map

builder.Services.Configure<GoogleRoutesApiOptions>(builder.Configuration.GetSection("GoogleRoutesApi"));
builder.Services.AddHttpClient<IGoogleRoutesService, GoogleRoutesService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GoogleRoutesApiOptions>>().Value;

    client.BaseAddress = new Uri($"{options.BaseUrl}/directions/v2:computeRoutes");
    client.Timeout = options.Timeout;

    client.DefaultRequestHeaders.Add("X-Goog-Api-Key", options.ServerSideApiKey);
    client.DefaultRequestHeaders.Add("X-Goog-FieldMask",
        "routes.routeLabels,routes.legs,routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline,routes.legs.steps,routes.legs.startLocation,routes.legs.endLocation");
});


//builder.Services.AddHttpClient("VehicleModelsApi", client =>
//{
//    client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/");
//    client.Timeout = TimeSpan.FromHours(5); // Set a reasonable timeout
//});



var app = builder.Build();
// Configure the HTTP request pipeline.


if (!app.Environment.IsDevelopment())
{
    // 1) catch *unhandled* exceptions and send to /Home/Error
    app.UseExceptionHandler("/Home/Error");

    // 2) catch status codes (401, 403, 404, etc.) and re‐execute to a controller
    app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");
}
else
{
    app.UseDeveloperExceptionPage();  // only in dev
}

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseSession();

app.UseAuthorization();

//app.UseMiddleware<SessionTimeoutRedirectMiddleware>();


//app.UseHangfireDashboard("/hangfire");
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "FleetGuard Background Jobs"
});
//// ✅ Schedule recurring location processing job
//var locationSettings = builder.Configuration.GetSection("LocationTrackingSettings").Get<LocationTrackingSettings>();

//RecurringJob.AddOrUpdate<LocationProcessingJob>(
//    "process-location-queue",
//    job => job.ProcessLocations(),
//    $"*/{locationSettings.BackgroundJobIntervalMinutes} * * * *" // Every N minutes
//);


//app.MapControllerRoute(
//    name: "admin",
//    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
//    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=ResetPassword}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Map SignalR Hub
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapHub<TripTrackingHub>("/hubs/trip-tracking");

//    // Your other endpoints
//    endpoints.MapControllers();
//    endpoints.MapRazorPages();
//});

app.MapHub<TripTrackingHub>("/hubs/trip-tracking");
app.MapHub<NotificationHub>("/notificationHub");



//using (var scope = app.Services.CreateScope())
//{
//    var svc = scope.ServiceProvider.GetRequiredService<IPublicHolidayService>();
//    await svc.FetchAndStoreHolidaysAsync("NG", DateTime.UtcNow.Year);
//}


RecurringJob.AddOrUpdate<IPublicHolidayService>("GetHolidays", svc => svc.FetchAndStoreHolidaysAsync("NG", DateTime.UtcNow.Year), Cron.Yearly(1, 1)); // every Jan 1
RecurringJob.AddOrUpdate<ITripReportService>("recompute-daily-aggregrates", svc => svc.RecomputeDailyAggregateAsync(DateTime.UtcNow.Date.AddDays(-1)), Cron.Daily); // every midnight
var locationSettings = builder.Configuration.GetSection("LocationTrackingSettings").Get<LocationTrackingSettings>();

RecurringJob.AddOrUpdate<ITripLocationService>(
    "process-location-queue",
    service => service.ProcessLocationQueueAsync(),
    $"*/{locationSettings.BackgroundJobIntervalMinutes} * * * *" // Every 2 minutes
);


//Create a scope to resolve scoped services
using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;

    // Resolve the scoped service
    var userService = scopedProvider.GetRequiredService<IUserService>();

    var vehicleService = scopedProvider.GetRequiredService<IAdminVehicleService>();

    // Call the method on the scoped service
    //await userService.SeedRoles();
    //await userService.SeedSuperAdminUser();

    //await vehicleService.LoadModels();
}

app.Run();
