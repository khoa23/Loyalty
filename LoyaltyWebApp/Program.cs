using Serilog;
using System.IO;

// Cấu hình Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.File(
        path: Path.Combine(Directory.GetCurrentDirectory(), "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Starting LoyaltyWebApp application");

    var builder = WebApplication.CreateBuilder(args);

    // Sử dụng Serilog thay vì logging mặc định
    builder.Host.UseSerilog();

    builder.Services.AddRazorPages()
        .AddRazorPagesOptions(options =>
        {
            options.Conventions.AddPageRoute("/Admin/Create", "/Create");
        });
    builder.Services.AddControllers();
    
    // Cấu hình HttpClient để bypass SSL validation (cho development/testing)
    // CẢNH BÁO: Chỉ sử dụng cho development, không dùng cho production!
    builder.Services.AddHttpClient("LoyaltyAPI", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001/");
        var apiKey = builder.Configuration.GetValue<string>("ApiKey");
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
        }
    })
    .ConfigurePrimaryHttpMessageHandler(() => 
    {
        var handler = new HttpClientHandler();
        // Cấu hình HttpClient để bypass SSL validation (cho development/testing)
        // CẢNH BÁO: Chỉ sử dụng cho development, không dùng cho production!
        if (!builder.Environment.IsProduction())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        return handler;
    });

// Add session management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddScoped<LoyaltyWebApp.Services.IUserService, LoyaltyWebApp.Services.UserService>();
builder.Services.AddScoped<LoyaltyWebApp.Services.IRewardService, LoyaltyWebApp.Services.RewardService>();
builder.Services.AddScoped<LoyaltyWebApp.Services.ILoyaltyService, LoyaltyWebApp.Services.LoyaltyService>();
builder.Services.AddScoped<LoyaltyWebApp.Services.ICustomerService, LoyaltyWebApp.Services.CustomerService>();

    var app = builder.Build();

    if (!app.Environment.IsProduction())
    {
        app.UseDeveloperExceptionPage();
    }

    // Cấu hình base path cho IIS deployment
    var pathBase = builder.Configuration.GetValue<string>("ASPNETCORE_APPL_PATH") ?? "";
    if (!string.IsNullOrEmpty(pathBase))
    {
        app.UsePathBase(pathBase);
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.MapRazorPages();
    app.MapControllers();

    Log.Information("LoyaltyWebApp is starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LoyaltyWebApp failed to start");
    throw;
}
finally
{
    Log.Information("LoyaltyWebApp is shutting down");
    Log.CloseAndFlush();
}
