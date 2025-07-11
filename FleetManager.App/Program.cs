

using System.Globalization;
using DVLA.Business.UserModule;
using FleetManager.Business;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Hubs;
using FleetManager.Business.Implementations;
using FleetManager.Business.Implementations.EmailModule;
using FleetManager.Business.Implementations.UserModule;
using FleetManager.Business.Interfaces.EmailModule;
using FleetManager.Business.Interfaces.UserModule;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

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
builder.Services.AddSignalR();

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


// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthDemo.Cookie";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use HTTPS
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();


//Add UserManager<ApplicationUser>, RoleManager<ApplicationRole>, and SignInManager<ApplicationUser>
builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddScoped<RoleManager<ApplicationRole>>();
builder.Services.AddScoped<SignInManager<ApplicationUser>>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); // Set session duration
    options.Cookie.HttpOnly = true; // Make the cookie HTTP only
    options.Cookie.IsEssential = true; // Make the cookie essential
});
builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IUserService, UserService>();
//builder.Services.AddTransient<IApplicantService, ApplicantService>();
//builder.Services.AddTransient<IVisualAssessmentResultRepository, VisualAssessmentResultService>();
//builder.Services.AddTransient<ILocationService, LocationService>();
//builder.Services.AddTransient<IOptometristService, OptometristService>();
//builder.Services.AddTransient<ISlotRepository, SlotRepository>();
//builder.Services.AddTransient<ISlotUsageRepository, SlotUsageRepository>();
//builder.Services.AddTransient<IReportRepository, ReportService>();
//builder.Services.AddScoped(typeof(IRepositoryQuery<>), typeof(GenericRepository<>));
//builder.Services.AddTransient<IAuditRepo, AuditRepo>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
//builder.Services.AddTransient<IAnalyticRepository, AnalyticRepository>();
//builder.Services.AddTransient<INotificationRepository, NotificationRepository>();
//builder.Services.AddTransient<IPaymentService, PaymentService>();
//builder.Services.AddTransient<ISmsRepository, SmsRepository>();
builder.Services.AddTransient<IAuditRepo, AuditRepo>();
builder.Services.AddTransient<IAuthUser, AuthUser>();
//builder.Services.AddTransient<ITempPasswordService, TempPasswordService>();
builder.Services.AddTransient<BackgroundJobService>();


builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseDefaultTypeSerializer()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));


builder.Services.AddHangfireServer();



var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
//app.UseStaticFiles(new StaticFileOptions()
//{
//    FileProvider = new PhysicalFileProvider(
//    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template")),
//    RequestPath = "/wwwroot/Template"
//});
//app.UseStaticFiles(new StaticFileOptions()
//{
//    FileProvider = new PhysicalFileProvider(
//    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AppFile")),
//    RequestPath = "/wwwroot/AppFile"
//});
//app.UseStaticFiles(new StaticFileOptions()
//{
//    FileProvider = new PhysicalFileProvider(
//    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logos")),
//    RequestPath = "/wwwroot/logos"
//});

//app.UseStaticFiles(new StaticFileOptions()
//{
//    FileProvider = new PhysicalFileProvider(
//    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Passports")),
//    RequestPath = "/passports"
//});


app.UseRouting();

app.UseAuthentication();

app.UseSession();

app.UseAuthorization();


app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=ResetPassword}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notification");

//RecurringJob.AddOrUpdate<BackgroundJobService>("SendBulkEmail", service => service.SendBulkEmail(), "*/2 * * * *");
//RecurringJob.AddOrUpdate<BackgroundJobService>("VerifyTransfers", service => service.VerifyPayments(), "*/1 * * * *");
//RecurringJob.AddOrUpdate<BackgroundJobService>("PushVisualAssessmentResult", service => service.PushVisualAssessmentResult(), "*/2 * * * *");//Every 1 minute


//Create a scope to resolve scoped services
using (var scope = app.Services.CreateScope())
{
    var scopedProvider = scope.ServiceProvider;

    // Resolve the scoped service
    var userService = scopedProvider.GetRequiredService<IUserService>();

    // Call the method on the scoped service
    await userService.SeedRoles();
    await userService.SeedSuperAdminUser();
}

app.Run();
